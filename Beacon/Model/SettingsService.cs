using AsyncAwaitBestPractices;
using SQLite;
namespace Beacon.Model
{
    public class SettingsService
    {
        private static SQLiteAsyncConnection _dbConnection;

        public Settings settings = new Settings();

		public SettingsService()
		{
			_dbConnection = new SQLiteAsyncConnection(Constants.BeaconDbPath, Constants.Flags);
			_dbConnection.CreateTableAsync<Settings>();
            Initial().SafeFireAndForget();
		}

        public async Task Initial()
        {
            var count = await _dbConnection.Table<Settings>().CountAsync();
            if (count > 0)
            {
                await Load();
            }
            else
            {
                await _dbConnection.InsertAsync(settings);
            }
        }

        public async Task Save()
        {
            await _dbConnection.UpdateAsync(settings);
        }

        public async Task Load()
        {
            settings = await _dbConnection.Table<Settings>().FirstAsync();
        }


    }
}
