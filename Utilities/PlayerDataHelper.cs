﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Utilities
{
    internal class PlayerDataHelper : MonoBehaviour
    {
        private static PlayerDataHelper _instance;
        public static PlayerDataHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    var playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
                    _playerData = playerDataModel?.playerData;

                    if (_playerData != null)
                    {
                        _instance = new GameObject("EnhancedSearchAndFiltersPlayerDataHelper").AddComponent<PlayerDataHelper>();
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                if (_instance == null)
                    Logger.log.Warn("PlayerData object could not be found. The feature requesting the PlayerData object may display unexpected behaviour");

                // NOTE: could return null if playerData somehow is null
                return _instance;
            }
        }

        private static PlayerData _playerData;

        /// <summary>
        /// Check whether the player has successfully completed a level. 
        /// A level is determined to be completed if there is a valid score on any characteristic and difficulty. 
        /// Optionally, you can limit the search by characteristic and/or difficulties. 
        /// All duplicate custom beatmaps are treated as completed if any of the duplicates have been completed at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <param name="characteristicName">The serialized name of the characteristic to check for level completion (optional).</param>
        /// <param name="difficulties">A list of difficulties to check for level completion (optional).</param>
        /// <returns>True if the player has completed the beatmap at least once, otherwise false.</returns>
        public bool HasCompletedLevel(string levelID, string characteristicName = null, List<BeatmapDifficulty> difficulties = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties != null && difficulties.Count == 0)
                difficulties = null;

            if (!string.IsNullOrEmpty(characteristicName) && difficulties != null)
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    x.beatmapCharacteristic.serializedName == characteristicName &&
                    difficulties.Contains(x.difficulty) &&
                    x.validScore);
            }
            else if (!string.IsNullOrEmpty(characteristicName))
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    x.beatmapCharacteristic.serializedName == characteristicName &&
                    x.validScore);
            }
            else if (difficulties != null)
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    difficulties.Contains(x.difficulty) &&
                    x.validScore);
            }
            else
            {
                return _playerData.levelsStatsData.Any(x => x.levelID.StartsWith(levelID) && x.validScore);
            }
        }

        /// <summary>
        /// Check whether the player has achieved a full combo for a level on any characteristic and difficulty. 
        /// Optionally, you can limit the search for a full combo to a specific characteristic and/or difficulties. 
        /// All duplicate custom beatmaps are treated as full combo'd if any of the duplicates have been full combo'd at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <param name="characteristicName">The serialized name of the characteristic to check for a full combo (optional).</param>
        /// <param name="difficulties">A list of difficulties to check for a full combo (optional).</param>
        /// <returns>True if the player has achieved a full combo on the beatmap, otherwise false.</returns>
        public bool HasFullComboForLevel(string levelID, string characteristicName = null, List<BeatmapDifficulty> difficulties = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties != null && difficulties.Count == 0)
                difficulties = null;

            if (!string.IsNullOrEmpty(characteristicName) && difficulties != null)
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    x.beatmapCharacteristic.serializedName == characteristicName &&
                    difficulties.Contains(x.difficulty) &&
                    x.validScore && x.fullCombo && x.maxCombo != 0);
            }
            else if (!string.IsNullOrEmpty(characteristicName))
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    x.beatmapCharacteristic.serializedName == characteristicName &&
                    x.validScore && x.fullCombo && x.maxCombo != 0);
            }
            else if (difficulties != null)
            {
                return _playerData.levelsStatsData.Any(x =>
                    x.levelID.StartsWith(levelID) &&
                    difficulties.Contains(x.difficulty) &&
                    x.validScore && x.fullCombo && x.maxCombo != 0);
            }
            else
            {
                return _playerData.levelsStatsData.Any(x => x.levelID.StartsWith(levelID) && x.validScore && x.fullCombo && x.maxCombo != 0);
            }
        }

        /// <summary>
        /// Returns the number of times the player has played a beatmap.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap to check.</param>
        /// <returns>An integer representing the number of times the player has played the beatmap.</returns>
        public int GetPlayCountForLevel(string levelID)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);
            return _playerData.levelsStatsData.Where(x => x.levelID.StartsWith(levelID)).Sum(x => x.playCount);
        }

        public static readonly string[] AllCharacteristicStrings = new string[]
        {
            "Standard",
            "NoArrows",
            "OneSaber",
            "Lawless",
            "Lightshow",
            "90Degree",
            "360Degree"
        };

        /// <summary>
        /// Returns the highest rank achieved for a level and a given list of difficulties.
        /// </summary>
        /// <param name="levelID">The level ID of the level to search through.</param>
        /// <param name="difficulties">A list of BeatmapDifficulties to search through. Use null to search through all difficulties.</param>
        /// <param name="characteristics">A list of characteristics to search through. Each characteristic is represented by its serialized string. 
        /// Use null to search through all characteristics.</param>
        /// <returns>The highest RankModel.Rank enum found for the selected difficulties, or null if the level has not yet been completed.</returns>
        public RankModel.Rank? GetHighestRankForLevel(string levelID, IEnumerable<BeatmapDifficulty> difficulties = null, IEnumerable<string> characteristics = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties == null)
                difficulties = LocalLeaderboardDataHelper.AllDifficulties;
            if (characteristics == null)
                characteristics = AllCharacteristicStrings;

            var validEntries = _playerData.levelsStatsData.Where(x =>
                x.levelID.StartsWith(levelID) &&
                x.highScore != 0 &&
                difficulties.Contains(x.difficulty) &&
                characteristics.Contains(x.beatmapCharacteristic.serializedName));

            if (validEntries.Count() > 0)
                return validEntries.Max(x => x.maxRank);
            else
                return null;
        }
    }
}
