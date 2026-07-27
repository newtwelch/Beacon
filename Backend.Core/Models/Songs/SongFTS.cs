using SQLite;

namespace Backend.Core.Models.Songs
{
    [Table("SongFts")]
    public class SongFTS
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // FTS fields
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string LyricText { get; set; } = "";
        public string Tags { get; set; } = "";
    }
}
