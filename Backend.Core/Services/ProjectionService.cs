using Microsoft.Extensions.Logging;
using Backend.Core.Models.Beacon;

namespace Backend.Core.Services
{
    public class ProjectionService
    {
        private readonly ILogger<ProjectionService> logger;

        public ProjectionService(ILogger<ProjectionService> _logger)
        {
            logger = _logger;
        }

        private List<Screen> _monitors = new List<Screen>();

        public event Action<string, string, string, int, string?, int, int>? LyricChanged;
        public event Action<string, string, string, int, int>? VerseTextChanged;
        public event Action<int> VersePortionChanged;
        public event Action<Screen> MonitorChanged;

        public void ChangeLyric(string songTitle,string lyricLine,string lyricText,int stanzaCount,string? key,int lyricId,int totalLyrics)
        {
            LyricChanged?.Invoke(songTitle,lyricLine,lyricText,stanzaCount, key, lyricId, totalLyrics
            );
        }
        public void ChangeVerseText(string verseReference, string translation, string text, int verse, int totalVerses)
        {
            VerseTextChanged?.Invoke(verseReference, translation, text, verse, totalVerses);
        }

        public void ChangeTranslation(string translation)
        {
            VerseTextChanged?.Invoke("", translation, "", 0, 0);
        }

        public void ChangeVersePortion(int id)
        {
            VersePortionChanged?.Invoke(id);
        }

        public void ChangeMonitor(Screen monitor)
        {
            MonitorChanged?.Invoke(monitor);
        }

        public void AddMonitor(Screen monitor)
        {
            _monitors.Add(monitor);
        }

        public List<Screen> GetMonitors()
        {
            return _monitors;
        }
    }
}
