using SQLite;

namespace Backend.API.Wrapper
{
    public class SongDbConnection : SQLiteAsyncConnection
    {
        public SongDbConnection(string path) : base(path) { }
    }

    public class BibleDbConnection : SQLiteAsyncConnection
    {
        public BibleDbConnection(string path) : base(path) { }
    }

    public class BeaconDbConnection : SQLiteAsyncConnection
    {
        public BeaconDbConnection(string path) : base(path) { }
    }
}
