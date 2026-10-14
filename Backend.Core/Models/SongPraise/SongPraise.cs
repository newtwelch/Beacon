using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Core.Models.SongPraise
{
    public sealed class SongPraiseTitle
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public bool Deleted { get; set; }
        public int Views { get; set; }
        public int VerseCount { get; set; }
    }

    public sealed class SongPraiseSong
    {
        public string Uuid { get; set; } = "";
        public string Title { get; set; } = "";
        public bool Deleted { get; set; }
        public int Views { get; set; }
        public List<SongPraiseVerse>? SongVerseDTOS { get; set; }
        public SongPraiseLanguage? LanguageDTO { get; set; }
        public string? VersionGroup { get; set; }
        public string? BackUpSongId { get; set; }
        public bool IsBackUp { get; set; }
        public List<int>? VerseOrderList { get; set; }
        public string? VerseOrder { get; set; }
    }

    public sealed class SongPraiseVerse
    {
        public string Text { get; set; } = "";
        public bool Chorus { get; set; }
        public int Type { get; set; }
    }

    public sealed class SongPraiseLanguage
    {
        public string? EnglishName { get; set; }
        public string? NativeName { get; set; }
    }

    public static class SongPraiseCache
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static string Serialize(SongPraiseSong song) => JsonSerializer.Serialize(song, Json);

        public static IEnumerable<SongPraiseSong> ReadAll(string path)
        {
            if (!File.Exists(path)) yield break;

            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                SongPraiseSong? song;
                try { song = JsonSerializer.Deserialize<SongPraiseSong>(line, Json); }
                catch (JsonException) { continue; }   // half-written last line after a crash

                if (song is not null && !string.IsNullOrEmpty(song.Uuid))
                    yield return song;
            }
        }
    }
}
