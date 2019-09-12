using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedSearchAndFilters.Search
{
    internal class WordCountStorage
    {
        public bool IsLoading { get; private set; } = false;
        public bool IsReady { get; private set; } = false;

        private Trie _trie = new Trie();
        private Dictionary<string, WordInformation> _words = new Dictionary<string, WordInformation>();

        private HMTask _task;
        private ManualResetEvent _manualResetEvent;
        private bool _taskCancelled;

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
            // we don't build the _words object immediately only because we want to also add the
            // counts of other words that are prefixed by a word to the count
            List<string> allWords = new List<string>();
            Dictionary<string, Dictionary<string, int>> allWordConnections = new Dictionary<string, Dictionary<string, int>>();

            foreach (var level in levelPack.beatmapLevelCollection.beatmapLevels)
            {
                _manualResetEvent.WaitOne();
                if (_taskCancelled)
                    return false;

                var songNameWords = GetWordsFromString(level.songName);
                var songSubNameWords = GetWordsFromString(level.songSubName);
                var authorNameWords = GetWordsFromString(level.songAuthorName);
                var levelAuthors = GetWordsFromString(level.levelAuthorName);

                string[][] wordsFromSong = new string[][]
                {
                    songNameWords,
                    songSubNameWords,
                    authorNameWords
                };

                foreach (var wordsFromField in wordsFromSong)
                {
                    for (int i = 0; i < wordsFromField.Length; ++i)
                    {
                        var currentWord = wordsFromField[i];
                        allWords.Add(currentWord);

                        if (!allWordConnections.ContainsKey(currentWord))
                            allWordConnections[currentWord] = new Dictionary<string, int>();

                        if (i + 1 < wordsFromField.Length)
                        {
                            var nextWord = wordsFromField[i + 1];
                            var connections = allWordConnections[currentWord];

                            if (connections.ContainsKey(nextWord))
                                connections[nextWord] += 1;
                            else
                                connections.Add(nextWord, 1);
                        }
                    }
                }

                // last word of song name connects to the first word of subname and all mappers
                var lastWord = songNameWords.LastOrDefault();
                if (!string.IsNullOrEmpty(lastWord))
                {
                    var connections = allWordConnections[lastWord];
                    string[] firstWords = levelAuthors.Append(songSubNameWords.FirstOrDefault()).ToArray();

                    foreach (var firstWord in firstWords)
                    {
                        // only make a connection once (same thing for the below connections)
                        if (!string.IsNullOrEmpty(firstWord) && !connections.ContainsKey(firstWord))
                             connections.Add(firstWord, 1);
                    }
                }

                // last word of song subname connects to first word of author
                lastWord = songSubNameWords.LastOrDefault();
                if (!string.IsNullOrEmpty(lastWord))
                {
                    var connections = allWordConnections[lastWord];
                    var firstWord = authorNameWords.FirstOrDefault();

                    if (!string.IsNullOrEmpty(firstWord) && !connections.ContainsKey(firstWord))
                        connections.Add(firstWord, 1);
                }

                // last word of author name connects to first word of song name
                lastWord = authorNameWords.LastOrDefault();
                if (!string.IsNullOrEmpty(lastWord))
                {
                    var connections = allWordConnections[lastWord];
                    var firstWord = songNameWords.FirstOrDefault();

                    if (!string.IsNullOrEmpty(firstWord) && !connections.ContainsKey(firstWord))
                        connections.Add(firstWord, 1);
                }

                // level authors are added to the word storage differently from the other fields
                var firstSongNameWord = songNameWords.FirstOrDefault();
                for (int i = 0; i < levelAuthors.Length; ++i)
                {
                    var author = levelAuthors[i];

                    // since the names of map makers occur very frequently, we limit them to only one entry
                    // otherwise, they always show up as the first couple of predictions
                    if (!allWords.Contains(author))
                        allWords.Add(author);

                    Dictionary<string, int> levelAuthorConnections;
                    if (!allWordConnections.ContainsKey(author))
                    {
                        levelAuthorConnections = new Dictionary<string, int>();
                        allWordConnections[author] = levelAuthorConnections;
                    }
                    else
                    {
                        levelAuthorConnections = allWordConnections[author];
                    }

                    // make connection between this mapper and the first word of the song name
                    if (!string.IsNullOrEmpty(firstSongNameWord) && !levelAuthorConnections.ContainsKey(firstSongNameWord))
                        levelAuthorConnections.Add(firstSongNameWord, 1);
                }
            }

            // sort by word length in descending order
            allWords.Sort((x, y) => y.Length - x.Length);

            // add words to the storage
            foreach (var word in allWords)
            {
                _manualResetEvent.WaitOne();
                if (_taskCancelled)
                    return false;

                if (_words.ContainsKey(word))
                {
                    _words[word].Count += 1;
                    continue;
                }

                // get count of words that have this word as a prefix
                int count = 1;
                foreach (var prefixedWord in _trie.StartsWith(word))
                    count += _words[prefixedWord].Count;

                _trie.AddWord(word);
                _words.Add(
                    word,
                    new WordInformation(count,
                        allWordConnections[word].ToList()
                        .OrderByDescending(x => x.Value)
                        .Select(p => p.Key)
                        .ToList())
                );
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
                return _trie.StartsWith(prefix.ToLower()).OrderByDescending(s => _words[s].Count).ToList();
        }

        /// <summary>
        /// Gets the words that appear after this word, sorted by occurence.
        /// </summary>
        /// <param name="word">Find words that appear after this word.</param>
        /// <returns>A list of words.</returns>
        public List<string> GetFollowUpWords(string word)
        {
            if (!IsReady || !_words.TryGetValue(word.ToLower(), out var wordInfo))
                return new List<string>();

            return wordInfo.FollowUpWords;
        }

        private string[] GetWordsFromString(string s)
        {
            return WordPredictionEngine.RemoveSymbolsRegex.Replace(s.ToLower(), " ").Split(SplitStrings, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 2).ToArray();
        }
    }

    internal class WordInformation
    {
        /// <summary>
        /// Number of occurences of this word in the level pack.
        /// </summary>
        public int Count;

        /// <summary>
        /// Words that come immediately after this word, sorted by the number of occurences.
        /// </summary>
        public List<string> FollowUpWords;

        public WordInformation(int count, List<string> followUpWords)
        {
            Count = count;
            FollowUpWords = followUpWords;
        }
    }
}
