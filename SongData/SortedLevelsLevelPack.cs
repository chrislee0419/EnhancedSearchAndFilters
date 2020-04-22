﻿using System.Collections.Generic;
using UnityEngine;
using BS_Utils.Utilities;
using UIUtilities = EnhancedSearchAndFilters.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.SongData
{
    internal class SortedLevelsLevelPack : IBeatmapLevelPack
    {
        public string packID { get; private set; }
        public string packName { get; private set; }
        public string shortPackName { get; private set; }
        public string collectionName => shortPackName;

        public Sprite coverImage { get; private set; }

        private BeatmapLevelCollection _beatmapLevelCollection = new BeatmapLevelCollection(new IPreviewBeatmapLevel[0]);
        public IBeatmapLevelCollection beatmapLevelCollection => _beatmapLevelCollection;

        public const string PackIDSuffix = "ESAFSorted";
        public const string PackName = "Sorted Songs";

        /// <summary>
        /// Applies and stores sorting of a provided <see cref="IBeatmapLevelPack"/>.
        /// </summary>
        /// <param name="levelPack">A level pack to sort.</param>
        /// <returns>Returns itself if not using the non-default sort mode, otherwise returns the provided level pack.</returns>
        private IBeatmapLevelPack SetupFromLevelPack(IBeatmapLevelPack levelPack)
        {
            if (SongSortModule.IsDefaultSort)
                return levelPack;

            packID = levelPack.packID + PackIDSuffix;
            packName = levelPack.packName;
            shortPackName = levelPack.shortPackName + PackIDSuffix;
            coverImage = levelPack.coverImage;

            _beatmapLevelCollection.SetPrivateField("_levels", SongSortModule.SortSongs(levelPack.beatmapLevelCollection.beatmapLevels), typeof(BeatmapLevelCollection));

            return this;
        }

        /// <summary>
        /// Applies and stores sorting of a provided <see cref="IAnnotatedBeatmapLevelCollection"/>.
        /// </summary>
        /// <param name="levelCollection">A level collection to sort.</param>
        /// <returns>Returns itself if not using the non-default sort mode, otherwise returns the provided level pack.</returns>
        public IBeatmapLevelPack SetupFromLevelCollection(IAnnotatedBeatmapLevelCollection levelCollection)
        {
            if (levelCollection is IBeatmapLevelPack levelPack)
                return SetupFromLevelPack(levelPack);

            packID = PackIDSuffix;
            packName = PackName;
            shortPackName = levelCollection.collectionName + PackIDSuffix;
            coverImage = levelCollection.coverImage;

            _beatmapLevelCollection.SetPrivateField("_levels", SongSortModule.SortSongs(levelCollection.beatmapLevelCollection.beatmapLevels), typeof(BeatmapLevelCollection));

            return this;
        }

        /// <summary>
        /// Applies and stores sorting of a provided list of levels.
        /// </summary>
        /// <param name="levels">An enumerable containing the levels to sort.</param>
        /// <returns>Returns this instance.</returns>
        public IBeatmapLevelPack SetupFromLevels(IEnumerable<IPreviewBeatmapLevel> levels)
        {
            packID = PackIDSuffix;
            packName = PackName;
            shortPackName = PackIDSuffix;
            coverImage = UIUtilities.DefaultCoverImage;

            _beatmapLevelCollection.SetPrivateField("_levels", SongSortModule.SortSongs(levels), typeof(BeatmapLevelCollection));

            return this;
        }
    }
}
