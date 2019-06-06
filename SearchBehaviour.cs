using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EnhancedSearchAndFilters
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
                }

                _instance.gameObject.SetActive(true);
                return _instance;
            }
            private set
            {
                _instance = value;
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

        private const int MaxSearchInOneFrame = 200;
        private static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ]");

        public void StartNewSearch(IPreviewBeatmapLevel[] searchSpace, string searchQuery, Action<IPreviewBeatmapLevel[]> action)
        {
            if (searchSpace == null || searchSpace.Count() < 1 || action == null)

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
                queryWords = searchQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            else
                queryWords = new string[] { searchQuery };

            while (index < _searchSpace.Count)
            {
                // limit the number of songs to evaluate so users with large song libraries don't pause on every key press
                for (int count = 0; count < MaxSearchInOneFrame && index < _searchSpace.Count; ++count)
                {
                    IPreviewBeatmapLevel level = _searchSpace[index];
                    string fields;

                    if (songFields == SearchableSongFields.All)
                        fields = $"{level.songName} {level.songSubName} {level.levelAuthorName} {level.songAuthorName}".ToLower();
                    else if (songFields == SearchableSongFields.TitleOnly)
                        fields = $"{level.songName}".ToLower();
                    //else if (songFields == SearchableSongFields.TitleAndSubtitle)
                    else
                        fields = $"{level.songName} {level.songSubName}".ToLower();

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
