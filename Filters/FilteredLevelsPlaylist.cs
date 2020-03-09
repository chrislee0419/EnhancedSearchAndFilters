using UnityEngine;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class FilteredLevelsPlaylist : IPlaylist
    {
        public string collectionName => "Filtered Songs";

        public Sprite coverImage { get; } = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        private BeatmapLevelCollection _beatmapLevelCollection = new BeatmapLevelCollection(new IPreviewBeatmapLevel[0]);
        public IBeatmapLevelCollection beatmapLevelCollection => _beatmapLevelCollection;

        /// <summary>
        /// Applies filter settings and then filters, sorts, and stores the resulting levels from the provided level collection.
        /// </summary>
        /// <param name="levels">Levels to filter and sort.</param>
        /// <param name="applyStagedSettings">Apply the staged filter settings before using the filter.</param>
        /// <returns>True if at least one filter has been applied, otherwise false.</returns>
        public bool SetupFromUnfilteredLevels(IPreviewBeatmapLevel[] levels, bool applyStagedSettings = true)
        {
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
            if (sortSongs)
                filteredLevels = SongSortModule.SortSongs(filteredLevels);
            _beatmapLevelCollection.SetPrivateField("_levels", filteredLevels, typeof(BeatmapLevelCollection));
        }
    }
}
