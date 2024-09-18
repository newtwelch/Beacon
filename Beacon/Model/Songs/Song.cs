using SQLite;

namespace Beacon.Model.Songs
{
    [Table("Song")]
    public class Song
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Number { get; set; } = 0;
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string LyricText { get; set; } = "";
        public string Sequence { get; set; } = "";
        public string Language { get; set; } = "";
        public string Tags { get; set; } = "";
        public int QueueOrder { get; set; }
        public bool InQueue { get; set;} = false;

        public Song() { }

        /// <summary>
        /// Default values are created
        /// </summary>
        /// <param name="_title"></param>
        /// <param name="_author"></param>
        public Song(string _title, string _author)
        {
            Title = _title;
            Author = _author;
            Language = "ENGLISH";
            LyricText = "";
            Sequence = "auto";
            Tags = "";
        }

        /// <summary>
        /// Requires to include Number which will determine song pairs or languages
        /// </summary>
        /// <param name="_number"></param>
        /// <param name="_title"></param>
        /// <param name="_author"></param>
        public Song(int _number, string _title, string _author)
        {
            Language = "Filipino";
            Title = _title;
            Number = _number;
            Author = _author;
            Sequence = "auto";
            Tags = "";
        }

        /// <summary>
        /// Create a duplicate due to https://stackoverflow.com/questions/6569486/creating-a-copy-of-an-object-in-c-sharp
        /// </summary>
        /// <param name="_song"></param>
        public Song(Song _song)
        {
            Id = _song.Id;
            Number = _song.Number;
            Language = _song.Language;
            Title = _song.Title;
            Author = _song.Author;
            LyricText = _song.LyricText;
            Sequence = _song.Sequence;
            Tags = _song.Tags;
        }
    }

    [Table("SongFts")]
    public class SongFTS : Song
    {

    }

    public enum SearchMode
    {
        Title,
        Lyric,
        Author,
        Tag
    }

}
