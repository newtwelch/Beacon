using System.Text.RegularExpressions;

namespace Beacon.Model.Songs
{
    public static class SongExtension
    {
        public static Song Normalize(this Song song) 
        {
            if (song is null)
                return new Song();

            var normalizedSong = song;

            normalizedSong.Title = song.Title.Replace("<span class=\"text-orange group-hover:text-accented-on-primary\">", "");
            normalizedSong.Title = song.Title.Replace("</span>", "");
            normalizedSong.Author = song.Author.Replace("<span class=\"text-orange group-hover:text-accented-on-primary\">", "");
            normalizedSong.Author = song.Author.Replace("</span>", "");
            normalizedSong.Tags = song.Tags.Replace("<span class=\"text-orange group-hover:text-accented-on-primary\">", "");
            normalizedSong.Tags = song.Tags.Replace("</span>", "");
            normalizedSong.LyricText = song.LyricText.Replace("<span class=\"text-orange group-hover:text-accented-on-primary\">", "");
            normalizedSong.LyricText = song.LyricText.Replace("</span>", "");


            return normalizedSong;
        }

        public static List<Lyric> Lyrics(this Song song)
        {
            if (String.IsNullOrWhiteSpace(song.LyricText))
                return new List<Lyric>(0);

            var lyrics = new List<Lyric>();

            var chorus = ConvertTextToLyrics(song.LyricText).Find(x => x.Type == LyricType.Chorus);
            var bridge = ConvertTextToLyrics(song.LyricText).Find(x => x.Type == LyricType.Bridge);
            var preChorus = ConvertTextToLyrics(song.LyricText).Find(x => x.Type == LyricType.PreChorus);

            var stanzaBeforeChorus = 0;
            var stanzaBeforeChorusCounter = 0;
            var lyricId = 1;

            if (String.IsNullOrWhiteSpace(song.Sequence) || song.Sequence.ToLower() == "o" || song.Sequence.ToLower() == "auto")
            {
                foreach (var lyric in ConvertTextToLyrics(song.LyricText))
                {
                    bool isStanza = lyric.Type.Equals(LyricType.Stanza);
                    bool isPrechorus = lyric.Type.Equals(LyricType.Bridge);
                    bool isChorus = lyric.Type.Equals(LyricType.Chorus);
                    bool isBridge = lyric.Type.Equals(LyricType.Bridge);

                    lyric.Id = lyricId++;
                    lyrics.Add(lyric);

                    if (isStanza && !lyrics.Any(l => l.Type.Equals(LyricType.Chorus)))
                    {
                        stanzaBeforeChorus++;
                        continue;
                    }

                    if (stanzaBeforeChorus == 0)
                        stanzaBeforeChorus = 1;

                    if (isStanza)
                        stanzaBeforeChorusCounter++;

                    if((stanzaBeforeChorus == stanzaBeforeChorusCounter && chorus is not null) || isBridge)
                    {
                        if(preChorus is not null)
                        {
                            preChorus.Id = lyricId++;
                            lyrics.Add(preChorus);
                        }

                        var chorusCopy = new Lyric(chorus);
                        chorusCopy.Id = lyricId++;
                        lyrics.Add(chorusCopy);

                        stanzaBeforeChorusCounter = 0;
                    }

                }
            }
            else
            {
                string[] sequence = song.Sequence.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in sequence)
                {
                    var lyric = ConvertTextToLyrics(song.LyricText).Find(x => x.Line.Replace("V", "").Replace("v", "").ToLower() == line.Replace("V", "").Replace("v", "").ToLower());
                    
                    if(lyric is not null)
                        lyric.Id = lyricId++;

                    if (lyric is not null)
                        lyrics.Add(lyric);
                }
            }

            return lyrics;
        }

        private static List<Lyric> ConvertTextToLyrics(string lyricText)
        {
            var lyrics = new List<Lyric>();
            
            int stanzaNumber = 1;

            string[] paragraphs = Array.FindAll(Regex.Split(lyricText, "(\r?\n){2,}", RegexOptions.Multiline), p => !string.IsNullOrWhiteSpace(p));
            
            foreach (string p in paragraphs)
            {
                var firstLine = new Regex("[^\r\n]*").Match(p).Value.ToLower();

                if ((firstLine.ToLower().Equals("pre-chorus") || firstLine.ToLower().Equals("prechorus") || firstLine.ToLower().Equals("pre chorus") ||
                    firstLine.ToLower().Equals("pre chor") || firstLine.ToLower().Equals("pre-chor")) &&
                    lyrics.Any(l => l.Type == LyricType.PreChorus)) continue;

                if ((firstLine.ToLower().Equals("chorus") || firstLine.ToLower().Equals("chorus 1") || firstLine.ToLower().Equals("chorus1") ||
                    firstLine.ToLower().Equals("chorus i") || firstLine.ToLower().Equals("chorusi")) && 
                    lyrics.Any(l => l.Type == LyricType.Chorus)) continue;

                if ((firstLine.ToLower().Equals("chorus 2") || firstLine.ToLower().Equals("chorus2") ||
                    firstLine.ToLower().Equals("chorus ii") || firstLine.ToLower().Equals("chorusii")) &&
                    lyrics.Any(l => l.Type == LyricType.ChorusII)) continue;

                if ((firstLine.ToLower().Equals("chorus 3") || firstLine.ToLower().Equals("chorus3") ||
                    firstLine.ToLower().Equals("chorus iii") || firstLine.ToLower().Equals("chorusiii")) &&
                    lyrics.Any(l => l.Type == LyricType.ChorusIII)) continue;

                if ((firstLine.ToLower().Equals("bridge") || firstLine.ToLower().Equals("bridge 1") || firstLine.ToLower().Equals("bridge1") ||
                    firstLine.ToLower().Equals("bridge i") || firstLine.ToLower().Equals("bridgei")) && 
                    lyrics.Any(l => l.Type == LyricType.Bridge)) continue;

                if ((firstLine.ToLower().Equals("bridge 2") || firstLine.ToLower().Equals("bridge2") ||
                    firstLine.ToLower().Equals("bridge ii") || firstLine.ToLower().Equals("bridgeii")) &&
                    lyrics.Any(l => l.Type == LyricType.BridgeII)) continue;

                if ((firstLine.ToLower().Equals("bridge 3") || firstLine.ToLower().Equals("bridge3") ||
                    firstLine.ToLower().Equals("bridge iii") || firstLine.ToLower().Equals("bridgeiii")) &&
                    lyrics.Any(l => l.Type == LyricType.BridgeIII)) continue;


                var lyric = new Lyric();
                var split = p.Split(Environment.NewLine.ToCharArray(), 2);
                if(split.Count() == 2)
                    lyric.Text = split.Skip(1).FirstOrDefault()!.Trim();

                switch (firstLine)
                {
                    case "prechorus": case "pre chorus":
                    case "pre-chorus": case "pc":
                    case "pre chor": case "pre-chor":
                        lyric.Line = "PRE";
                        lyric.Type = LyricType.PreChorus;
                        break;

                    case "chorus":
                    case "chorusi": case "chorus i":
                    case "chorus1": case "chorus 1":
                        lyric.Line = "C";
                        lyric.Type = LyricType.Chorus;
                        break;

                    case "chorusii": case "chorus ii":
                    case "chorus2": case "chorus 2":
                        lyric.Line = "C2";
                        lyric.Type = LyricType.ChorusII;
                        break;


                    case "chorusiii": case "chorus iii":
                    case "chorus3": case "chorus 3":
                        lyric.Line = "C3";
                        lyric.Type = LyricType.ChorusIII;
                        break;

                    case "bridge":
                    case "bridgei": case "bridge i":
                    case "bridge1": case "bridge 1":
                        lyric.Line = "B";
                        lyric.Type = LyricType.Bridge;
                        break;

                    case "bridgeii": case "bridge ii":
                    case "bridge2": case "bridge 2":
                        lyric.Line = "B2";
                        lyric.Type = LyricType.BridgeII;
                        break;

                    case "bridgeiii": case "bridge iii":
                    case "bridge3": case "bridge 3":
                        lyric.Line = "B3";
                        lyric.Type = LyricType.BridgeIII;
                        break;

                    default:
                        lyric.Text = p;
                        lyric.Line = $"V{stanzaNumber++}";
                        lyric.Type = LyricType.Stanza;
                        break;
                }
                lyrics.Add(lyric);
            }

            return lyrics;
        }
    }
}
