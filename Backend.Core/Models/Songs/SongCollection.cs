using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Models.Songs
{
    public class SongCollection
    {
        public byte Source { get; set; }               // 0 = Beacon, 1 = SongPraise
        public string SourceName { get; set; } = "";   // "Beacon Songs" | "SongPraise"
        public string Language { get; set; } = "";
        public int Count { get; set; }

        public string GetKey() => $"{Source}:{Language}";
    }
}
