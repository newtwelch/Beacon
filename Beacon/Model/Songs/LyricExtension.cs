using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Beacon.Model.Songs
{
    public static class LyricExtension
    {
        public static string AddHighlight(this string lyric, string snippet)
        {
            var startIndex = snippet.IndexOf("<b>");
            var endIndex = snippet.IndexOf("</b>") - startIndex - "<b>".Length;
            var match = snippet.Substring(startIndex + "<b>".Length, endIndex);
            return lyric.Replace(match, $"<span class=\"text-orange\">{match}</span>");
        }

        public static string RemoveHighlight(this string lyric)
        {
            return lyric.Replace("<span class=\"text-orange\">", "").Replace("</span>", "").Replace("<span class=\"text-orange group-hover:text-white_light\">", "");
        }
    }
}
