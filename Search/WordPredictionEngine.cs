using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace EnhancedSearchAndFilters.Search
{
    public class WordPredictionEngine : PersistentSingleton<WordPredictionEngine>
    {
        private WordCountStorage _activeWordStorage = null;
        private Dictionary<string, WordCountStorage> _cache = new Dictionary<string, WordCountStorage>();

        // NOTE: this regex keeps apostrophes
        public static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ']");
        public static readonly char[] SpaceCharArray = new char[] { ' ' };

        public const int SuggestedWordsCountThreshold = 10;

        private WordPredictionEngine()
        { }

        public void SetActiveWordStorageFromLevelPack(IBeatmapLevelPack levelPack)
        {
            if (!_cache.TryGetValue(levelPack.packName, out var storage))
            {
                storage = new WordCountStorage(levelPack);

                // never cache filtered level packs
                if (!(levelPack.packName == UI.SongListUI.FilteredSongsPackName) &&
                    !Tweaks.SongBrowserTweaks.IsFilterApplied())
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
        public List<SuggestedWord> GetSuggestedWords(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery) || _activeWordStorage == null || !_activeWordStorage.IsReady)
                return new List<SuggestedWord>();

            string[] words = searchQuery.Split(SpaceCharArray, StringSplitOptions.RemoveEmptyEntries);
            List<SuggestedWord> suggestedWords = new List<SuggestedWord>();

            // query only had spaces
            if (words.Length == 0)
                return suggestedWords;

            string lastWord = words[words.Length - 1];

            if (searchQuery[searchQuery.Length - 1] != ' ')
            {
                suggestedWords.AddRange(
                    _activeWordStorage.GetWordsWithPrefix(lastWord)
                        .Select(word => new SuggestedWord(word, SuggestedWord.SuggestionType.Prefixed)));
            }

            if (suggestedWords.Count >= SuggestedWordsCountThreshold)
                return suggestedWords;

            suggestedWords.AddRange(
                _activeWordStorage.GetFollowUpWords(lastWord)
                    .Where(word => !suggestedWords.Any(suggested => suggested.Word == word))
                    .Select(word => new SuggestedWord(word, SuggestedWord.SuggestionType.FollowUp)));

            // if we still don't reach the threshold of suggestions, assume there's a/some typo(s) => use fuzzy string matching
            if (suggestedWords.Count >= SuggestedWordsCountThreshold)
                return suggestedWords;

            int tolerance = Math.Min(2, Convert.ToInt32(lastWord.Length * 0.7f));
            suggestedWords.AddRange(
                _activeWordStorage.GetFuzzyMatchedWords(lastWord, tolerance)
                    .Where(word => !suggestedWords.Any(suggested => suggested.Word == word))
                    .Select(word => new SuggestedWord(word, SuggestedWord.SuggestionType.FuzzyMatch)));

            if (suggestedWords.Count >= SuggestedWordsCountThreshold)
                return suggestedWords;

            suggestedWords.AddRange(
                _activeWordStorage.GetFuzzyMatchedWordsAlternate(lastWord)
                    .Where(word => !suggestedWords.Any(suggested => suggested.Word == word))
                    .Select(word => new SuggestedWord(word, SuggestedWord.SuggestionType.FuzzyMatch)));

            return suggestedWords;
        }
    }

    public struct SuggestedWord
    {
        public string Word;
        public SuggestionType Type;

        public SuggestedWord(string word, SuggestionType type)
        {
            Word = word;
            Type = type;
        }

        public enum SuggestionType
        {
            Prefixed,
            FollowUp,
            FuzzyMatch
        }
    }
}
