using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnhancedSearchAndFilters.Search
{
    internal class WordCountStorage
    {
        public bool IsLoading { get; private set; } = false;
        public bool IsReady { get; private set; } = false;

        private Trie _trie = new Trie();
        private Dictionary<string, int> _counts = new Dictionary<string, int>();

        private HMTask _task;
        private ManualResetEvent _manualResetEvent;
        private bool _taskCancelled;

        private static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ']");
        private static readonly char[] SplitStrings = new char[] { ' ' };

        public WordCountStorage()
        { }

        public WordCountStorage(IBeatmapLevelPack levelPack)
        {
            SetupStorage(levelPack);
        }

        /// <summary>
        /// Populate word storage with the words in the song name, sub-name, author, and map creator of a level pack.
        /// </summary>
        /// <param name="levelPack">The level pack whose words you want to store.</param>
        public void SetupStorage(IBeatmapLevelPack levelPack)
        {
            IsLoading = true;
            _manualResetEvent = new ManualResetEvent(true);
            _taskCancelled = false;

            _task = new HMTask(
                delegate ()
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    Logger.log.Info($"Creating word count storage object for the \"{levelPack.packName}\" level pack (contains {levelPack.beatmapLevelCollection.beatmapLevels.Length} songs)");

                    if (!SetWordsFromLevelPack(levelPack))
                        return;

                    sw.Stop();
                    Logger.log.Info($"Finished creating word count storage object for the \"{levelPack.packName}\" level pack (took {sw.ElapsedMilliseconds/1000f} seconds)");
                },
                delegate ()
                {
                    _manualResetEvent = null;
                    _task = null;
                    IsLoading = false;
                });

            _task.Run();
        }

        /// <summary>
        /// Pause the setup task if it exists.
        /// </summary>
        public void PauseSetup()
        {
            if (!IsLoading || _manualResetEvent == null || _task == null)
                return;

            _manualResetEvent.Reset();
            Logger.log.Info("Blocking word count storage setup thread");
        }

        /// <summary>
        /// Resume the setup task if it exists.
        /// </summary>
        public void ResumeSetup()
        {
            if (!IsLoading || _manualResetEvent == null || _task == null)
                return;

            _manualResetEvent.Set();
            Logger.log.Info("Resuming word count storage setup thread");
        }

        /// <summary>
        /// Cancels the setup task if it exists.
        /// </summary>
        public void CancelSetup()
        {
            if (!IsLoading || _manualResetEvent == null || _task == null)
                return;

            _taskCancelled = true;
            _manualResetEvent.Set();
            Logger.log.Info("Cancelling word count storage setup thread");
        }

        private bool SetWordsFromLevelPack(IBeatmapLevelPack levelPack)
        {
            List<string> words = new List<string>();
            foreach (var level in levelPack.beatmapLevelCollection.beatmapLevels)
            {
                _manualResetEvent.WaitOne();
                if (_taskCancelled)
                    return false;

                words.AddRange(GetWordsFromString(level.songName));
                words.AddRange(GetWordsFromString(level.songSubName));
                words.AddRange(GetWordsFromString(level.songAuthorName));
                foreach (var author in GetWordsFromString(level.levelAuthorName))
                {
                    // since the names of map makers occur very frequently,
                    // we limit them to only one entry
                    // otherwise, they always show up as the first couple of predictions
                    if (!words.Contains(author))
                        words.Add(author);
                }
            }

            // sort by word length in descending order
            words.Sort((x, y) => y.Length - x.Length);

            // add words to the storage
            foreach (var word in words)
            {
                _manualResetEvent.WaitOne();
                if (_taskCancelled)
                    return false;

                if (_counts.ContainsKey(word))
                {
                    _counts[word] += 1;
                    continue;
                }

                // get count of words that have this word as a prefix
                int count = 1;
                foreach (var prefixedWord in _trie.StartsWith(word))
                    count += _counts[prefixedWord];

                _trie.AddWord(word);
                _counts.Add(word, count);
            }

            IsReady = true;
            return true;
        }

        /// <summary>
        /// Get words that start with a given prefix, sorted by their counts in descending order.
        /// </summary>
        /// <param name="prefix">Find words that start with this prefix.</param>
        /// <returns>A list of words.</returns>
        public List<string> GetWordsWithPrefix(string prefix)
        {
            if (!IsReady)
                return new List<string>();
            else
                return _trie.StartsWith(prefix.ToLower()).OrderByDescending(s => _counts[s]).ToList();
        }

        private string[] GetWordsFromString(string s)
        {
            return RemoveSymbolsRegex.Replace(s.ToLower(), " ").Split(SplitStrings, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 2).ToArray();
        }
    }
}
