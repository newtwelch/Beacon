using AsyncAwaitBestPractices;
using SQLite;

namespace Beacon.Model.Songs
{
    public class SongService : ISongService
    {
        private SQLiteAsyncConnection dbConnection;

        public SongService()
        {
            dbConnection = new SQLiteAsyncConnection(Constants.SongDbPath, Constants.Flags);
            //CreateTables().SafeFireAndForget();
        }

        public async Task<List<Song>> GetAllAsync()
        {
            return await dbConnection.QueryAsync<Song>($"SELECT Id, Title, Author, InQueue FROM Song WHERE Language = 'English' OR Language = 'Filipino'");
        }

		public async Task<int> GetCountAsync()
		{
			return await dbConnection.Table<Song>().CountAsync();
		}

		//    private SQLiteAsyncConnection dbConnection;

		//    public SongService()
		//    {
		//        dbConnection = new SQLiteAsyncConnection(Constants.SongDbPath, Constants.Flags);
		//        CreateTables().SafeFireAndForget();
		//    }

		//    public async Task DeleteAsync(Song song) => await dbConnection.DeleteAsync(song).ConfigureAwait(false);

		//    public async Task CreateTables()
		//    {
		//        try
		//        {
		//            await dbConnection.CreateTableAsync<Song>();
		//            await dbConnection.ExecuteAsync("CREATE VIRTUAL TABLE IF NOT EXISTS SongFts USING Fts5 (Id, Title, Author, LyricText, Tags, InQueue)");
		//            //dbConnection.ExecuteAsync("INSERT INTO SongFts(Id, Title, Author, LyricText, Tags, InQueue) SELECT Id, Title, Author, LyricText, Tags, InQueue FROM Song");
		//            await dbConnection.ExecuteAsync("CREATE TRIGGER insert_songFts after INSERT on Song begin INSERT INTO songFts (Id, Title, Author, LyricText, Tags, InQueue) VALUES (NEW.Id, NEW.Title, NEW.Author, NEW.LyricText, NEW.Tags, NEW.InQueue); end;");
		//            await dbConnection.ExecuteAsync("CREATE TRIGGER update_songFts after UPDATE on Song begin UPDATE songFts SET Title = NEW.Title, Author = NEW.Author, LyricText = NEW.LyricText, Tags = NEW.Tags, InQueue = NEW.InQueue WHERE ID = NEW.ID; end;");
		//            await dbConnection.ExecuteAsync("CREATE TRIGGER delete_songFts after DELETE on Song begin DELETE FROM songFts WHERE Id = OLD.Id; end;");

		//            await dbConnection.ExecuteAsync("VACUUM");
		//        }
		//        catch (Exception ex)
		//        {
		//            var t = ex;
		//        }

		//    }

		//    public async Task<Song> GetAsync(int id) => await dbConnection.Table<Song>().FirstOrDefaultAsync(song => song.Id == id).ConfigureAwait(false);
		//    public async Task UpdateAsync(Song song) => await dbConnection.UpdateAsync(song).ConfigureAwait(false);
		//    public async Task AddAsync(Song song) => await dbConnection.InsertAsync(song).ConfigureAwait(false);
		//    public async Task<int> GetLastNumberAsync()
		//    {
		//        var song = await dbConnection.FindWithQueryAsync<Song>("SELECT * FROM Song WHERE Number = (SELECT MAX(Number) FROM Song)");
		//        return song.Number;
		//    }
		//    public async Task<int> GetLastQueueOrderAsync()
		//    {
		//        var song = await dbConnection.FindWithQueryAsync<Song>("SELECT * FROM Song WHERE QueueOrder = (SELECT MAX(QueueOrder) FROM Song)");
		//        return song.QueueOrder;
		//    }

		//    public async Task<List<Song>> QueryTitleAsync(string searchTerm)
		//    {
		//        searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* + ");
		//        var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, HIGHLIGHT(SongFts, 1, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') AS Title, Author, InQueue FROM SongFts WHERE Title MATCH '{searchTerm}*' ORDER BY rank DESC").ConfigureAwait(false);
		//        return songsQueried;
		//    }

		//    public async Task<List<Song>> QueryAuthorAsync(string searchTerm)
		//    {
		//        searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* + ");
		//        var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, HIGHLIGHT(SongFts, 2, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') AS Author, InQueue FROM SongFts WHERE Author MATCH '{searchTerm}*' ORDER BY rank DESC").ConfigureAwait(false);
		//        return songsQueried;
		//    }

		//    public async Task<List<Song>> QueryLyricAsync(string searchTerm)
		//    {
		//        searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* + ");
		//        var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, SNIPPET(SongFts, 3, '<b>', '</b>', '', 12) AS LyricText, InQueue FROM SongFts WHERE LyricText MATCH '{searchTerm}*' ORDER BY rank").ConfigureAwait(false);
		//        return songsQueried;
		//    }

		//    public async Task<List<Song>> QueryTagsAsync(string searchTerm)
		//    {
		//        searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* + ");
		//        var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, HIGHLIGHT(SongFts, 4, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') AS Tags, InQueue FROM SongFts WHERE Tags MATCH '{searchTerm}*' ORDER BY rank DESC").ConfigureAwait(false);
		//        return songsQueried;
		//    }

		//    public async Task<List<Song>> GetAllAsync() => await dbConnection.QueryAsync<Song>($"SELECT Id, Title, Author, InQueue FROM Song WHERE Language = 'English' OR Language = 'Filipino'").ConfigureAwait(false);
		//    public async Task<List<Song>> GetQueueAsync() => await dbConnection.QueryAsync<Song>($"SELECT Id, Title, Author, InQueue FROM Song WHERE InQueue = true ORDER BY QueueOrder DESC").ConfigureAwait(false);

		//    public async Task<List<Song>> GetLanguagesOfSongAsync(int Number)
		//    {
		//        var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Language FROM Song WHERE Number = {Number}").ConfigureAwait(false);
		//        return songsQueried;
		//    }

	}

    public interface ISongService
    {
    //    public Task<Song> GetAsync(int id);
        public Task<List<Song>> GetAllAsync();
        public Task<int> GetCountAsync();

    //    public Task<List<Song>> GetQueueAsync();
    //    public Task<List<Song>> GetLanguagesOfSongAsync(int Number);
    //    public Task<List<Song>> QueryTitleAsync(string searchTerm);
    //    public Task<List<Song>> QueryAuthorAsync(string searchTerm);
    //    public Task<List<Song>> QueryLyricAsync(string searchTerm);
    //    public Task<List<Song>> QueryTagsAsync(string searchTerm);
    //    public Task<int> GetLastNumberAsync();
    //    public Task<int> GetLastQueueOrderAsync();
    //    public Task DeleteAsync(Song song);
    //    public Task AddAsync(Song song);
    //    public Task UpdateAsync(Song song);
    }
}
