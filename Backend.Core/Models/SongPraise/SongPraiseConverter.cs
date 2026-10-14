using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Models.SongPraise
{
    using System.Text.RegularExpressions;

    public enum StanzaKind { Verse, Chorus, PreChorus, Bridge }

    public sealed record ConvertedLyrics(string LyricText, string Sequence, IReadOnlyCollection<int> UnmappedTypes);

    public static class SongPraiseConverter
    {
        // Only 1 (verse) and 3 (chorus) are confirmed by your sample. Anything else is treated as a
        // verse and reported in the import log, so you can add bridge/pre-chorus types here.
        private static readonly Dictionary<int, StanzaKind> TypeMap = new()
        {
            [1] = StanzaKind.Verse,
            [2] = StanzaKind.PreChorus,
            [3] = StanzaKind.Chorus,
            [4] = StanzaKind.Bridge,
            [5] = StanzaKind.Verse,   // "ending": Beacon has none, so it stays a stanza
        };


        private sealed record Block(StanzaKind Kind, int Index, List<string> Lines);

        public static ConvertedLyrics? ToBeacon(SongPraiseSong song, int maxLineLength = 45)
        {
            var verses = song.SongVerseDTOS ?? new List<SongPraiseVerse>();
            var blocks = new List<Block?>(verses.Count);     // index-aligned with verseOrderList
            var unique = new Dictionary<string, Block>();    // kind + words -> block
            var distinct = new List<Block>();                // paragraphs, in order of first appearance
            var counters = new Dictionary<StanzaKind, int>();
            var unmapped = new HashSet<int>();

            foreach (var verse in verses)
            {
                var lines = FormatLines(verse.Text, maxLineLength);
                if (lines.Count == 0) { blocks.Add(null); continue; }

                StanzaKind kind;
                if (!TypeMap.TryGetValue(verse.Type, out kind))
                {
                    // type outside 1-5: fall back to the chorus flag and report it in the log
                    kind = verse.Chorus ? StanzaKind.Chorus : StanzaKind.Verse;
                    unmapped.Add(verse.Type);
                }

                // same kind + same words = same stanza (reuse its label); different words = next number
                var key = $"{kind}|{Normalize(lines)}";
                if (!unique.TryGetValue(key, out var block))
                {
                    counters[kind] = counters.GetValueOrDefault(kind) + 1;
                    block = new Block(kind, counters[kind], lines);
                    unique[key] = block;
                    distinct.Add(block);
                }
                blocks.Add(block);
            }

            if (distinct.Count == 0) return null;

            var lyricText = string.Join("\n\n", distinct.Select(b =>
            {
                var tag = Tag(b.Kind, b.Index);
                var body = string.Join("\n", b.Lines);
                return tag is null ? body : tag + "\n" + body;
            }));

            // play order -> "1, pc, c, 2, pc, c"; repeated blocks resolve to the same label
            var tokens = ReadOrder(song)
                .Where(i => i >= 0 && i < blocks.Count && blocks[i] is not null)
                .Select(i => Token(blocks[i]!.Kind, blocks[i]!.Index))
                .ToList();

            var sequence = tokens.Count > 0 ? string.Join(", ", tokens) : "auto";
            return new ConvertedLyrics(lyricText, sequence, unmapped);
        }
        private static string Normalize(IEnumerable<string> lines) =>
    new string(string.Concat(lines).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        // First occurrence is bare ("CHORUS", "c"), later ones are numbered ("CHORUS 2", "c2").
        private static string? Tag(StanzaKind kind, int n) => kind switch
        {
            StanzaKind.Chorus => n == 1 ? "CHORUS" : $"CHORUS {n}",
            StanzaKind.PreChorus => n == 1 ? "PRE-CHORUS" : $"PRE-CHORUS {n}",
            StanzaKind.Bridge => n == 1 ? "BRIDGE" : $"BRIDGE {n}",
            _ => null,                                    // verses stay untagged
        };

        private static string Token(StanzaKind kind, int n)
        {
            if (kind == StanzaKind.Verse) return n.ToString();
            var prefix = kind switch { StanzaKind.Chorus => "c", StanzaKind.PreChorus => "pc", _ => "b" };
            return n == 1 ? prefix : prefix + n;
        }

        private static IReadOnlyList<int> ReadOrder(SongPraiseSong song)
        {
            if (song.VerseOrderList is { Count: > 0 } list) return list;

            if (!string.IsNullOrWhiteSpace(song.VerseOrder))
                return song.VerseOrder
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var n) ? n : -1)
                    .ToList();

            return Array.Empty<int>();
        }

        // --------------------
        //  Line formatting
        // --------------------
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        // "Chorus", "(Verse 2)", "Bridge:" typed into the lyrics by uploaders
        private static readonly Regex LabelLine = new(
            @"^[\(\[]?\s*(verse|chorus|refrain|bridge|pre-?chorus|intro|outro|interlude)\s*\d*\s*[\)\]]?\s*[:.\-]?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static List<string> FormatLines(string? text, int maxLen)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var isFirst = true;
            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = Whitespace.Replace(raw, " ").Trim();
                if (line.Length == 0) continue;          // a blank line would start a new paragraph in Beacon

                var wasFirst = isFirst;
                isFirst = false;
                if (wasFirst && LabelLine.IsMatch(line)) continue;

                result.AddRange(SplitLongLine(line, maxLen));
            }
            return result;
        }

        private const string PauseChars = ",;:.!?—–";
        private const string WrapChars = "\"'“”‘’()[]";
        private static readonly char[] TrimChars = (PauseChars + WrapChars).ToCharArray();

        private static readonly HashSet<string> BreakBefore = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "but", "or", "nor", "yet", "for", "that", "who", "whom", "which", "when", "where",
        "while", "as", "till", "until", "because", "though", "since", "if", "then", "so"
    };

        private static readonly HashSet<string> NoBreakAfter = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "of", "to", "in", "on", "at", "by", "with", "from", "my", "thy", "thine",
        "his", "her", "our", "your", "their", "its", "and", "but", "or", "that", "who", "which",
        "as", "is", "are", "was", "be"
    };

        // Splits into the fewest balanced parts that fit maxLen, preferring commas/semicolons
        // and breaks before words like "and"/"but", and never stranding "the"/"of" at a line end.
        private static List<string> SplitLongLine(string line, int maxLen)
        {
            if (line.Length <= maxLen) return new List<string> { line };

            var parts = (int)Math.Ceiling(line.Length / (double)maxLen);
            var result = new List<string>();
            var rest = line;

            for (var remaining = parts; remaining > 1; remaining--)
            {
                var ideal = rest.Length / remaining;
                var minLeft = rest.Length - maxLen * (remaining - 1);   // the remainder must still fit
                var cut = FindBreak(rest, ideal, minLeft, maxLen);
                if (cut <= 0) break;                                    // one giant word, leave it

                result.Add(rest[..cut]);
                rest = rest[(cut + 1)..];
            }

            result.Add(rest);
            return result;
        }

        private static int FindBreak(string text, int ideal, int minLeft, int maxLeft)
        {
            int best = -1, bestCost = int.MaxValue;
            int nearest = -1, nearestDist = int.MaxValue;

            for (var s = 1; s < text.Length - 1; s++)
            {
                if (text[s] != ' ') continue;

                var dist = Math.Abs(s - ideal);
                if (dist < nearestDist) { nearest = s; nearestDist = dist; }
                if (s < minLeft || s > maxLeft) continue;

                var cost = dist;
                if (EndsWithPause(text, s)) cost -= 6;
                if (BreakBefore.Contains(WordAt(text, s + 1))) cost -= 4;
                if (NoBreakAfter.Contains(WordBefore(text, s))) cost += 8;
                if (s < 8 || text.Length - s - 1 < 8) cost += 10;       // avoid tiny orphan lines

                if (cost < bestCost) { best = s; bestCost = cost; }
            }

            return best >= 0 ? best : nearest;
        }

        private static bool EndsWithPause(string text, int space)
        {
            var i = space - 1;
            while (i > 0 && WrapChars.Contains(text[i])) i--;
            return PauseChars.Contains(text[i]);
        }

        private static string WordBefore(string text, int space)
        {
            var start = text.LastIndexOf(' ', space - 1) + 1;
            return text[start..space].Trim(TrimChars);
        }

        private static string WordAt(string text, int start)
        {
            var end = text.IndexOf(' ', start);
            if (end < 0) end = text.Length;
            return text[start..end].Trim(TrimChars);
        }
    }
}
