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

                // NOTE: could return null if playerData somehow is null
                return _instance;
            }
        }

        private static PlayerData _playerData;

        public bool HasCompletedLevel(string levelID)
        {
            if (levelID.StartsWith("custom_level_"))
                levelID = levelID.Substring(0, 53);
            return _playerData.levelsStatsData.Any(x => x.levelID.StartsWith(levelID) && x.validScore);
        }
    }
}
