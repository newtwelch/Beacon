using Backend.Core.Models.Songs;
using Backend.Core.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Core.Models.SongPraise
{
    public static class SongLinker
    {
        // Returns, for each song (same order as the input), the index of its version group.
        public static int[] Group(IReadOnlyList<SongPraiseSong> songs, int maxGroupSize, ILogger? logger = null)
        {
            var mentions = new Dictionary<string, int>();
            foreach (var s in songs)
                foreach (var t in Tokens(s))
                    mentions[t] = mentions.GetValueOrDefault(t) + 1;

            var parent = new Dictionary<string, string>();

            string Find(string x)
            {
                parent.TryAdd(x, x);
                var root = x;
                while (parent[root] != root) root = parent[root];
                while (parent[x] != root) { var next = parent[x]; parent[x] = root; x = next; }
                return root;
            }

            for (var i = 0; i < songs.Count; i++)
            {
                var self = Find($"#{i}");
                foreach (var t in Tokens(songs[i]))
                {
                    if (mentions[t] > maxGroupSize) continue;   // shared junk value, ignore
                    var a = Find(self);
                    var b = Find(t);
                    if (a != b) parent[a] = b;
                }
            }

            var groupOf = new int[songs.Count];
            var ids = new Dictionary<string, int>();
            for (var i = 0; i < songs.Count; i++)
            {
                var root = Find($"#{i}");
                if (!ids.TryGetValue(root, out var g)) ids[root] = g = ids.Count;
                groupOf[i] = g;
            }

            var sizes = groupOf.GroupBy(g => g).Select(x => x.Count()).ToList();
            logger?.LogInformation("{Songs} songs in {Groups} groups; {Linked} link several versions (largest: {Max})",
                songs.Count, sizes.Count, sizes.Count(s => s > 1), sizes.Max());

            return groupOf;
        }

        private static IEnumerable<string> Tokens(SongPraiseSong s)
        {
            var set = new HashSet<string>();
            foreach (var t in new[] { s.Uuid, s.VersionGroup, s.BackUpSongId })
                if (!string.IsNullOrWhiteSpace(t)) set.Add(t.Trim());
            return set;
        }
    }

    public sealed class SongPraiseImportOptions
    {
        public int MaxLineLength { get; init; } = 45;       // "Amazing Grace, how sweet the sound" is 34
        public int MaxLinkedVersions { get; init; } = 25;
        public bool SkipExistingTitles { get; init; } = false;   // same title + language already in Beacon
        public int BatchSize { get; init; } = 500;
    }

    public sealed record ImportSummary(
        int Imported, int SkippedExisting, int SkippedDuplicates, int SkippedEmpty, int NumbersAfter);

    public sealed class SongPraiseImporter
    {
        private readonly SongService songs;
        private readonly ILogger<SongPraiseImporter> logger;

        public SongPraiseImporter(SongService songs, ILogger<SongPraiseImporter> logger)
        {
            this.songs = songs;
            this.logger = logger;
        }

        public async Task<ImportSummary> ImportAsync(string cacheFile, SongPraiseImportOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new SongPraiseImportOptions();
            await songs.Ready;

            var source = SongPraiseCache.ReadAll(cacheFile)
                .Where(s => !s.Deleted && !string.IsNullOrWhiteSpace(s.Title))
                .DistinctBy(s => s.Uuid)
                .ToList();

            var groupOf = SongLinker.Group(source, options.MaxLinkedVersions, logger);

            var max = await songs.GetHighestNumberAsync();
            if (!max.Success) throw new InvalidOperationException(max.ErrorMessage);
            var startNumber = max.Data;
            var nextNumber = startNumber;

            var existing = new HashSet<string>();
            if (options.SkipExistingTitles)
            {
                var keys = await songs.GetTitleLanguageKeysAsync();
                if (!keys.Success) throw new InvalidOperationException(keys.ErrorMessage);
                existing = keys.Data ?? existing;
            }

            var numberOfGroup = new Dictionary<int, int>();
            var seen = new HashSet<string>();
            var unmapped = new HashSet<int>();
            var batch = new List<Song>(options.BatchSize);
            int imported = 0, skippedExisting = 0, skippedDupes = 0, skippedEmpty = 0;

            async Task FlushAsync()
            {
                if (batch.Count == 0) return;
                var result = await songs.AddRangeAsync(batch);
                if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
                imported += batch.Count;
                logger.LogInformation("Imported {Done}/{Total}", imported, source.Count);
                batch.Clear();
            }

            for (var i = 0; i < source.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var src = source[i];

                var converted = SongPraiseConverter.ToBeacon(src, options.MaxLineLength);
                if (converted is null) { skippedEmpty++; continue; }
                unmapped.UnionWith(converted.UnmappedTypes);

                var title = src.Title.Trim();
                var language = string.IsNullOrWhiteSpace(src.LanguageDTO?.EnglishName)
                    ? "English"
                    : src.LanguageDTO!.EnglishName!.Trim();
                var key = SongService.SongKey(title, language);

                if (existing.Contains(key)) { skippedExisting++; continue; }
                // exact re-uploads of the same song are dropped; different lyrics under one title are kept
                if (!seen.Add(key + "|" + Fingerprint(converted.LyricText))) { skippedDupes++; continue; }

                if (!numberOfGroup.TryGetValue(groupOf[i], out var number))
                    numberOfGroup[groupOf[i]] = number = ++nextNumber;

                batch.Add(new Song
                {
                    Number = number,
                    Title = title,
                    Author = "",   // not in the API sample; createdByEmail is the uploader, so not used
                    LyricText = converted.LyricText,
                    Sequence = converted.Sequence,
                    Language = language,
                });

                if (batch.Count >= options.BatchSize) await FlushAsync();
            }

            await FlushAsync();

            if (unmapped.Count > 0)
                logger.LogWarning("Verse types not in TypeMap (imported as plain verses): {Types}",
                    string.Join(", ", unmapped.OrderBy(t => t)));

            logger.LogInformation("New songs use Numbers {From}..{To}", startNumber + 1, nextNumber);
            return new ImportSummary(imported, skippedExisting, skippedDupes, skippedEmpty, nextNumber);
        }

        private static string Fingerprint(string lyrics)
        {
            var letters = new string(lyrics.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(letters)));
        }
    }
}
