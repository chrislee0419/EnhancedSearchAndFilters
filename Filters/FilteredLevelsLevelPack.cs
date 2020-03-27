using UnityEngine;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.SongData;
using UIUtilities = EnhancedSearchAndFilters.UI.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class FilteredLevelsLevelPack : IBeatmapLevelPack
    {
        public string packID => PackID;
        public string packName => PackName;
        public string shortPackName => CollectionName;
        public string collectionName => CollectionName;

        public Sprite coverImage { get; private set; }

        private BeatmapLevelCollection _beatmapLevelCollection = new BeatmapLevelCollection(new IPreviewBeatmapLevel[0]);
        public IBeatmapLevelCollection beatmapLevelCollection => _beatmapLevelCollection;

        public const string PackID = "EnhancedSearchAndFiltersCustomLevelPack";
        public const string PackName = "Filtered Songs";
        public const string CollectionName = "ESAFFiltered";

        /// <summary>
        /// Applies filter settings and then filters, sorts, and stores the resulting levels from the provided level collection.
        /// </summary>
        /// <param name="levels">Levels to filter and sort.</param>
        /// <param name="applyStagedSettings">Apply the staged filter settings before using the filter.</param>
        /// <returns>True if at least one filter has been applied, otherwise false.</returns>
        public bool SetupFromUnfilteredLevels(IPreviewBeatmapLevel[] levels, Sprite coverImage = null, bool applyStagedSettings = true)
        {
            this.coverImage = coverImage != null ? coverImage : UIUtilities.DefaultCoverImage;

            if (FilterList.ApplyFilter(levels, out var filteredLevels, applyStagedSettings))
            {
                IPreviewBeatmapLevel[] filteredAndSortedLevels = SongSortModule.SortSongs(filteredLevels);
                _beatmapLevelCollection.SetPrivateField("_levels", filteredAndSortedLevels, typeof(BeatmapLevelCollection));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sort and store a list of already filtered songs. Sorting is optional.
        /// </summary>
        /// <param name="filteredLevels">Levels that have already been filtered</param>
        /// <param name="sortSongs">Sort the songs before storing.</param>
        public void SetupFromPrefilteredLevels(IPreviewBeatmapLevel[] filteredLevels, bool sortSongs = true)
        {
            if (coverImage == null)
                coverImage = UIUtilities.DefaultCoverImage;

            if (sortSongs)
                filteredLevels = SongSortModule.SortSongs(filteredLevels);

            _beatmapLevelCollection.SetPrivateField("_levels", filteredLevels, typeof(BeatmapLevelCollection));
        }
    }
}
