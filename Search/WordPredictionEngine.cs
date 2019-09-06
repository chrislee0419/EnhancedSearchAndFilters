using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        // NOTE: this regex keeps apostrophes
        public static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ']");

        private WordCountStorage _activeWordStorage = null;
        private Dictionary<string, WordCountStorage> _cache = new Dictionary<string, WordCountStorage>();

        private WordPredictionEngine()
        { }

        public void SetActiveWordStorageFromLevelPack(IBeatmapLevelPack levelPack)
        {
            if (!_cache.TryGetValue(levelPack.packName, out var storage))
            {
                storage = new WordCountStorage(levelPack);
                _cache[levelPack.packName] = storage;
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

        /// <summary>
        /// Clear all existing WordCountStorage objects. This should be used when songs are refreshed.
        /// </summary>
        public void ClearCache()
        {
            Logger.log.Info("Clearing word prediction storage cache");

            _cache.Clear();
            _activeWordStorage = null;
        }

        /// <summary>
        /// Gets a list of suggested words from the active word storage, based on what was typed by the user.
        /// </summary>
        /// <param name="searchQuery">The typed search query.</param>
        /// <returns>A list of suggested words.</returns>
        public List<string> GetSuggestedWords(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery) || _activeWordStorage == null || !_activeWordStorage.IsReady)
                return new List<string>();

            string[] words = searchQuery.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            List<string> suggestedWords = new List<string>();

            // query only had spaces
            if (words.Length == 0)
                return suggestedWords;

            string lastWord = words[words.Length - 1];

            if (searchQuery[searchQuery.Length - 1] != ' ')
                suggestedWords.AddRange(_activeWordStorage.GetWordsWithPrefix(lastWord));
            suggestedWords.AddRange(_activeWordStorage.GetFollowUpWords(lastWord));

            return suggestedWords;
        }
    }
}
