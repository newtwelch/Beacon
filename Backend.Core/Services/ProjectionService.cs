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

        public event Action<string, string, string>? LyricChanged, VerseTextChanged;
        public event Action<int> VersePortionChanged;
        public event Action<Screen> MonitorChanged;

        public void ChangeLyric(string songTitle, string lyricLine, string lyricText)
        {
            LyricChanged?.Invoke(songTitle, lyricLine, lyricText);
        }

        public void ChangeVerseText(string verseReference, string translation, string text)
        {
            VerseTextChanged?.Invoke(verseReference, translation, text);
        }

        public void ChangeTranslation(string translation)
        {
            VerseTextChanged?.Invoke("", translation, "");
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
