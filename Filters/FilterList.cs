using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private static readonly IFilter[] DefaultFilters =
        {
            new DifficultyFilter(),
            new DurationFilter(),
            new NJSFilter(),
            new PPFilter(),
            new StarDifficultyFilter(),
            new PlayerStatsFilter(),
            new OtherFilter()
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
    }
}
