using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Models.Songs
{
    public static class LyricExtension
    {
        public static string AddHighlight(this string lyric, string snippet)
        {
            return lyric.Replace("<b>", $"<span class=\"text-orange group-hover:text-blue\">").Replace("</b>", "</span>");
        }

        public static string RemoveHighlight(this string lyric)
        {
            return lyric.Replace("<span class=\"text-orange\">", "").Replace("</span>", "").Replace("<span class=\"text-orange group-hover:text-white_light\">", "");
        }
    }
}
