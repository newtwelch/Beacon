using AsyncAwaitBestPractices;
using SQLite;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace Beacon.Model.Songs
{
    public class SongService : ISongService
    {
        private SQLiteAsyncConnection dbConnection;

        public SongService()
        {
            dbConnection = new SQLiteAsyncConnection(Constants.SongDbPath, Constants.Flags);
            CreateTables().SafeFireAndForget();
        }

		public async Task CreateTables()
		{
		    try
		    {
		        await dbConnection.CreateTableAsync<Song>();
		        await dbConnection.ExecuteAsync("CREATE VIRTUAL TABLE IF NOT EXISTS SongFts USING Fts5 (Id, Title, Author, LyricText, Tags, InQueue)");
		        //dbConnection.ExecuteAsync("INSERT INTO SongFts(Id, Title, Author, LyricText, Tags, InQueue) SELECT Id, Title, Author, LyricText, Tags, InQueue FROM Song");
		        await dbConnection.ExecuteAsync("CREATE TRIGGER insert_songFts after INSERT on Song begin INSERT INTO songFts (Id, Title, Author, LyricText, Tags, InQueue) VALUES (NEW.Id, NEW.Title, NEW.Author, NEW.LyricText, NEW.Tags, NEW.InQueue); end;");
		        await dbConnection.ExecuteAsync("CREATE TRIGGER update_songFts after UPDATE on Song begin UPDATE songFts SET Title = NEW.Title, Author = NEW.Author, LyricText = NEW.LyricText, Tags = NEW.Tags, InQueue = NEW.InQueue WHERE ID = NEW.ID; end;");
		        await dbConnection.ExecuteAsync("CREATE TRIGGER delete_songFts after DELETE on Song begin DELETE FROM songFts WHERE Id = OLD.Id; end;");

		        await dbConnection.ExecuteAsync("VACUUM");
		    }
		    catch (Exception ex)
		    {
		        var test = ex;
		    }

		}

		public async Task<Song> GetAsync(int id) => await dbConnection.Table<Song>().FirstOrDefaultAsync(song => song.Id == id);
		public async Task<List<Song>> GetQueueAsync() => await dbConnection.QueryAsync<Song>($"SELECT Id, Title, Author, InQueue FROM Song WHERE InQueue = true ORDER BY QueueOrder DESC");
		public async Task<List<Song>> GetAllAsync()
        {
            return await dbConnection.QueryAsync<Song>($"SELECT Id, Title, Author, InQueue FROM Song WHERE Language = 'English' OR Language = 'Filipino' OR Language = 'LANGUAGE'");
        }

		public async Task<int> GetCountAsync()
		{
			return await dbConnection.Table<Song>().CountAsync();
		}
		public async Task<int> GetHighestQueueOrderAsync()
		{
			return await dbConnection.ExecuteScalarAsync<int>("SELECT MAX(QueueOrder) FROM Song");
		}

		public async Task<List<Song>> GetLanguagesAsync(int Number)
		{
		    return await dbConnection.QueryAsync<Song>($"SELECT * FROM Song WHERE Number = {Number}");
		}

		public async Task<List<Song>> QueryTitleAsync(string searchText)
		{
		    searchText = searchText.Trim().Replace(" ", "* + ").Replace("'", " ");
		    var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, HIGHLIGHT(SongFts, 1, '<span class=\"text-orange group-hover:text-accented-on-primary\">', '</span>') AS Title, Author, InQueue " +
																   $"FROM SongFts WHERE Title MATCH '{searchText}*' ORDER BY rank DESC LIMIT 100");
		    return songsQueried;
		}
        public async Task<List<Song>> QueryAuthorAsync(string searchText)
        {
            searchText = searchText.Trim().Replace(" ", "* + ").Replace("'", " ");
            var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, HIGHLIGHT(SongFts, 2, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') AS Author, InQueue " +
																   $"FROM SongFts WHERE Author MATCH '{searchText}*' ORDER BY rank DESC LIMIT 100");
            return songsQueried;
        }
        public async Task<List<Song>> QueryLyricAsync(string searchText)
        {
            searchText = searchText.Trim().Replace(" ", "* + ").Replace("'", " ");
            var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, SNIPPET(SongFts, 3, '<b>', '</b>', '', 12) AS LyricText, InQueue " +
																   $"FROM SongFts WHERE LyricText MATCH '{searchText}*' ORDER BY rank LIMIT 100");
			return songsQueried;
		}
		public async Task<List<Song>> QueryTagAsync(string searchText)
		{
			searchText = searchText.Trim().Replace(" ", "* + ").Replace("'", " ");
			var songsQueried = await dbConnection.QueryAsync<Song>($"SELECT Id, Title, HIGHLIGHT(SongFts, 4, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') AS Tags, InQueue " +
																   $"FROM SongFts WHERE Tags MATCH '{searchText}*' ORDER BY rank DESC LIMIT 100").ConfigureAwait(false);
		    return songsQueried;
		}

		public async Task<Song> AddAsync(Song song)
		{
			var newSong = new Song();

			bool isALanguage = song.Number > 0;
			var AssignNewNumber = "(SELECT MAX(Number) FROM Song) + 1";

			newSong = await dbConnection.FindWithQueryAsync<Song>("INSERT INTO Song(Number, Title, Author, Language) " +
				$"VALUES({(isALanguage ? song.Number : AssignNewNumber)}, '{song.Title}', '{song.Author}', '{song.Language}') RETURNING *");

			return newSong;
		}
		public async Task UpdateAsync(Song song) => await dbConnection.UpdateAsync(song); 
		public async Task DeleteAsync(Song song) => await dbConnection.DeleteAsync(song);


	}

	public interface ISongService
    {
        public Task<Song> GetAsync(int id);
        public Task<Song> AddAsync(Song song);
        public Task UpdateAsync(Song song);
        public Task DeleteAsync(Song song);
        public Task<List<Song>> GetAllAsync();
		public Task<List<Song>> GetQueueAsync();
        public Task<int> GetCountAsync();
        public Task<int> GetHighestQueueOrderAsync();
		public Task<List<Song>> GetLanguagesAsync(int Number);
		public Task<List<Song>> QueryTitleAsync(string searchText);
		public Task<List<Song>> QueryLyricAsync(string searchText);
		public Task<List<Song>> QueryAuthorAsync(string searchText);
		public Task<List<Song>> QueryTagAsync(string searchText);

	}
}
