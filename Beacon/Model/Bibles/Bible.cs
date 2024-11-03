using SQLite;
using System.Text.Json.Serialization;

namespace Beacon.Model.Bibles
{
    public class BeaconVerse
    {
        public int Book { get; set; }
        public string BookName { get; set; } = "";
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Text { get; set; } = "";
    }

    [Table("Bibles")]
    public class Bible
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [JsonPropertyName("language")]
        public string Language { get; set; } = "";
        [JsonPropertyName("abbreviation")]
        public string Abbreviation { get; set; } = "";
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("books")]
        [Ignore]
        public List<Book> Books { get; set; } = new List<Book>();
    }

    public class Book
    {
        [JsonPropertyName("nr")]
        public int Id { get; set; }
        [JsonPropertyName("name")] 
        public string Name { get; set; } = "";
        [JsonPropertyName("chapters")] 
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
    }

    public class Chapter
    {
        [JsonPropertyName("chapter")] 
        [Column("Chapter")]
        public int Id { get; set; }
        [JsonPropertyName("verses")] 
        public List<Verse> Verses { get; set; } = new List<Verse>();
    }

    public class Verse
    {
        [JsonPropertyName("chapter")] 
        public int Chapter { get; set; }
        [JsonPropertyName("verse")] 
        public int Id { get; set; }
        [JsonPropertyName("name")] 
        public string Name { get; set; } = "";
        [JsonPropertyName("text")] 
        public string Text { get; set; } = "";
    }
}
