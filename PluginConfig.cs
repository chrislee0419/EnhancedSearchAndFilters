using System;
using BS_Utils.Utilities;

namespace EnhancedSearchAndFilters
{
    internal static class PluginConfig
    {
        private static readonly Config config = new Config("EnhancedSearchAndFilters");
        private const string MainSection = "EnhancedSearchAndFilters";
        private const string QuickFiltersSection = "QuickFilters";
        private const string QuickFilterPrefix = "QuickFilter";

        /// <summary>
        /// The maximum number of songs that can be displayed as an intermediate search result.
        /// </summary>
        public static int MaxSearchResults
        {
            get
            {
                int value = config.GetInt(MainSection, "MaxSearchResults", MaxSearchResultsDefaultValue, true);
                return Math.Min(Math.Max(value, MaxSearchResultsMinValue), MaxSearchResultsUnlimitedValue);
            }
            set => config.SetInt(MainSection, "MaxSearchResults", value);
        }
        public const int MaxSearchResultsDefaultValue = 14;
        public const int MaxSearchResultsMaxValue = 28;
        public const int MaxSearchResultsMinValue = 7;
        public const int MaxSearchResultsUnlimitedValue = MaxSearchResultsMaxValue + MaxSearchResultsValueIncrement;
        public const int MaxSearchResultsValueIncrement = 7;

        /// <summary>
        /// Determine whether to strip symbols from song details when performing a search
        /// </summary>
        public static bool StripSymbols
        {
            get => config.GetBool(MainSection, "StripSymbolsFromSearch", StripSymbolsDefaultValue, true);
            set => config.SetBool(MainSection, "StripSymbolsFromSearch", value);
        }
        public const bool StripSymbolsDefaultValue = true;

        /// <summary>
        /// Determine whether to query each word independently (A word is any cluster of characters separated by a space)
        /// </summary>
        public static bool SplitQueryByWords
        {
            get => config.GetBool(MainSection, "SplitQueryByWords", SplitQueryByWordsDefaultValue, true);
            set => config.SetBool(MainSection, "SplitQueryByWords", value);
        }
        public const bool SplitQueryByWordsDefaultValue = true;

        /// <summary>
        /// Determine which parts of a beatmap's details to search
        /// </summary>
        public static SearchableSongFields SongFieldsToSearch
        {
            get
            {
                int value = config.GetInt(MainSection, "SongFieldsToSearch", (int)SongFieldsToSearchDefaultValue, true);
                return Enum.IsDefined(typeof(SearchableSongFields), value) ? (SearchableSongFields)value : SongFieldsToSearchDefaultValue;
            }
            set => config.SetInt(MainSection, "SongFieldsToSearch", (int)value);
        }
        public const SearchableSongFields SongFieldsToSearchDefaultValue = SearchableSongFields.All;

        /// <summary>
        /// Use the compact, single-screen keyboard in the search screen.
        /// </summary>
        public static bool CompactSearchMode
        {
            get => config.GetBool(MainSection, "CompactSearchMode", CompactSearchModeDefaultValue, true);
            set => config.SetBool(MainSection, "CompactSearchMode", value);
        }
        public const bool CompactSearchModeDefaultValue = false;

        /// <summary>
        /// The number of songs to search through in one frame. This setting is not exposed in the UI and can only be edited in the config.
        /// </summary>
        public static int MaxSongsToSearchInOneFrame
        {
            get => config.GetInt(MainSection, "MaxSongsToSearchInOneFrame", 100, true);
        }

        /// <summary>
        /// Show the explanatory loading screen on the filters page.
        /// </summary>
        public static bool ShowFirstTimeLoadingText
        {
            get => config.GetBool(MainSection, "ShowFirstTimeLoadingText", true, true);
            set => config.SetBool(MainSection, "ShowFirstTimeLoadingText", value);
        }

        /// <summary>
        /// Remove this mod's search feature. 
        /// Intended for users who want to use another mod's (only SongBrowser at the moment) search feature instead.
        /// </summary>
        public static bool DisableSearch
        {
            get => config.GetBool(MainSection, "DisableSearch", false, true);
            set => config.SetBool(MainSection, "DisableSearch", value);
        }

        /// <summary>
        /// Remove this mod's filter feature. Intended for those who don't use filters. 
        /// Removes access to details cache, which takes up a relatively large amount of memory.
        /// </summary>
        public static bool DisableFilters
        {
            get => config.GetBool(MainSection, "DisableFilter", false, true);
            set => config.SetBool(MainSection, "DisableFilter", value);
        }

        /// <summary>
        /// Scroll speed of the fast page up and down buttons. This setting is currently not exposed in the UI.
        /// </summary>
        public static float FastScrollSpeed
        {
            get => config.GetFloat(MainSection, "FastScrollSpeed", 5f, true);
            set => config.SetFloat(MainSection, "FastScrollSpeed", value);
        }

        /// <summary>
        /// The number of songs to load in one unit of work done by the beatmap details loader/cacher. 
        /// This setting is currently not exposed in the UI.
        /// </summary>
        public static int LoaderWorkChunkSize
        {
            get => config.GetInt(MainSection, "LoaderWorkChunkSize", LoaderWorkChunkSizeDefaultValue, true);
            set => config.SetInt(MainSection, "LoaderWorkChunkSize", value);
        }
        public const int LoaderWorkChunkSizeDefaultValue = 20;

        public static string GetQuickFilterData(int slot) => config.GetString(QuickFiltersSection, $"{QuickFilterPrefix}{slot}", "", true);

        public static void SetQuickFilterData(int slot, string serializedData) => config.SetString(QuickFiltersSection, $"{QuickFilterPrefix}{slot}", serializedData);
    }

    internal enum SearchableSongFields
    {
        All = 0,
        TitleAndAuthor = 1,
        TitleOnly = 2
    }
}
