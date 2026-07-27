using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backend.Core.Services
{
    public static class VersePortioner
    {
        private static readonly Regex HtmlRegex =
            new("<.*?>", RegexOptions.Compiled);

        public static IReadOnlyList<string> Split(string text)
        {
            text = StripHtml(text);

            var results = new List<string>();

            foreach (var portion in SmartSplit(text))
                results.Add(portion);

            if (results.Count == 0 && !string.IsNullOrWhiteSpace(text))
                results.Add(text);

            return results;
        }

        private static IEnumerable<string> SmartSplit(string text)
        {
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var sentences = Regex.Split(
                text,
                @"(?<=[.;:!?])\s+");

            foreach (var sentence in sentences)
            {
                foreach (var piece in SplitLongSentence(sentence))
                    yield return piece.Trim();
            }
        }

        private static IEnumerable<string> SplitLongSentence(string sentence)
        {
            const int PreferredWords = 12;
            const int MaxWords = 18;

            var words = sentence.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= MaxWords)
            {
                yield return sentence;
                yield break;
            }

            var current = new StringBuilder();
            var count = 0;

            foreach (var word in words)
            {
                current.Append(word);
                current.Append(' ');

                count++;

                if (count >= PreferredWords &&
                    (word.EndsWith(",") ||
                     word.EndsWith(";")))
                {
                    yield return current.ToString().Trim();

                    current.Clear();
                    count = 0;
                    continue;
                }

                if (count >= MaxWords)
                {
                    yield return current.ToString().Trim();

                    current.Clear();
                    count = 0;
                }
            }

            if (current.Length > 0)
                yield return current.ToString().Trim();
        }

        private static string StripHtml(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return HtmlRegex.Replace(text, "");
        }
    }
}
