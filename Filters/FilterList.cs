using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    public static class FilterList
    {
        public static event Action FilterListChanged;

        private static ReadOnlyCollection<IFilter> _readOnlyFilterList;
        public static ReadOnlyCollection<IFilter> ActiveFilters
        {
            get
            {
                if (_readOnlyFilterList == null)
                    _readOnlyFilterList = new ReadOnlyCollection<IFilter>(CurrentFilterList);

                return _readOnlyFilterList;
            }
        }

        private static List<IFilter> _currentFilterList;
        private static List<IFilter> CurrentFilterList
        {
            get
            {
                if (_currentFilterList == null)
                    _currentFilterList = new List<IFilter>(DefaultFilters);

                return _currentFilterList;
            }
        }

        internal static SearchFilter SearchFilter => CurrentFilterList.FirstOrDefault(f => f is SearchFilter) as SearchFilter;

        public static bool AnyApplied => CurrentFilterList.Any(x => x.IsFilterApplied);
        public static bool AnyChanged => CurrentFilterList.Any(x => x.HasChanges);

        private static readonly IFilter[] DefaultFilters =
        {
            new SearchFilter(),
            new DifficultyFilter(),
            new DurationFilter(),
            new NoteDensityFilter(),
            new NJSFilter(),
            new PPFilter(),
            new StarDifficultyFilter(),
            new VotedFilter(),
            new PlayerStatsFilter(),
            new CharacteristicsFilter(),
            new ModRequirementsFilter()
        };

        /// <summary>
        /// Install a custom filter. This function should be (and is recommended to be) used when your plugin is enabled. 
        /// This function should not be used when the filters screen is being shown.
        /// </summary>
        /// <param name="customFilter">A filter to add to the list.</param>
        public static void AddFilterToList(IFilter customFilter)
        {
            if (customFilter == null || CurrentFilterList.Contains(customFilter))
                return;

            CurrentFilterList.Insert(0, customFilter);

            FilterListChanged?.Invoke();
        }

        /// <summary>
        /// Remove a custom filter. This function should be used when your plugin is in the process of being disabled. 
        /// This function should not be used when the filters screen is being shown.
        /// </summary>
        /// <param name="customFilter">A filter to remove from the list.</param>
        public static void RemoveFilterFromList(IFilter customFilter)
        {
            if (customFilter == null && !DefaultFilters.Contains(customFilter))
                return;

            CurrentFilterList.Remove(customFilter);
            customFilter.Cleanup();

            FilterListChanged?.Invoke();
        }

        /// <summary>
        /// Change the staging values in each filter according to the settings saved to the provided QuickFilter.
        /// </summary>
        /// <param name="quickFilter">A QuickFilter containing the saved filter settings.</param>
        internal static void ApplyQuickFilter(QuickFilter quickFilter)
        {
            foreach (var filter in CurrentFilterList)
            {
                var filterSettings = quickFilter.Filters.FirstOrDefault(x => x.Name == filter.Name);

                if (filterSettings != null)
                    filter.SetStagingValuesFromPairs(filterSettings.Settings);
                else
                    filter.SetDefaultValuesToStaging();
            }
        }

        /// <summary>
        /// Apply filters to a provided array of IPreviewBeatmapLevel objects.
        /// </summary>
        /// <param name="unfilteredLevels">An array of levels to filter.</param>
        /// <param name="filteredLevels">An enumerable of filtered levels.</param>
        /// <param name="applyStagedSettings">Apply any staged settings before filtering levels. By default, this is set to true.</param>
        /// <returns>True if there is at least one filter applied. Otherwise, false.</returns>
        internal static bool ApplyFilter(IPreviewBeatmapLevel[] unfilteredLevels, out IEnumerable<IPreviewBeatmapLevel> filteredLevels, bool applyStagedSettings = true)
        {
            Logger.log.Debug("Applying filters without a provided list of BeatmapDetails objects. Filtered levels may not be correct if some custom levels are uncached");

            BeatmapDetails[] detailsList = BeatmapDetailsLoader.instance.LoadBeatmapsInstant(unfilteredLevels);
            Dictionary<BeatmapDetails, IPreviewBeatmapLevel> pairs = new Dictionary<BeatmapDetails, IPreviewBeatmapLevel>(unfilteredLevels.Length);

            for (int i = 0; i < unfilteredLevels.Length; ++i)
            {
                if (detailsList[i] != null && !pairs.ContainsKey(detailsList[i]))
                    pairs.Add(detailsList[i], unfilteredLevels[i]);
            }

            List<BeatmapDetails> filteredBeatmapDetails = pairs.Keys.ToList();
            bool hasApplied = ApplyFilter(ref filteredBeatmapDetails, applyStagedSettings);

            Logger.log.Debug($"Filter applied, {filteredBeatmapDetails.Count} songs left");

            filteredLevels = pairs.Where(x => filteredBeatmapDetails.Contains(x.Key)).Select(x => x.Value);
            return hasApplied;
        }

        /// <summary>
        /// Apply filters to a provided list of BeatmapDetails objects.
        /// </summary>
        /// <param name="detailsList">A list of BeatmapDetails objects that will be filtered through.</param>
        /// <param name="applyStagedSettings">Apply any staged settings before filtering levels. By default, this is set to true.</param>
        /// <returns>True if there is at least one filter applied. Otherwise, false.</returns>
        internal static bool ApplyFilter(ref List<BeatmapDetails> detailsList, bool applyStagedSettings = true)
        {
            bool hasApplied = false;

            foreach (var filter in CurrentFilterList)
            {
                if (applyStagedSettings)
                    filter.ApplyStagingValues();

                if (filter.IsFilterApplied)
                {
                    filter.FilterSongList(ref detailsList);
                    hasApplied = true;
                }
            }

            return hasApplied;
        }
    }
}
