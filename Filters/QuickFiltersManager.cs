using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        private const char EscapeCharacter = '~';
        private const char BackslashReplacementCharacter = 's';
        private const char SeparatorReplacementCharacter = 'b';

        private const char FilterListStartCharacter = ';';
        private const char FilterListSeparatorCharacter = '|';

        public const int MaxNameLength = 20;

        public QuickFilter()
        {
            Filters = new List<FilterSettings>();
        }

        public QuickFilter(string name, IEnumerable<IFilter> filters)
        {
            Name = name;

            Filters = new List<FilterSettings>();

            foreach (var filter in filters)
                Filters.Add(new FilterSettings(filter));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            StringBuilder escapedName = new StringBuilder(Name)
                .Replace(EscapeCharacter.ToString(), $"{EscapeCharacter}{EscapeCharacter}")
                .Replace("\\", $"{EscapeCharacter}{BackslashReplacementCharacter}")
                .Replace(FilterListSeparatorCharacter.ToString(), $"{EscapeCharacter}{SeparatorReplacementCharacter}")
                .Replace(FilterListStartCharacter.ToString(), $"{EscapeCharacter}{FilterListStartCharacter}");

            builder.Append(escapedName);
            builder.Append(FilterListStartCharacter);

            foreach (var filter in Filters)
            {
                StringBuilder filterString = new StringBuilder(filter.ToString())
                    .Replace(EscapeCharacter.ToString(), $"{EscapeCharacter}{EscapeCharacter}")
                    .Replace(FilterListSeparatorCharacter.ToString(), $"{EscapeCharacter}{SeparatorReplacementCharacter}");

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

            StringBuilder name = new StringBuilder(quickFilterString.Substring(0, startCharPos));

            for (int i = 0; i < name.Length; ++i)
            {
                if (name[i] == EscapeCharacter && (i + 1) < name.Length)
                {
                    char c = name[i + 1];

                    switch (c)
                    {
                        case EscapeCharacter:
                            name.Replace($"{EscapeCharacter}{EscapeCharacter}", EscapeCharacter.ToString(), i, 1);
                            break;
                        case BackslashReplacementCharacter:
                            name.Replace($"{EscapeCharacter}{BackslashReplacementCharacter}", "\\", i, 1);
                            break;
                        case SeparatorReplacementCharacter:
                            name.Replace($"{EscapeCharacter}{SeparatorReplacementCharacter}", FilterListSeparatorCharacter.ToString(), i, 1);
                            break;
                        case FilterListStartCharacter:
                            name.Replace($"{EscapeCharacter}{FilterListStartCharacter}", FilterListStartCharacter.ToString(), i, 1);
                            break;
                        default:
                            name.Remove(i, 1);
                            --i;
                            break;
                    }
                }
            }

            quickFilter.Name = name.ToString();

            string[] filterSettings = quickFilterString.Substring(startCharPos + 1).Split(FilterListSeparatorCharacter);
            foreach (var filter in filterSettings)
            {
                StringBuilder filterString = new StringBuilder(filter);

                for (int i = 0; i < filterString.Length; ++i)
                {
                    if (filterString[i] == EscapeCharacter && (i + 1) < filterString.Length)
                    {
                        char c = filterString[i + 1];

                        switch (c)
                        {
                            case EscapeCharacter:
                                filterString.Replace($"{EscapeCharacter}{EscapeCharacter}", EscapeCharacter.ToString(), i, 1);
                                break;
                            case SeparatorReplacementCharacter:
                                filterString.Replace($"{EscapeCharacter}{SeparatorReplacementCharacter}", FilterListSeparatorCharacter.ToString(), i, 1);
                                break;
                            default:
                                filterString.Remove(i, 1);
                                --i;
                                break;
                        }
                    }
                }

                var fs = FilterSettings.FromString(filterString.ToString());

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

        private const char EscapeCharacter = '?';
        private const char NewLineReplacementCharacter = 'n';
        private const char BackslashReplacementCharacter = 's';

        private const char SettingsListStartCharacter = '{';
        private const char SettingsListEndCharacter = '}';
        private const char SettingsListSeparatorCharacter = ',';

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

            StringBuilder escapedName = new StringBuilder(Name)
                .Replace(EscapeCharacter.ToString(), $"{EscapeCharacter}{EscapeCharacter}")
                .Replace("\n", $"{EscapeCharacter}{NewLineReplacementCharacter}")
                .Replace("\\", $"{EscapeCharacter}{BackslashReplacementCharacter}")
                .Replace(SettingsListStartCharacter.ToString(), $"{EscapeCharacter}{SettingsListStartCharacter}");

            builder.Append(escapedName);
            builder.Append(SettingsListStartCharacter);

            foreach (var pair in Settings)
            {
                builder.Append(pair.ToString());
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

            StringBuilder name = new StringBuilder(settingsString.Substring(0, startCharPos));

            for (int i = 0; i < name.Length; ++i)
            {
                if (name[i] == EscapeCharacter && (i + 1) < name.Length)
                {
                    char c = name[i + 1];

                    switch (c)
                    {
                        case EscapeCharacter:
                            name.Replace($"{EscapeCharacter}{EscapeCharacter}", EscapeCharacter.ToString(), i, 1);
                            break;
                        case NewLineReplacementCharacter:
                            name.Replace($"{EscapeCharacter}{NewLineReplacementCharacter}", "\n", i, 1);
                            break;
                        case BackslashReplacementCharacter:
                            name.Replace($"{EscapeCharacter}{BackslashReplacementCharacter}", "\\", i, 1);
                            break;
                        case SettingsListStartCharacter:
                            name.Replace($"{EscapeCharacter}{SettingsListStartCharacter}", SettingsListStartCharacter.ToString(), i, 1);
                            break;
                        default:
                            name.Remove(i, 1);
                            --i;
                            break;
                    }
                }
            }

            FilterSettings filterSettings = new FilterSettings();
            filterSettings.Name = name.ToString();

            // parse filter settings
            int endCharPos = settingsString.IndexOf(SettingsListEndCharacter, startCharPos);
            if (endCharPos < 0)
                return null;

            string[] settings = settingsString.Substring(startCharPos + 1, endCharPos - startCharPos - 1).Split(SettingsListSeparatorCharacter);
            foreach (var setting in settings)
            {
                var kv = FilterSettingsKeyValuePair.FromString(setting);
                if (kv != null)
                    filterSettings.Settings.Add(kv);
            }

            return filterSettings;
        }
    }
}
