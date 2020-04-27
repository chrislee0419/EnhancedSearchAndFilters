using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal static class QuickFiltersManager
    {
        private static List<QuickFilter> _quickFiltersList;
        private static List<QuickFilter> InternalQuickFiltersList
        {
            get
            {
                if (_quickFiltersList == null)
                {
                    _quickFiltersList = new List<QuickFilter>(NumberOfSlots);
                    for (int i = 1; i <= NumberOfSlots; ++i)
                    {
                        var quickFilter = QuickFilter.FromString(PluginConfig.GetQuickFilterData(i));

                        if (quickFilter == null)
                            break;

                        _quickFiltersList.Add(quickFilter);
                    }
                }

                return _quickFiltersList;
            }
        }

        private static IReadOnlyList<QuickFilter> _readonlyQuickFiltersList;
        public static IReadOnlyList<QuickFilter> QuickFiltersList
        {
            get
            {
                if (_readonlyQuickFiltersList == null)
                    _readonlyQuickFiltersList = InternalQuickFiltersList.AsReadOnly();

                return _readonlyQuickFiltersList;
            }
        }

        public static int Count => QuickFiltersList.Count;
        public static bool HasSlotsAvailable => Count < NumberOfSlots;

        public const int NumberOfSlots = 10;

        public static bool SaveCurrentSettingsToQuickFilter(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.log.Warn("Unable to save quick filter with blank name");
                return false;
            }
            else if (name.Length > QuickFilter.MaxNameLength)
            {
                Logger.log.Warn($"Unable to save quick filter with a name over {QuickFilter.MaxNameLength} characters");
                return false;
            }
            else if (InternalQuickFiltersList.Count >= NumberOfSlots)
            {
                Logger.log.Warn($"Unable to save more than {NumberOfSlots} quick filters");
                return false;
            }

            var newQuickFilter = new QuickFilter(name, FilterList.ActiveFilters);

            InternalQuickFiltersList.Add(newQuickFilter);
            PluginConfig.SetQuickFilterData(InternalQuickFiltersList.Count, newQuickFilter.ToString());

            return true;
        }

        public static void DeleteQuickFilter(int slot)
        {
            if (slot < 0 || slot >= InternalQuickFiltersList.Count)
                return;

            InternalQuickFiltersList.RemoveAt(slot - 1);
            SaveAllQuickFilters();
        }

        public static void DeleteQuickFilter(string name)
        {
            var quickFilter = InternalQuickFiltersList.FirstOrDefault(x => x.Name == name);
            if (quickFilter == null)
                return;

            InternalQuickFiltersList.Remove(quickFilter);
            SaveAllQuickFilters();
        }

        public static void DeleteQuickFilter(QuickFilter quickFilter)
        {
            if (InternalQuickFiltersList.Remove(quickFilter))
                SaveAllQuickFilters();
        }

        private static void SaveAllQuickFilters()
        {
            for (int i = 1; i <= NumberOfSlots; ++i)
            {
                if (i <= InternalQuickFiltersList.Count)
                    PluginConfig.SetQuickFilterData(i, InternalQuickFiltersList[i - 1].ToString());
                else
                    PluginConfig.SetQuickFilterData(i, "");
            }
        }

        /// <summary>
        /// Gets the index of the given quick filter in the global list.
        /// </summary>
        /// <param name="quickFilter">A QuickFilter instance.</param>
        /// <returns>The index of the quick filter if found. Otherwise, -1.</returns>
        public static int IndexOf(QuickFilter quickFilter)
        {
            if (quickFilter == null || InternalQuickFiltersList.Count == 0)
                return -1;
            else
                return InternalQuickFiltersList.IndexOf(quickFilter);
        }
    }

    internal class QuickFilter
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value", "A quick filter cannot have a 'null' name.");
                else if (value.Length > MaxNameLength)
                    throw new ArgumentException($"A quick filter cannot have a name longer than '{MaxNameLength}'.", "value");
                _name = value;
            }
        }
        public List<FilterSettings> Filters { get; set; }

        private const char FilterListStartCharacter = ';';
        private const char FilterListSeparatorCharacter = '|';

        private const char EscapeCharacter = '~';
        private readonly static Dictionary<char, char> EscapeMapping = new Dictionary<char, char>()
        {
            { '\\', 's' },
            { FilterListStartCharacter, 'a' },
            { FilterListSeparatorCharacter, 'b' }
        };

        // NOTE: there is a text-based reference to this constant in the FilterTutorialView.bsml
        // if i ever change this again, make sure to update that reference too
        public const int MaxNameLength = 30;

        public QuickFilter()
        {
            Filters = new List<FilterSettings>();
        }

        public QuickFilter(string name, IEnumerable<IFilter> filters)
        {
            Name = name;

            Filters = new List<FilterSettings>();

            foreach (var filter in filters)
            {
                if (filter.IsFilterApplied)
                    Filters.Add(new FilterSettings(filter));
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            StringBuilder escapedName = new StringBuilder(Name).EscapeString(EscapeCharacter, EscapeMapping);

            builder.Append(escapedName);
            builder.Append(FilterListStartCharacter);

            foreach (var filter in Filters)
            {
                StringBuilder filterString = new StringBuilder(filter.ToString()).EscapeString(EscapeCharacter, EscapeMapping);

                builder.Append(filterString);
                builder.Append(FilterListSeparatorCharacter);
            }

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public static QuickFilter FromString(string quickFilterString)
        {
            if (string.IsNullOrEmpty(quickFilterString))
                return null;

            QuickFilter quickFilter = new QuickFilter();

            int startCharPos = quickFilterString.IndexOf(FilterListStartCharacter);
            if (startCharPos < 1)
            {
                Logger.log.Warn("Unable to convert deserialize string to QuickFilter object.");
                return null;
            }

            try
            {
                while (quickFilterString[startCharPos - 1] == EscapeCharacter)
                    startCharPos = quickFilterString.IndexOf(FilterListStartCharacter, startCharPos + 1);
            }
            catch (ArgumentOutOfRangeException)
            {
                Logger.log.Warn("Unable to convert deserialize string to QuickFilter object.");
                return null;
            }

            StringBuilder name = new StringBuilder(quickFilterString.Substring(0, startCharPos)).UnescapeString(EscapeCharacter, EscapeMapping);

            quickFilter.Name = name.ToString();

            string[] filterSettings = quickFilterString.Substring(startCharPos + 1).Split(FilterListSeparatorCharacter);
            foreach (var filter in filterSettings)
            {
                var fs = FilterSettings.FromString(new StringBuilder(filter).UnescapeString(EscapeCharacter, EscapeMapping).ToString());
                if (fs != null)
                    quickFilter.Filters.Add(fs);
            }

            return quickFilter;
        }
    }

    internal class FilterSettings
    {
        public string Name { get; set; }
        public List<FilterSettingsKeyValuePair> Settings { get; set; }

        private const char SettingsListStartCharacter = '{';
        private const char SettingsListEndCharacter = '}';
        private const char SettingsListSeparatorCharacter = ',';

        private const char EscapeCharacter = '?';
        private readonly static Dictionary<char, char> EscapeMapping = new Dictionary<char, char>
        {
            { '\n', 'n' },
            { '\\', 's' },
            { SettingsListStartCharacter, 'a' },
            { SettingsListEndCharacter, 'z' },
            { SettingsListSeparatorCharacter, 'c' },
        };

        public FilterSettings()
        {
            Settings = new List<FilterSettingsKeyValuePair>();
        }

        public FilterSettings(IFilter filter)
        {
            Name = filter.Name;
            Settings = filter.GetAppliedValuesAsPairs();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            StringBuilder escapedName = new StringBuilder(Name).EscapeString(EscapeCharacter, EscapeMapping);

            builder.Append(escapedName);
            builder.Append(SettingsListStartCharacter);

            foreach (var pair in Settings)
            {
                builder.Append(new StringBuilder(pair.ToString()).EscapeString(EscapeCharacter, EscapeMapping));
                builder.Append(SettingsListSeparatorCharacter);
            }

            builder.Remove(builder.Length - 1, 1);
            builder.Append(SettingsListEndCharacter);

            return builder.ToString();
        }

        public static FilterSettings FromString(string settingsString)
        {
            if (string.IsNullOrEmpty(settingsString))
                return null;

            // parse filter name
            int startCharPos = settingsString.IndexOf(SettingsListStartCharacter);
            if (startCharPos < 1)
            {
                Logger.log.Warn("Unable to deserialize string to FilterSettings object.");
                return null;
            }

            try
            {
                while (settingsString[startCharPos - 1] == EscapeCharacter)
                    startCharPos = settingsString.IndexOf(SettingsListStartCharacter, startCharPos + 1);
            }
            catch (ArgumentOutOfRangeException)
            {
                Logger.log.Warn("Unable to deserialize string to FilterSettings object.");
                return null;
            }

            StringBuilder name = new StringBuilder(settingsString.Substring(0, startCharPos)).UnescapeString(EscapeCharacter, EscapeMapping);

            FilterSettings filterSettings = new FilterSettings();
            filterSettings.Name = name.ToString();

            // parse filter settings
            int endCharPos = settingsString.IndexOf(SettingsListEndCharacter, startCharPos);
            if (endCharPos < 0)
                return null;

            string[] settings = settingsString.Substring(startCharPos + 1, endCharPos - startCharPos - 1).Split(SettingsListSeparatorCharacter);
            foreach (var setting in settings)
            {
                var kv = FilterSettingsKeyValuePair.FromString(new StringBuilder(setting).UnescapeString(EscapeCharacter, EscapeMapping).ToString());
                if (kv != null)
                    filterSettings.Settings.Add(kv);
            }

            return filterSettings;
        }
    }
}
