using System.Collections.Generic;

namespace EnhancedSearchAndFilters.Search
{
    public class WordPredictionEngine
    {
        private static WordPredictionEngine _instance = null;
        public static WordPredictionEngine Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WordPredictionEngine();
                }
                return _instance;
            }
        }

        private WordCountStorage _activeWordStorage = null;
        private Dictionary<string, WordCountStorage> _cache = new Dictionary<string, WordCountStorage>();

        private WordPredictionEngine()
        { }

        public void SetActiveWordStorageFromLevelPack(IBeatmapLevelPack levelPack)
        {
            if (!_cache.TryGetValue(levelPack.packName, out var storage))
            {
                storage = new WordCountStorage(levelPack);
                _cache[levelPack.packID] = storage;
            }

            _activeWordStorage = storage;
        }

        /// <summary>
        /// Pause ongoing WordCountStorage creation tasks. This should be used when the player is playing 
        /// the game to reduce whatever performance penalty these background tasks could have.
        /// </summary>
        public void PauseTasks()
        {
            foreach (var storage in _cache.Values)
                storage.PauseSetup();
        }

        /// <summary>
        /// Resume tasks that were paused with PauseTasks().
        /// </summary>
        public void ResumeTasks()
        {
            foreach (var storage in _cache.Values)
                storage.ResumeSetup();
        }

        /// <summary>
        /// Cancel all ongoing WordCountStorage creation tasks. This should be used during application quit 
        /// and when custom songs are reloaded.
        /// </summary>
        public void CancelTasks()
        {
            foreach (var storage in _cache.Values)
                storage.CancelSetup();
        }

        public List<string> GetWordsWithPrefix(string prefix)
        {
            return _activeWordStorage?.GetWordsWithPrefix(prefix) ?? new List<string>();
        }
    }
}
