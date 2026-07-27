using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backend.Core.Models.Songs
{
    public static class SongExtension
    {
        // --- Public API ------------------------------------------------

        public static Song Normalize(this Song song)
        {
            if (song is null) return new Song();

            string StripHighlights(string s) =>
                string.IsNullOrEmpty(s) ? s :
                Regex.Replace(s, @"</?span[^>]*>", "", RegexOptions.IgnoreCase);

            var normalized = song;
            normalized.Title = StripHighlights(song.Title);
            normalized.Author = StripHighlights(song.Author);
            normalized.Tags = StripHighlights(song.Tags);
            normalized.LyricText = StripHighlights(song.LyricText);
            return normalized;
        }

        /// <summary>
        /// Entry: returns a list of Lyric objects sequenced according to song.Sequence ("auto" or custom).
        /// </summary>
        public static List<Lyric> Lyrics(this Song song)
        {
            if (song is null || string.IsNullOrWhiteSpace(song.LyricText))
                return new List<Lyric>(0);

            var parsed = ParseLyrics(song.LyricText);

            if (string.IsNullOrWhiteSpace(song.Sequence)
                || song.Sequence.Equals("o", StringComparison.OrdinalIgnoreCase)
                || song.Sequence.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return BuildAutoSequence(parsed);
            }
            else
            {
                return BuildManualSequence(parsed, song.Sequence);
            }
        }

        // --- Internal representations ----------------------------------

        // A parsed entry preserves original ordering; header definitions (chorus, prechorus, bridge)
        // are flagged as "IsHeader" so we can decide whether to output them where they were written.
        private sealed class PartEntry
        {
            public Lyric Lyric { get; set; } = new Lyric();
            public bool IsHeader { get; set; } // true for Chorus/Pre/Bridge definitions, false for stanza text
        }

        private sealed class ParsedLyrics
        {
            public List<PartEntry> AllPartsInOrder { get; } = new List<PartEntry>();
            // quick lookup by number
            public Dictionary<int, Lyric> Choruses { get; } = new Dictionary<int, Lyric>();
            public Dictionary<int, Lyric> PreChoruses { get; } = new Dictionary<int, Lyric>();
            public Dictionary<int, Lyric> Bridges { get; } = new Dictionary<int, Lyric>();
            public List<Lyric> Stanzas { get; } = new List<Lyric>();
        }

        // --- Parsing ---------------------------------------------------

        // Precompile regexes for performance
        private static readonly Regex ParagraphSplit = new Regex(@"(\r?\n){2,}", RegexOptions.Compiled);
        private static readonly Regex FirstLine = new Regex(@"^[^\r\n]*", RegexOptions.Compiled);
        private static readonly Regex HeaderRegex = new Regex(
            @"^\s*(?<type>pre[- ]?chorus|prechorus|pc|pre chor|chorus|chor|ch|chorusi|chorus\s*[ivx]+|bridge|br|bridge\s*[ivx]+|bridgei|stanza|verse|v)\b\.?\s*(?<num>[0-9]+|[ivx]+)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TokenRegex = new Regex(@"^(?<code>[a-zA-Z\-]+)(?<num>[0-9]+|[ivx]+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumberInLine = new Regex(@"\b(\d+)\b", RegexOptions.Compiled);

        private static ParsedLyrics ParseLyrics(string lyricText)
        {
            var parsed = new ParsedLyrics();
            if (string.IsNullOrWhiteSpace(lyricText)) return parsed;

            // paragraphs = blocks separated by 2+ newlines
            var paragraphs = ParagraphSplit.Split(lyricText)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            int stanzaCounter = 1;

            foreach (var p in paragraphs)
            {
                // get first line to test header
                var headerCandidate = FirstLine.Match(p).Value.Trim();
                var m = HeaderRegex.Match(headerCandidate);

                if (m.Success)
                {
                    var rawType = m.Groups["type"].Value.ToLowerInvariant();
                    var numGroup = m.Groups["num"].Success ? m.Groups["num"].Value : null;
                    int number = ParseNumberOrRomanOrDefault(numGroup, 1);

                    // text body is everything after first newline (if any); otherwise empty
                    var split = p.Split(new[] { '\r', '\n' }, 2);
                    var body = split.Length == 2 ? split[1].Trim() : string.Empty;

                    if (rawType.StartsWith("pre"))
                    {
                        var ly = new Lyric { Line = number == 1 ? "Pre-Chorus" : $"Pre-Chorus {number}", Text = body, Type = LyricType.PreChorus };
                        parsed.AllPartsInOrder.Add(new PartEntry { Lyric = ly, IsHeader = true });
                        if (!parsed.PreChoruses.ContainsKey(number)) parsed.PreChoruses[number] = ly;
                        continue;
                    }

                    if (rawType.StartsWith("chor") || rawType == "ch" || rawType == "chorusi")
                    {
                        int idx = number;
                        var ly = new Lyric { Line = idx == 1 ? "Chorus" : $"Chorus {idx}", Text = body, Type = idx == 1 ? LyricType.Chorus : (idx == 2 ? LyricType.ChorusII : LyricType.ChorusIII) };
                        parsed.AllPartsInOrder.Add(new PartEntry { Lyric = ly, IsHeader = true });
                        if (!parsed.Choruses.ContainsKey(idx)) parsed.Choruses[idx] = ly;
                        continue;
                    }

                    if (rawType.StartsWith("brid") || rawType == "br" || rawType.StartsWith("bridgei"))
                    {
                        int idx = number;
                        var ly = new Lyric { Line = idx == 1 ? "Bridge" : $"Bridge {idx}", Text = body, Type = idx == 1 ? LyricType.Bridge : (idx == 2 ? LyricType.BridgeII : LyricType.BridgeIII) };
                        parsed.AllPartsInOrder.Add(new PartEntry { Lyric = ly, IsHeader = true });
                        if (!parsed.Bridges.ContainsKey(idx)) parsed.Bridges[idx] = ly;
                        continue;
                    }

                    if (rawType.StartsWith("stanza") || rawType.StartsWith("verse") || rawType == "v")
                    {
                        var ly = new Lyric { Line = $"Stanza {stanzaCounter++}", Text = (p.Contains("\n") ? p.Trim() : p.Trim()), Type = LyricType.Stanza };
                        parsed.AllPartsInOrder.Add(new PartEntry { Lyric = ly, IsHeader = false });
                        parsed.Stanzas.Add(ly);
                        continue;
                    }

                    // fallback: treat as stanza if unknown
                }

                // default: stanza (paragraph without recognized header)
                var stanza = new Lyric { Line = $"Stanza {stanzaCounter++}", Text = p.Trim(), Type = LyricType.Stanza };
                parsed.AllPartsInOrder.Add(new PartEntry { Lyric = stanza, IsHeader = false });
                parsed.Stanzas.Add(stanza);
            }

            return parsed;
        }

        // --- Sequencing ------------------------------------------------

        /// <summary>
        /// Auto-sequencer:
        /// - iterates original parts
        /// - outputs stanzas immediately
        /// - when stanza threshold (stanzas-before-first-chorus) reached, inserts prechorus and chorus unless the next original part is already that chorus (avoids duplicates)
        /// - when encountering a bridge, inserts the latest seen chorus after the bridge unless the next original part is that chorus
        /// - when encountering an explicit chorus in the original text, outputs it and resets counters
        /// </summary>
        private static List<Lyric> BuildAutoSequence(ParsedLyrics parsed)
        {
            var outList = new List<Lyric>();
            if (parsed == null) return outList;

            int id = 1;

            // Determine stanzaBeforeChorus: count consecutive stanzas before first chorus in original scan
            int stanzaBeforeChorus = 0;
            foreach (var part in parsed.AllPartsInOrder)
            {
                if (part.Lyric.Type == LyricType.Stanza) stanzaBeforeChorus++;
                else if (IsChorusType(part.Lyric.Type) || IsPreChorusType(part.Lyric.Type)) break;
                // ignore pre/bridge in this calculation
            }
            if (stanzaBeforeChorus <= 0) stanzaBeforeChorus = 1;

            int stanzaCounterSinceInsert = 0;
            int lastSeenChorusNumber = parsed.Choruses.Any() ? parsed.Choruses.Keys.OrderBy(k => k).First() : 0;

            // iterate with index so we can peek next original part (to avoid duplicates)
            var parts = parsed.AllPartsInOrder;
            for (int i = 0; i < parts.Count; i++)
            {
                var entry = parts[i];
                var part = entry.Lyric;

                // If part is header (chorus/pre/bridge) the entry.IsHeader==true; for stanzas it's false
                // We will output stanzas immediately. For header parts, we will output them when they appear in original,
                // but we will not auto-insert duplicates later.

                if (part.Type == LyricType.Stanza)
                {
                    outList.Add(CloneWithNewId(part, id++));
                    stanzaCounterSinceInsert++;

                    // check insertion condition
                    if (parsed.Choruses.Any() && stanzaCounterSinceInsert >= stanzaBeforeChorus)
                    {
                        // choose chorus to insert: prefer lastSeenChorusNumber (if >0), otherwise Chorus 1
                        int chorusNumToInsert = lastSeenChorusNumber > 0 ? lastSeenChorusNumber : 1;

                        // Peek next original: if next original is the chorus we're about to insert, skip insertion.
                        bool nextIsSameChorus = false;
                        if (i + 1 < parts.Count)
                        {
                            var next = parts[i + 1].Lyric;
                            if (IsChorusType(next.Type) || IsPreChorusType(next.Type))
                            {
                                int nextNum = ExtractNumberFromLineOrDefault(next.Line, 1);
                                if (nextNum == chorusNumToInsert) nextIsSameChorus = true;
                            }
                        }

                        if (!nextIsSameChorus)
                        {
                            // insert prechorus for that chorus if exists
                            if (parsed.PreChoruses.TryGetValue(chorusNumToInsert, out var pre))
                            {
                                outList.Add(CloneWithNewId(pre, id++));
                            }

                            // insert the chorus
                            if (parsed.Choruses.TryGetValue(chorusNumToInsert, out var chorusToInsert))
                            {
                                outList.Add(CloneWithNewId(chorusToInsert, id++));
                            }
                        }

                        // reset counter after insertion (or skip)
                        stanzaCounterSinceInsert = 0;
                    }
                }
                else if (IsChorusType(part.Type))
                {
                    // explicit chorus in original text -> output it and update lastSeenChorusNumber and reset stanza counter
                    outList.Add(CloneWithNewId(part, id++));
                    lastSeenChorusNumber = ExtractNumberFromLineOrDefault(part.Line, 1);
                    stanzaCounterSinceInsert = 0;
                }
                else if (part.Type == LyricType.PreChorus)
                {
                    // explicit prechorus in text: output it (it's the author-provided instance)
                    outList.Add(CloneWithNewId(part, id++));
                    // do NOT change stanzaCounter -- prechorus is not counted as a stanza
                }
                else if (IsBridgeType(part.Type))
                {
                    // output the bridge
                    outList.Add(CloneWithNewId(part, id++));

                    // after a bridge, insert the latest seen chorus (if any), unless the next original part is that chorus
                    if (parsed.Choruses.Any() && lastSeenChorusNumber > 0)
                    {
                        bool nextIsSameChorus = false;
                        if (i + 1 < parts.Count)
                        {
                            var next = parts[i + 1].Lyric;
                            if (IsChorusType(next.Type))
                            {
                                int nextNum = ExtractNumberFromLineOrDefault(next.Line, 1);
                                if (nextNum == lastSeenChorusNumber) nextIsSameChorus = true;
                            }
                        }

                        if (!nextIsSameChorus)
                        {
                            // insert prechorus if exists
                            if (parsed.PreChoruses.TryGetValue(lastSeenChorusNumber, out var pre))
                            {
                                outList.Add(CloneWithNewId(pre, id++));
                            }

                            if (parsed.Choruses.TryGetValue(lastSeenChorusNumber, out var chorusToInsert))
                            {
                                outList.Add(CloneWithNewId(chorusToInsert, id++));
                            }

                            // reset stanza counter after insertion
                            stanzaCounterSinceInsert = 0;
                        }
                    }
                }
                else
                {
                    // unknown type - output safe fallback
                    outList.Add(CloneWithNewId(part, id++));
                }
            }

            return outList;
        }

        // Manual sequence builder
        private static List<Lyric> BuildManualSequence(ParsedLyrics parsed, string rawSequence)
        {
            var outList = new List<Lyric>();
            if (parsed == null) return outList;

            int id = 1;

            // tokens split by whitespace or comma
            var tokens = rawSequence.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in tokens)
            {
                var token = raw.Trim();
                if (string.IsNullOrEmpty(token)) continue;

                // numeric stanza reference?
                if (int.TryParse(token, out var stanzaNum))
                {
                    var stanza = parsed.Stanzas.ElementAtOrDefault(stanzaNum - 1);
                    if (stanza != null) outList.Add(CloneWithNewId(stanza, id++));
                    continue;
                }

                var m = TokenRegex.Match(token);
                if (!m.Success) continue;

                var code = m.Groups["code"].Value.ToLowerInvariant();
                int num = ParseNumberOrRomanOrDefault(m.Groups["num"].Value, 1);

                // chorus tokens
                if (code == "c" || code == "ch" || code == "chor" || code == "chorus")
                {
                    if (parsed.Choruses.TryGetValue(num, out var ch))
                        outList.Add(CloneWithNewId(ch, id++));
                    continue;
                }

                // prechorus tokens
                if (code == "pc" || code == "pre" || code == "prechor" || code == "prechorus" || code == "pre-chorus")
                {
                    if (parsed.PreChoruses.TryGetValue(num, out var pc))
                        outList.Add(CloneWithNewId(pc, id++));
                    continue;
                }

                // bridge tokens
                if (code == "b" || code == "br" || code == "bridge")
                {
                    if (parsed.Bridges.TryGetValue(num, out var br))
                        outList.Add(CloneWithNewId(br, id++));
                    continue;
                }

                // fallback: try to find by Line matching token
                var fallback = parsed.AllPartsInOrder.Select(pe => pe.Lyric)
                    .FirstOrDefault(l => !string.IsNullOrEmpty(l.Line) && l.Line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                if (fallback != null) outList.Add(CloneWithNewId(fallback, id++));
            }

            return outList;
        }

        // --- Helpers ---------------------------------------------------

        private static bool IsChorusType(LyricType t) =>
            t == LyricType.Chorus || t == LyricType.ChorusII || t == LyricType.ChorusIII;

        private static bool IsPreChorusType(LyricType t) =>
            t == LyricType.PreChorus;

        private static bool IsBridgeType(LyricType t) =>
            t == LyricType.Bridge || t == LyricType.BridgeII || t == LyricType.BridgeIII;

        private static Lyric CloneWithNewId(Lyric src, int id)
        {
            // shallow copy is fine - Lyric class has only the listed properties
            return new Lyric
            {
                Id = id,
                Line = src.Line,
                Text = src.Text,
                Type = src.Type
            };
        }

        private static int ExtractNumberFromLineOrDefault(string line, int def)
        {
            if (string.IsNullOrWhiteSpace(line)) return def;
            var m = NumberInLine.Match(line);
            if (m.Success && int.TryParse(m.Value, out var n)) return n;
            return def;
        }

        private static int ParseNumberOrRomanOrDefault(string token, int def)
        {
            if (string.IsNullOrWhiteSpace(token)) return def;
            var v = ParseNumberOrRoman(token);
            return v ?? def;
        }

        // parse "2" or roman "ii" => 2; returns null if invalid
        private static int? ParseNumberOrRoman(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim().ToLowerInvariant();
            if (int.TryParse(token, out var n)) return n;

            // simple roman -> integer parser for i, v, x combos
            int RomanVal(char c) => c switch { 'i' => 1, 'v' => 5, 'x' => 10, _ => 0 };
            int total = 0;
            int prev = 0;
            for (int i = token.Length - 1; i >= 0; i--)
            {
                var val = RomanVal(token[i]);
                if (val == 0) return null; // invalid roman
                if (val < prev) total -= val; else total += val;
                prev = val;
            }
            return total == 0 ? null : (int?)total;
        }
    }
}