using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongBrowserTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static bool Initialized { get; private set; } = false;
    }
}
