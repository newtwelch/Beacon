namespace Backend.Core.Models.Songs
{
    public class Lyric
    {
        public int Id { get; set; }              // Position in parsed list
        public string Line { get; set; } = "";   // e.g. "Verse 1", "Chorus"
        public string Text { get; set; } = "";   // Actual text
        public LyricType Type { get; set; }      // Type for styling / display

        public Lyric() { }

        public Lyric(Lyric lyric)
        {
            Id = lyric.Id;
            Line = lyric.Line;
            Text = lyric.Text;
            Type = lyric.Type;
        }
    }

    public enum LyricType
    {
        Stanza,
        PreChorus,
        Chorus,
        ChorusII,
        ChorusIII,
        Bridge,
        BridgeII,
        BridgeIII
    }

}
