using SQLite;

namespace Backend.Core.Models.Songs
{
    [Table("Song")]
    public class Song
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int Number { get; set; } = 0;

        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string LyricText { get; set; } = "";  // Raw text, newline-separated
        public string Sequence { get; set; } = "auto"; // "auto" lets parser decide
        public string Language { get; set; } = "ENGLISH";
        public string Tags { get; set; } = "";
        public string Key { get; set; } = "";

        public int QueueOrder { get; set; }
        public bool InQueue { get; set; } = false;

        public Song() { }

        public Song(string title, string author)
        {
            Title = title;
            Author = author;
        }

        public Song(int number, string title, string author)
        {
            Number = number;
            Title = title;
            Author = author;
        }

        // Copy constructor
        public Song(Song song)
        {
            Id = song.Id;
            Number = song.Number;
            Title = song.Title;
            Author = song.Author;
            LyricText = song.LyricText;
            Sequence = song.Sequence;
            Language = song.Language;
            Tags = song.Tags;
            Key = song.Key;
            QueueOrder = song.QueueOrder;
            InQueue = song.InQueue;
        }

        public Song Clone()
        {
            var clonedSong = new Song();

            clonedSong.Id = Id;
            clonedSong.Number = Number;
            clonedSong.Title = Title;
            clonedSong.Author = Author;
            clonedSong.LyricText = LyricText;
            clonedSong.Sequence = Sequence;
            clonedSong.Language = Language;
            clonedSong.Tags = Tags;
            clonedSong.Key = Key;
            clonedSong.QueueOrder = QueueOrder;
            clonedSong.InQueue = InQueue;

            return clonedSong;
        }
    }
}
