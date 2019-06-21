using System;
using BS_Utils.Utilities;

namespace EnhancedSearchAndFilters
{
    internal static class PluginConfig
    {
        static private readonly Config config = new Config("EnhancedSearchAndFilters");

        /// <summary>
        /// The maximum number of songs that can be displayed as an intermediate search result.
        /// </summary>
        static public int MaxSearchResults
        {
            get
            {
                int value = config.GetInt("EnhancedSearchAndFilters", "MaxSearchResults", MaxSearchResultsDefaultValue, true);
                return Math.Min(Math.Max(value, MaxSearchResultsMinValue), MaxSearchResultsMaxValue);
            }
            set
            {
                config.SetInt("EnhancedSearchAndFilters", "MaxSearchResults", value);
            }
        }
        public const int MaxSearchResultsDefaultValue = 14;
        public const int MaxSearchResultsMaxValue = 28;
        public const int MaxSearchResultsMinValue = 7;

        /// <summary>
        /// Determine whether to strip symbols from song details when performing a search
        /// </summary>
        static public bool StripSymbols
        {
            get
            {
                return config.GetBool("EnhancedSearchAndFilters", "StripSymbolsFromSearch", StripSymbolsDefaultValue, true);
            }
            set
            {
                config.SetBool("EnhancedSearchAndFilters", "StripSymbolsFromSearch", value);
            }
        }
        public const bool StripSymbolsDefaultValue = false;

        /// <summary>
        /// Determine whether to query each word independently (A word is any cluster of characters separated by a space)
        /// </summary>
        static public bool SplitQueryByWords
        {
            get
            {
                return config.GetBool("EnhancedSearchAndFilters", "SplitQueryByWords", SplitQueryByWordsDefaultValue, true);
            }
            set
            {
                config.SetBool("EnhancedSearchAndFilters", "SplitQueryByWords", value);
            }
        }
        public const bool SplitQueryByWordsDefaultValue = true;

        /// <summary>
        /// Determine which parts of a beatmap's details to search
        /// </summary>
        static public SearchableSongFields SongFieldsToSearch
        {
            get
            {
                int value = config.GetInt("EnhancedSearchAndFilters", "SongFieldsToSearch", (int)SongFieldsToSearchDefaultValue, true);
                return Enum.IsDefined(typeof(SearchableSongFields), value) ? (SearchableSongFields)value : SongFieldsToSearchDefaultValue;
            }
            set
            {
                config.SetInt("EnhancedSearchAndFilters", "SongFieldsToSearch", (int)value);
            }
        }
        public const SearchableSongFields SongFieldsToSearchDefaultValue = SearchableSongFields.All;

        /// <summary>
        /// The number of songs to search through in one frame. This setting is not exposed in the UI and can only be edited in the config.
        /// </summary>
        static public int MaxSongsToSearchInOneFrame
        {
            get
            {
                return config.GetInt("EnhancedSearchAndFilters", "MaxSongsToSearchInOneFrame", 100, true);
            }
        }

        static public bool ShowFirstTimeLoadingText
        {
            get
            {
                return config.GetBool("EnhancedSearchAndFilters", "ShowFirstTimeLoadingText", true, true);
            }
            set
            {
                config.SetBool("EnhancedSearchAndFilters", "ShowFirstTimeLoadingText", value);
            }
        }
    }

    internal enum SearchableSongFields
    {
        All = 0,
        TitleAndAuthor = 1,
        TitleOnly = 2
    }
}
