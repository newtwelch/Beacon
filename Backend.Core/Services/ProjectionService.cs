using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Services
{
    public class ProjectionService
    {
        private readonly ILogger<ProjectionService> logger;

        public ProjectionService(ILogger<ProjectionService> _logger) 
        {
            logger = _logger;
        }

        public event Action<string, string, string>? LyricChanged, VerseTextChanged;
        public event Action<int> VersePortionChanged;

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

    }
}
