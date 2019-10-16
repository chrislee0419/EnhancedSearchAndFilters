using System.Linq;
using UnityEngine;

namespace EnhancedSearchAndFilters.SongData
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
                    var playerDataModelSO = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();
                    _playerData = playerDataModelSO?.playerData;

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
        /// All duplicate custom beatmaps are treated as completed if any of the duplicates have been completed at least once.
        /// </summary>
        /// <param name="levelID">The level ID of the beatmap.</param>
        /// <returns>True if the player has completed the beatmap at least once, otherwise false.</returns>
        public bool HasCompletedLevel(string levelID)
        {
            if (levelID.StartsWith("custom_level_"))
                levelID = levelID.Substring(0, 53);

            return _playerData.levelsStatsData.Any(x => x.levelID.StartsWith(levelID) && x.validScore);
        }
    }
}
