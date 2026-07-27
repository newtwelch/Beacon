using AsyncAwaitBestPractices;
using Backend.Core.Models.Songs;
using Microsoft.Extensions.Logging;
using SQLite;
using static SQLite.SQLite3;

namespace Backend.Core.Services
{
    public class SongService
    {
        private readonly SQLiteAsyncConnection dbConnection;
        private readonly ILogger<SongService> logger;

        public SongService(SQLiteAsyncConnection _dbConnection, ILogger<SongService> _logger)
        {
            dbConnection = _dbConnection;
            logger = _logger;

            EnsureSchemaAsync().SafeFireAndForget();
        }

        private async Task EnsureSchemaAsync()
        {
            try
            {
                await dbConnection.CreateTableAsync<Song>();

                await dbConnection.ExecuteAsync(@"
                CREATE VIRTUAL TABLE IF NOT EXISTS SongFts
                USING Fts5 (Id UNINDEXED, Title, Author, LyricText, Tags, InQueue UNINDEXED)");

                // triggers
                await dbConnection.ExecuteAsync(@"
                CREATE TRIGGER IF NOT EXISTS insert_songFts AFTER INSERT ON Song
                BEGIN
                    INSERT INTO SongFts (Id, Title, Author, LyricText, Tags, InQueue)
                    VALUES (NEW.Id, NEW.Title, NEW.Author, NEW.LyricText, NEW.Tags, NEW.InQueue);
                END;");

                await dbConnection.ExecuteAsync(@"
                CREATE TRIGGER IF NOT EXISTS update_songFts AFTER UPDATE ON Song
                BEGIN
                    UPDATE SongFts
                    SET Title = NEW.Title,
                        Author = NEW.Author,
                        LyricText = NEW.LyricText,
                        Tags = NEW.Tags,
                        InQueue = NEW.InQueue
                    WHERE Id = NEW.Id;
                END;");

                await dbConnection.ExecuteAsync(@"
                CREATE TRIGGER IF NOT EXISTS delete_songFts AFTER DELETE ON Song
                BEGIN
                    DELETE FROM SongFts WHERE Id = OLD.Id;
                END;");

                logger.LogInformation("Database tables and triggers ensured.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ensuring database schema.");
            }
        }


        public async Task<Result<Song>> GetAsync(int id) =>
            await ExecuteAsync(() =>
                dbConnection.Table<Song>().FirstOrDefaultAsync(s => s.Id == id));

        public async Task<Result<int>> GetCountAsync() =>
            await ExecuteAsync(() => dbConnection.Table<Song>().CountAsync());

        public async Task<Result<List<Song>>> GetLanguagesAsync(int number) =>
            await ExecuteAsync(() =>
                dbConnection.QueryAsync<Song>("SELECT * FROM Song WHERE Number = ?", number));

        public async Task<Result<List<Song>>> GetAllAsync() =>
            await ExecuteAsync(() =>
                dbConnection.QueryAsync<Song>(
                    "SELECT Id, Title, Author, InQueue FROM Song WHERE Language = 'English' ORDER BY Title ASC"));

        // --------------------
        //  Queues
        // --------------------
        public async Task<Result<int>> GetHighestQueueOrderAsync() =>
            await ExecuteAsync(() =>
                dbConnection.ExecuteScalarAsync<int>("SELECT IFNULL(MAX(QueueOrder),0) FROM Song"));

        public async Task<Result<List<Song>>> GetQueueAsync() =>
            await ExecuteAsync(() => dbConnection.QueryAsync<Song>(
                "SELECT Id, Title, Author, QueueOrder, InQueue FROM Song WHERE InQueue = 1 ORDER BY QueueOrder ASC"));

        public async Task<Result<bool>> ClearQueueAsync() =>
            await ExecuteAsync(async () =>
            {
                await dbConnection.ExecuteAsync(
                    "UPDATE Song SET InQueue = 0, QueueOrder = 0 WHERE InQueue = 1");

                logger.LogInformation("Cleared song queue");
                return true;
            });

        public async Task<Result<bool>> AddToQueueAsync(int songId) =>
            await ExecuteAsync(async () =>
            {
                var maxOrder = await GetHighestQueueOrderAsync();
                if (!maxOrder.Success)
                    throw new Exception(maxOrder.ErrorMessage);

                await dbConnection.ExecuteAsync(
                    "UPDATE Song SET InQueue = 1, QueueOrder = ? WHERE Id = ?",
                    maxOrder.Data + 1, songId);

                logger.LogInformation("Added song {SongId} to queue", songId);
                return true;
            });

        public async Task<Result<bool>> RemoveFromQueueAsync(int songId) =>
            await ExecuteAsync(async () =>
            {
                await dbConnection.ExecuteAsync(
                    "UPDATE Song SET InQueue = 0, QueueOrder = 0 WHERE Id = ?",
                    songId);

                logger.LogInformation("Removed song {SongId} from queue", songId);
                return true;
            });

        public async Task<Result<bool>> UpdateQueueOrderAsync(IEnumerable<(int SongId, int NewOrder)> updates) =>
            await ExecuteAsync(async () =>
            {
                // Build a single SQL statement with CASE expression for batch update
                var caseClauses = new List<string>();
                var parameters = new List<object>();

                foreach (var (songId, newOrder) in updates)
                {
                    caseClauses.Add("WHEN ? THEN ?");
                    parameters.Add(songId);
                    parameters.Add(newOrder);
                }

                var sql = $@"
                    UPDATE Song 
                    SET QueueOrder = CASE Id {string.Join(" ", caseClauses)} ELSE QueueOrder END
                    WHERE Id IN ({string.Join(",", updates.Select(u => u.SongId))})";

                await dbConnection.ExecuteAsync(sql, parameters.ToArray());

                logger.LogInformation("Updated queue order for {Count} songs", updates.Count());
                return true;
            });


        // --------------------
        //  Queries
        // --------------------
        public async Task<Result<List<Song>>> QueryTitleAsync(string searchText) =>
            await ExecuteAsync(() => dbConnection.QueryAsync<Song>(
                @"SELECT Id, HIGHLIGHT(SongFts, 1, '<span class=""text-orange group-hover:text-blue"">', '</span>') AS Title, Author, InQueue
                  FROM SongFts WHERE Title MATCH ? ORDER BY rank DESC LIMIT 100",
                $"{searchText.Trim().Replace(" ", "* + ").Replace("'", " ")}*"));

        public async Task<Result<List<Song>>> QueryAuthorAsync(string searchText) =>
            await ExecuteAsync(() => dbConnection.QueryAsync<Song>(
                @"SELECT Id, Title, HIGHLIGHT(SongFts, 2, '<span class=""text-orange group-hover:text-blue"">', '</span>') AS Author, InQueue
                  FROM SongFts WHERE Author MATCH ? ORDER BY rank DESC LIMIT 100",
                $"{searchText.Trim().Replace(" ", "* + ").Replace("'", " ")}*"));

        public async Task<Result<List<Song>>> QueryLyricAsync(string searchText) =>
            await ExecuteAsync(() => dbConnection.QueryAsync<Song>(
                @"SELECT Id, Title, SNIPPET(SongFts, 3, '<b>', '</b>', '', 12) AS LyricText, InQueue
                  FROM SongFts WHERE LyricText MATCH ? ORDER BY rank LIMIT 100",
                $"{searchText.Trim().Replace(" ", "* + ").Replace("'", " ")}*"));

        public async Task<Result<List<Song>>> QueryTagAsync(string searchText) =>
            await ExecuteAsync(() => dbConnection.QueryAsync<Song>(
                @"SELECT Id, Title, HIGHLIGHT(SongFts, 4, '<span class=""text-orange group-hover:text-blue"">', '</span>') AS Tags, InQueue
                  FROM SongFts WHERE Tags MATCH ? ORDER BY rank LIMIT 100",
                $"{searchText.Trim().Replace(" ", "* + ").Replace("'", " ")}*"));


        public async Task<Result<Song>> AddAsync(Song song) =>
            await ExecuteAsync(async () =>
            {
                var newSong = await dbConnection.FindWithQueryAsync<Song>(@"
                INSERT INTO Song(Number, Title, Author, Language)
                VALUES(
                CASE 
                    WHEN ? > 0 THEN ?
                    ELSE (SELECT IFNULL(MAX(Number),0) + 1 FROM Song)
                END,
                ?, ?, ?
                )
                RETURNING *
                ",
                song.Number,   // used in CASE
                song.Number,   // value if > 0
                song.Title,
                song.Author,
                song.Language);


                logger.LogInformation("Added song {Title} by {Author}", song.Title, song.Author);
                return newSong;
            });

        public async Task<Result<int>> UpdateAsync(Song song) =>
            await ExecuteAsync(() =>
            {
                logger.LogInformation("Updating song {Id}", song.Id);
                return dbConnection.UpdateAsync(song);
            });

        public async Task<Result<int>> DeleteAsync(Song song) =>
            await ExecuteAsync(() =>
            {
                logger.LogInformation("Deleting song {Id}", song.Id);
                return dbConnection.DeleteAsync(song);
            });

        private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                var result = await operation();
                return Result<T>.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database operation failed.");
                return Result<T>.Fail(ex.Message);
            }
        }
    }
}
