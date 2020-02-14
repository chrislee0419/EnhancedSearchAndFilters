using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SongCore;

namespace EnhancedSearchAndFilters.SongData
{
    internal class LocalLeaderboardDataHelper : MonoBehaviour
    {
        private static LocalLeaderboardDataHelper _instance;
        public static LocalLeaderboardDataHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _localLeaderboardsModel = Resources.FindObjectsOfTypeAll<LocalLeaderboardsModel>().FirstOrDefault();

                    if (_localLeaderboardsModel != null)
                    {
                        _instance = new GameObject("EnhancedSearchAndFiltersLocalLeaderboardsDataHelper").AddComponent<LocalLeaderboardDataHelper>();
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                if (_instance == null)
                    Logger.log.Warn("LocalLeaderboardsModel object count not be found. The feature requesting the LocalLeaderboardsModel object may display unexpected behaviour");

                // NOTE: as with the PlayerStatsHelper, the instance could be null if the LocalLeaderboardsModel is null
                return _instance;
            }
        }

        private static LocalLeaderboardsModel _localLeaderboardsModel;

        private static readonly string[] CharacteristicStrings = new string[]
        {
            "",             // 'Standard' characteristic maps to empty string
            "NoArrows",
            "OneSaber",
            "Lawless",
            "Lightshow",
            "90Degree",
            "360Degree"
        };
        private static readonly List<BeatmapDifficulty> AllDifficulties = new List<BeatmapDifficulty>(Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>());

        /// <summary>
        /// Check whether the level has been successfully completed in party mode. 
        /// All duplicate custom beatmaps are treated as completed if any of the duplicates have been completed at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <param name="difficulties">A list of difficulties to check for level completion (optional).</param>
        /// <param name="playerName">The name of the player on the local leaderboards (optional).</param>
        /// <returns>True if the player(s) has/have completed the beatmap at least once, otherwise false.</returns>
        public bool HasCompletedLevel(string levelID, List<BeatmapDifficulty> difficulties = null, string playerName = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties == null || difficulties.Count == 0)
                difficulties = AllDifficulties;

            // get any level duplicates
            List<string> duplicateLevelIDs = new List<string>();
            if (levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
            {
                foreach (var duplicateLevel in Loader.CustomLevelsCollection.beatmapLevels.Where(x => x.levelID.StartsWith(levelID)))
                    duplicateLevelIDs.Add(duplicateLevel.levelID);

                if (!duplicateLevelIDs.Contains(levelID))
                    duplicateLevelIDs.Add(levelID);
            }
            else
            {
                duplicateLevelIDs.Add(levelID);
            }

            foreach (var levID in duplicateLevelIDs)
            {
                foreach (var characteristic in CharacteristicStrings)
                {
                    foreach (var difficulty in difficulties)
                    {
                        string leaderboardID = levID + characteristic + difficulty.ToString();
                        var scores = _localLeaderboardsModel.GetScores(leaderboardID, LocalLeaderboardsModel.LeaderboardType.AllTime);

                        if (scores != null && (playerName == null || scores.Any(x => x._playerName == playerName)))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether the level has been full combo'd in party mode. 
        /// All duplicate custom beatmaps are treated as full combo'd if any of the duplicates have been full combo'd at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <param name="difficulties">A list of difficulties to check for a full combo (optional).</param>
        /// <param name="playerName">The name of the player on the local leaderboards (optional).</param>
        /// <returns>True if the player(s) has/have achieved a full combo on the beatmap, otherwise false</returns>
        public bool HasFullComboForLevel(string levelID, List<BeatmapDifficulty> difficulties = null, string playerName = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties == null || difficulties.Count == 0)
                difficulties = AllDifficulties;

            // get any level duplicates
            List<string> duplicateLevelIDs = new List<string>();
            if (levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
            {
                foreach (var duplicateLevel in Loader.CustomLevelsCollection.beatmapLevels.Where(x => x.levelID.StartsWith(levelID)))
                    duplicateLevelIDs.Add(duplicateLevel.levelID);

                if (!duplicateLevelIDs.Contains(levelID))
                    duplicateLevelIDs.Add(levelID);
            }
            else
            {
                duplicateLevelIDs.Add(levelID);
            }

            foreach (var levID in duplicateLevelIDs)
            {
                foreach (var characteristic in CharacteristicStrings)
                {
                    foreach (var difficulty in difficulties)
                    {
                        string leaderboardID = levID + characteristic + difficulty.ToString();
                        var scores = _localLeaderboardsModel.GetScores(leaderboardID, LocalLeaderboardsModel.LeaderboardType.AllTime);

                        if (scores != null)
                        {
                            if (scores.Any(x => x._fullCombo && (x._playerName == playerName || playerName == null)))
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
