using Backend.Core.Models.SongPraise;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Backend.Core.Services
{

    public sealed class SongPraiseDownloadOptions
    {
        public string BaseUrl { get; init; } = "https://www.songpraise.com/api";
        public int MaxConcurrency { get; init; } = 8;   // parallel requests; raise it gradually
        public TimeSpan DelayBetweenRequests { get; init; } = TimeSpan.FromMilliseconds(50);   // per worker
        public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; init; } = 5;
        public int MinViews { get; init; } = 0;
    }

    public sealed record DownloadSummary(int Saved, int Skipped);

    public sealed class SongPraiseDownloader
    {
        private readonly HttpClient http;
        private readonly ILogger<SongPraiseDownloader> logger;
        private readonly SongPraiseDownloadOptions options;

        private long pauseUntilTicks;   // while "now" is before this, every worker waits (set after a 429)
        private int requestDelayMs;     // per-worker delay, grows after rate limiting

        public SongPraiseDownloader(HttpClient http, ILogger<SongPraiseDownloader> logger,
            SongPraiseDownloadOptions? options = null)
        {
            this.http = http;
            this.logger = logger;
            this.options = options ?? new SongPraiseDownloadOptions();
            requestDelayMs = (int)this.options.DelayBetweenRequests.TotalMilliseconds;
        }

        public async Task<DownloadSummary> DownloadAsync(string cacheFile, int? limit = null,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cacheFile))!);
            var skipFile = cacheFile + ".skipped";   // ids that 404'd or were deleted, so we don't retry them

            var done = SongPraiseCache.ReadAll(cacheFile).Select(s => s.Uuid).ToHashSet();
            if (File.Exists(skipFile)) done.UnionWith(File.ReadLines(skipFile));

            logger.LogInformation("Fetching song list...");
            var titles = await GetTitleListAsync(ct);

            // most-viewed first, so stopping early still leaves you with the songs people actually use
            var todo = titles
                .Where(t => !t.Deleted && t.VerseCount > 0 && t.Views >= options.MinViews && !done.Contains(t.Id))
                .OrderByDescending(t => t.Views)
                .Select(t => t.Id)
                .Take(limit ?? int.MaxValue)
                .ToList();

            logger.LogInformation("{Listed} songs listed, {Todo} to download ({Workers} workers)",
                titles.Count, todo.Count, options.MaxConcurrency);

            int saved = 0, skipped = 0, finished = 0;
            var fileLock = new SemaphoreSlim(1, 1);
            await using var writer = new StreamWriter(cacheFile, append: true, Encoding.UTF8) { AutoFlush = true };

            var parallel = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, options.MaxConcurrency),
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(todo, parallel, async (id, token) =>
            {
                var song = await FetchSongAsync(id, token);

                await fileLock.WaitAsync(token);   // one writer at a time
                try
                {
                    if (song is null || song.Deleted || string.IsNullOrEmpty(song.Uuid))
                    {
                        await File.AppendAllTextAsync(skipFile, id + "\n", token);
                        Interlocked.Increment(ref skipped);
                    }
                    else
                    {
                        await writer.WriteLineAsync(SongPraiseCache.Serialize(song));
                        Interlocked.Increment(ref saved);
                    }
                }
                finally
                {
                    fileLock.Release();
                }

                var n = Interlocked.Increment(ref finished);
                if (n % 500 == 0) logger.LogInformation("Downloaded {Done}/{Total}", n, todo.Count);

                await Task.Delay(Volatile.Read(ref requestDelayMs), token);
            });

            return new DownloadSummary(saved, skipped);
        }

        private async Task<List<SongPraiseTitle>> GetTitleListAsync(CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(5));   // it's a big file
            return await http.GetFromJsonAsync<List<SongPraiseTitle>>(
                $"{options.BaseUrl}/songTitles", SongPraiseCache.Json, cts.Token) ?? new();
        }

        // null = permanently unavailable (404 / unreadable). Throws if the server keeps failing.
        private async Task<SongPraiseSong?> FetchSongAsync(string id, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                await WaitIfPausedAsync(ct);

                TimeSpan? retryAfter = null;
                var rateLimited = false;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(options.RequestTimeout);

                    using var response = await http.GetAsync(
                        $"{options.BaseUrl}/song/{Uri.EscapeDataString(id)}", cts.Token);

                    if (response.StatusCode == HttpStatusCode.NotFound) return null;

                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadFromJsonAsync<SongPraiseSong>(
                            SongPraiseCache.Json, cts.Token);

                    if (!IsTransient(response.StatusCode))
                        throw new HttpRequestException(
                            $"HTTP {(int)response.StatusCode} for song {id}", null, response.StatusCode);

                    rateLimited = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
                    retryAfter = response.Headers.RetryAfter?.Delta;
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Could not parse song {Id}, skipping", id);
                    return null;
                }
                catch (Exception ex) when (
                    (ex is HttpRequestException { StatusCode: null } || ex is IOException || ex is OperationCanceledException)
                    && !ct.IsCancellationRequested)
                {
                    logger.LogWarning("Network problem on song {Id}: {Message}", id, ex.Message);
                }

                if (attempt >= options.MaxRetries)
                    throw new HttpRequestException($"Giving up on song {id} after {attempt + 1} attempts");

                var wait = retryAfter ?? TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt + 1)));
                logger.LogWarning("Retrying song {Id} in {Wait}s (attempt {Attempt})", id, wait.TotalSeconds, attempt + 1);

                if (rateLimited)
                {
                    // everyone waits, and only the first worker to notice slows the pace down
                    if (DateTime.UtcNow.Ticks >= Interlocked.Read(ref pauseUntilTicks)) SlowDown();
                    PauseAll(wait);
                }
                else
                {
                    await Task.Delay(wait, ct);
                }
            }
        }

        private async Task WaitIfPausedAsync(CancellationToken ct)
        {
            var wait = new TimeSpan(Interlocked.Read(ref pauseUntilTicks) - DateTime.UtcNow.Ticks);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        }

        private void PauseAll(TimeSpan duration)
        {
            var until = DateTime.UtcNow.Add(duration).Ticks;
            long current;
            do
            {
                current = Interlocked.Read(ref pauseUntilTicks);
                if (until <= current) return;
            }
            while (Interlocked.CompareExchange(ref pauseUntilTicks, until, current) != current);
        }

        private void SlowDown()
        {
            var next = Math.Min(Math.Max(Volatile.Read(ref requestDelayMs), 50) * 2, 5_000);
            Interlocked.Exchange(ref requestDelayMs, next);
            logger.LogWarning("Rate limited: all workers pausing, per-request delay now {Delay} ms", next);
        }

        private static bool IsTransient(HttpStatusCode code) =>
            code is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)code >= 500;
    }
}
