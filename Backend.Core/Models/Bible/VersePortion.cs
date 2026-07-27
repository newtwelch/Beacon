using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Models.Bible
{
    //CHATGPT CHATGPT GENERATED
    public class VersePortion
    {
        /// <summary>
        /// Parent verse.
        /// </summary>
        public BeaconVerse Verse { get; set; } = null!;

        /// <summary>
        /// Portion number within the verse (1-based).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Text shown on the projector.
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Original text before modifications/highlighting.
        /// </summary>
        public string OriginalText { get; set; } = "";

        public override string ToString() => Text;
    }
}
