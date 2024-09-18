using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beacon.Model.Songs
{
    public class Lyric
    {
        public int Id { get; set; }
        public string Line { get; set; } = "";
        public string Text { get; set; } = "";
        public LyricType Type { get; set; }
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
