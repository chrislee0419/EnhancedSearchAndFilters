using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SongCore;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Utilities
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

        public static readonly string[] AllCharacteristicStrings = new string[]
        {
            "",             // 'Standard' characteristic maps to empty string
            "NoArrows",
            "OneSaber",
            "Lawless",
            "Lightshow",
            "90Degree",
            "360Degree"
        };
        public static readonly IEnumerable<BeatmapDifficulty> AllDifficulties = Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>();

        /// <summary>
        /// Check whether the level has been successfully completed in party mode. 
        /// All duplicate custom beatmaps are treated as completed if any of the duplicates have been completed at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <param name="difficulties">A list of difficulties to check for level completion (optional).</param>
        /// <param name="playerName">The name of the player on the local leaderboards (optional).</param>
        /// <returns>True if the player(s) has/have completed the beatmap at least once, otherwise false.</returns>
        public bool HasCompletedLevel(string levelID, IEnumerable<BeatmapDifficulty> difficulties = null, string playerName = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties == null || difficulties.Count() == 0)
                difficulties = AllDifficulties;

            // get any level duplicates
            List<string> duplicateLevelIDs = GetActualLevelIDs(levelID);

            StringBuilder sb = new StringBuilder();
            foreach (var levID in duplicateLevelIDs)
            {
                foreach (var characteristic in AllCharacteristicStrings)
                {
                    foreach (var difficulty in difficulties)
                    {
                        sb.Clear();
                        sb.Append(levID);
                        sb.Append(characteristic);
                        sb.Append(difficulty.ToString());
                        string leaderboardID = sb.ToString();
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
        public bool HasFullComboForLevel(string levelID, IEnumerable<BeatmapDifficulty> difficulties = null, string playerName = null)
        {
            levelID = BeatmapDetailsLoader.GetSimplifiedLevelID(levelID);

            if (difficulties == null || difficulties.Count() == 0)
                difficulties = AllDifficulties;

            // get any level duplicates
            List<string> duplicateLevelIDs = GetActualLevelIDs(levelID);

            StringBuilder sb = new StringBuilder();
            foreach (var levID in duplicateLevelIDs)
            {
                foreach (var characteristic in AllCharacteristicStrings)
                {
                    foreach (var difficulty in difficulties)
                    {
                        sb.Clear();
                        sb.Append(levID);
                        sb.Append(characteristic);
                        sb.Append(difficulty.ToString());
                        string leaderboardID = sb.ToString();
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

        /// <summary>
        /// Returns the highest rank achieved for a level and a given list of difficulties. 
        /// NOTE: this method obtains the rank by performing a calculation on the set score and assumes that no modifiers were used.
        /// </summary>
        /// <param name="level">The level to search through.</param>
        /// <param name="difficulties">A list of BeatmapDifficulties to search through. Use null to search through all difficulties.</param>
        /// <param name="characteristics">A list of characteristics to search through. Each characteristic is represented by its serialized string. 
        /// Use null to search through all characteristics.</param>
        /// <param name="playerName">The name of the player on the local leaderboards (optional).</param>
        /// <returns>The highest RankModel.Rank enum found for the selected difficulties, or null if the level has not yet been completed.</returns>
        public RankModel.Rank? GetHighestRankForLevel(BeatmapDetails level, IEnumerable<BeatmapDifficulty> difficulties = null, IEnumerable<string> characteristics = null, string playerName = null)
        {
            if (difficulties == null)
                difficulties = AllDifficulties;
            if (characteristics == null)
                characteristics = AllCharacteristicStrings;

            // get any level duplicates
            List<string> duplicateLevelIDs = GetActualLevelIDs(level.LevelID);

            StringBuilder sb = new StringBuilder();
            RankModel.Rank? highestRank = null;
            foreach (var levID in duplicateLevelIDs)
            {
                foreach (var characteristic in AllCharacteristicStrings)
                {
                    var simplifiedChar = level.DifficultyBeatmapSets.FirstOrDefault(x => x.CharacteristicName == characteristic || (characteristic == "" && x.CharacteristicName == "Standard"));
                    if (simplifiedChar == null)
                        continue;

                    foreach (var difficulty in difficulties)
                    {
                        var simplifiedDiff = simplifiedChar.DifficultyBeatmaps.FirstOrDefault(x => x.Difficulty == difficulty);
                        if (simplifiedDiff == null)
                            continue;

                        sb.Clear();
                        sb.Append(levID);
                        sb.Append(characteristic);
                        sb.Append(difficulty.ToString());
                        string leaderboardID = sb.ToString();
                        var scores = _localLeaderboardsModel.GetScores(leaderboardID, LocalLeaderboardsModel.LeaderboardType.AllTime);

                        int maxRawScore = ScoreModel.MaxRawScoreForNumberOfNotes(simplifiedDiff.NotesCount);
                        if (scores != null)
                        {
                            var validEntries = scores.Where(x => x._score != 0 && (x._playerName == playerName || playerName == null));

                            if (validEntries.Count() > 0)
                            {
                                var validRanks = validEntries.Select(x => RankModel.GetRankForScore(x._score, x._score, maxRawScore, maxRawScore));
                                highestRank = (RankModel.Rank)Math.Max((int)(highestRank ?? RankModel.Rank.E), (int)validRanks.Max());
                            }
                        }
                    }
                }
            }

            return highestRank;
        }

        private List<string> GetActualLevelIDs(string levelID)
        {
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

            return duplicateLevelIDs;
        }
    }
}
