using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beacon.Model
{
    public static class Settings
    {
        public static bool EnableDarkMode { get; set; }
        public static int ProjectorWidth { get; set; } = 1280;
        public static int ProjectorHeight { get; set; } = 720;
    }
}
