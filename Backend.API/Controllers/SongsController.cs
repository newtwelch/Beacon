using Backend.API.Wrapper;
using Backend.Core.Models.Songs;
using Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using SQLite;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Backend.API.Controllers
{
    [Route("songs")]
    [ApiController]
    public class SongsController : ControllerBase
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private static readonly byte[] NewLine = { (byte)'\n' };

        private readonly SongDbConnection beaconDb;
        private readonly SongPraiseDbConnection songPraiseDb;
        private readonly ILogger<SongsController> logger;

        private record SourceInfo(SQLiteAsyncConnection Db, string Name);

        private readonly Dictionary<byte, SourceInfo> sources;

        public SongsController(
            SongDbConnection _beaconDb,
            SongPraiseDbConnection _songPraiseDb,
            ILogger<SongsController> _logger)
        {
            beaconDb = _beaconDb;
            songPraiseDb = _songPraiseDb;
            logger = _logger;

            beaconDb.CreateTableAsync<Song>().Wait();
            songPraiseDb.CreateTableAsync<Song>().Wait();

            // Source id -> database. 0 = Beacon, 1 = SongPraise.
            sources = new()
            {
                [0] = new SourceInfo(beaconDb, "Beacon Songs"),
                [1] = new SourceInfo(songPraiseDb, "SongPraise"),
            };
        }

        // GET /songs/catalog
        [HttpGet("catalog")]
        public async Task<IActionResult> Catalog()
        {
            var result = new List<SongCollection>();

            foreach (var (id, src) in sources)
            {
                var rows = await src.Db.QueryAsync<SongCollection>(
                    "SELECT Language, COUNT(*) AS Count FROM Song " +
                    "GROUP BY Language ORDER BY Language");

                foreach (var row in rows)
                {
                    row.Source = id;
                    row.SourceName = src.Name;
                    result.Add(row);
                }
            }

            return Ok(result);
        }

        // GET /songs/1/english  -> NDJSON stream, one Song per line
        [HttpGet("{source:int}/{language}")]
        public async Task Download(byte source, string language, CancellationToken ct)
        {
            if (!sources.TryGetValue(source, out var src))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var songs = await src.Db.QueryAsync<Song>(
                "SELECT * FROM Song WHERE Language = ? COLLATE NOCASE ORDER BY Number, Id",
                language);

            if (songs.Count == 0)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Response.ContentType = "application/x-ndjson";
            Response.Headers["X-Total-Count"] = songs.Count.ToString();
            Response.Headers["X-Accel-Buffering"] = "no"; // stops nginx from buffering the whole response

            logger.LogInformation("Streaming {Count} songs for source {Source}/{Language}",
                songs.Count, source, language);

            try
            {
                var i = 0;
                foreach (var song in songs)
                {
                    song.Id = 0;            // the app's database assigns its own
                    song.QueueOrder = 0;
                    song.InQueue = false;

                    await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(song, Json), ct);
                    await Response.Body.WriteAsync(NewLine, ct);

                    if (++i % 50 == 0)
                        await Response.Body.FlushAsync(ct); // keeps the progress bar moving smoothly
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Client cancelled source {Source}/{Language} download", source, language);
            }
        }
    }
}
