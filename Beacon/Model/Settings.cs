﻿using SQLite;

namespace Beacon.Model
{
    public class Settings
    {
        [PrimaryKey]
        public int Id { get; set; } = 0;

        //APPEARANCE
        public bool EnableDarkMode { get; set; }
        public string ProjectionMonitor { get; set; }

        //SONG SETTINGS
        public bool AlwaysConfirmSongDeletion { get; set; }
        public bool ShowLyricMargin { get; set; }
        public bool EnableSearchModeUI { get; set; }

        //BIBLE SETTINGS
        public bool AlwaysEnglishBookNames { get; set; }

        //PROJECTOR SETTINGS
        public int ProjectorWidth { get; set; } = 1280;
        public int ProjectorHeight { get; set; } = 720;
        public bool TrueBlackBackground { get; set; }

    }
}
