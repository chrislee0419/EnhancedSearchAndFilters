using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EnhancedSearchAndFilters.Search
{
    class SearchBehaviour : MonoBehaviour
    {
        private static SearchBehaviour _instance;
        public static SearchBehaviour Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("EnhancedSearchBehaviour").AddComponent<SearchBehaviour>();
                    DontDestroyOnLoad(_instance.gameObject);

                    MaxSearchInOneFrame = PluginConfig.MaxSongsToSearchInOneFrame;
                }

                _instance.gameObject.SetActive(true);
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public IPreviewBeatmapLevel[] CachedResult
        {
            get
            {
                return IsSearching ? null : _searchSpace.ToArray();
            }
        }

        private Action<IPreviewBeatmapLevel[]> _searchCompletedAction;

        private Coroutine _searchCoroutine;
        private List<IPreviewBeatmapLevel> _searchSpace;

        public bool IsSearching
        {
            get
            {
                return _searchCoroutine != null;
            }
        }

        private static int MaxSearchInOneFrame;
        private static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ]");
        private static readonly char[] SplitCharacters = new char[] { ' ' };

        public void StartNewSearch(IPreviewBeatmapLevel[] searchSpace, string searchQuery, Action<IPreviewBeatmapLevel[]> action)
        {
            if (searchSpace == null || searchSpace.Count() < 1 || action == null)
                return;

            if (_searchCoroutine != null)
                StopSearch();

            _searchCompletedAction = action;
            _searchSpace = searchSpace.ToList();
            _searchCoroutine = StartCoroutine(_SearchSongs(searchQuery));
        }

        /// <summary>
        /// Used to query an already filtered song list. Only use this method to further refine an existing search result. 
        /// Otherwise, start a new search using StartNewSearch().
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="action"></param>
        public void StartSearchOnExistingList(string searchQuery, Action<IPreviewBeatmapLevel[]> action = null)
        {
            if (_searchSpace == null || _searchSpace.Count < 1)
            {
                action?.Invoke(new IPreviewBeatmapLevel[] { });
                return;
            }

            if (action == null)
                action = _searchCompletedAction;
            if (_searchCoroutine != null)
                StopSearch();

            _searchCompletedAction = action;
            _searchCoroutine = StartCoroutine(_SearchSongs(searchQuery));
        }

        public void StopSearch()
        {
            if (_searchCoroutine != null)
            {
                StopCoroutine(_searchCoroutine);
                _searchCoroutine = null;
            }
            _searchCompletedAction = null;
        }

        private IEnumerator _SearchSongs(string searchQuery)
        {
            int index = 0;
            string[] queryWords;
            bool stripSymbols = PluginConfig.StripSymbols;
            bool splitWords = PluginConfig.SplitQueryByWords;
            SearchableSongFields songFields = PluginConfig.SongFieldsToSearch;

            if (stripSymbols)
                searchQuery = RemoveSymbolsRegex.Replace(searchQuery.ToLower(), string.Empty);
            else
                searchQuery = searchQuery.ToLower();

            if (splitWords)
                queryWords = searchQuery.Split(SplitCharacters, StringSplitOptions.RemoveEmptyEntries);
            else
                queryWords = new string[] { searchQuery };

            while (index < _searchSpace.Count)
            {
                // limit the number of songs to evaluate so users with large song libraries don't pause on every key press
                for (int count = 0; count < MaxSearchInOneFrame && index < _searchSpace.Count; ++count)
                {
                    IPreviewBeatmapLevel level = _searchSpace[index];
                    string songName;

                    if (splitWords)
                    {
                        // combine contiguous single letter 'word' sequences in the title each into one word
                        // only done when Split Words option is enabled
                        StringBuilder songNameSB = new StringBuilder(level.songName.Length);
                        StringBuilder constructedWordSB = new StringBuilder(level.songName.Length);

                        foreach (string word in level.songName.Split(SplitCharacters, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (word.Length > 1 || !char.IsLetterOrDigit(word[0]))
                            {
                                // multi-letter word or special character
                                if (constructedWordSB.Length > 0)
                                {
                                    if (songNameSB.Length > 0)
                                        songNameSB.Append(' ');

                                    songNameSB.Append(constructedWordSB.ToString());
                                    constructedWordSB.Clear();
                                }

                                if (songNameSB.Length > 0)
                                    songNameSB.Append(' ');

                                songNameSB.Append(word);

                            }
                            else
                            {
                                // single letter 'word'
                                constructedWordSB.Append(word);
                            }
                        }

                        // add last constructed word if it exists
                        if (constructedWordSB.Length > 0)
                        {
                            if (songNameSB.Length > 0)
                                songNameSB.Append(' ');

                            songNameSB.Append(constructedWordSB.ToString());
                        }

                        songName = songNameSB.ToString();
                    }
                    else
                    {
                        songName = level.songName;
                    }

                    string fields;
                    if (songFields == SearchableSongFields.All)
                        fields = $"{songName} {level.songSubName} {level.levelAuthorName} {level.songAuthorName}".ToLower();
                    else if (songFields == SearchableSongFields.TitleOnly)
                        fields = $"{songName} {level.songSubName}".ToLower();
                    //else if (songFields == SearchableSongFields.TitleAndAuthor)
                    else
                        fields = $"{songName} {level.songSubName} {level.songAuthorName}".ToLower();

                    if (stripSymbols)
                        fields = RemoveSymbolsRegex.Replace(fields, string.Empty);

                    bool remove = false;
                    foreach (var word in queryWords)
                    {
                        if (!fields.Contains(word))
                        {
                            remove = true;
                            break;
                        }
                    }
                    if (remove)
                        _searchSpace.RemoveAt(index);
                    else
                        ++index;
                }
                yield return null;
            }

            _searchCompletedAction?.Invoke(_searchSpace.ToArray());
            _searchCoroutine = null;
        }
    }
}
