using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using SongCore;
using CustomUI.Utilities;

namespace EnhancedSearchAndFilters.SongData
{
    class BeatmapDetailsLoader : MonoBehaviour
    {
        private static BeatmapDetailsLoader _instance;
        public static BeatmapDetailsLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First().GetPrivateField<BeatmapLevelsModelSO>("_beatmapLevelsModel");
                    _customLevelLoader = _beatmapLevelsModel.GetPrivateField<CustomLevelLoaderSO>("_customLevelLoader");

                    _instance = new GameObject("BeatmapLoader").AddComponent<BeatmapDetailsLoader>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        public bool IsLoading { get; private set; } = false;
        public bool IsCaching { get; private set; } = false;
        public bool SongsAreCached { get; private set; } = false;

        private static BeatmapLevelsModelSO _beatmapLevelsModel;
        private static CustomLevelLoaderSO _customLevelLoader;

        private HMTask _cachingTask;
        private ManualResetEvent _manualResetEvent;
        private CancellationTokenSource _cachingTokenSource;

        private HMTask _loadingTask;
        private Action<int> _update;
        private CancellationTokenSource _loadingTokenSource;

        private IPreviewBeatmapLevel[] _levels;
        private List<BeatmapDetails> _loadedLevels;
        private List<Tuple<int, BeatmapDetails>> _loadedLevelsUnsorted = new List<Tuple<int, BeatmapDetails>>();

        // maintain our own cache because we don't need load the preview audio or cover image of each level and the game's HMCache is limited to 30 songs
        private ConcurrentDictionary<string, BeatmapDetails> _cache = new ConcurrentDictionary<string, BeatmapDetails>();

        // load the details in batches (there is a noticable delay with queueing all async load tasks at once)
        private const int WorkChunkSize = 5;

        private static string CachedBeatmapDetailsFilePath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedFilterDetailsCache.json");

        /// <summary>
        /// Should be run whenever in the menu, to start/resume populating the cache for filter/search.
        /// </summary>
        public void StartPopulatingCache(bool force = false)
        {
            if (IsLoading || (SongsAreCached && !force && !IsCaching))
                return;

            if (!IsCaching)
            {
                IsCaching = true;
                _manualResetEvent = new ManualResetEvent(true);
                _cachingTokenSource = new CancellationTokenSource();

                _cachingTask = new HMTask(
                    delegate ()
                    {
                        Logger.log.Info("Starting to cache all custom song details");

                        CacheAllBeatmapLevelsAsync();
                        SaveCacheToFile();

                        Logger.log.Info("Finished caching and storing all custom song details");
                    },
                    delegate ()
                    {
                        _manualResetEvent = null;
                        _cachingTask = null;

                        SongsAreCached = true;
                        IsCaching = false;
                    });

                _cachingTask.Run();
            }
            else
            {
                // resume old populate cache job
                _manualResetEvent.Set();
                Logger.log.Info("Resuming beatmap details caching thread");
            }
        }

        /// <summary>
        /// Blocks the thread that is populating the cache. Can be resumed with StartPopulatingCache().
        /// </summary>
        public void PausePopulatingCache()
        {
            if (!IsCaching || _manualResetEvent == null || _cachingTask == null)
                return;

            _manualResetEvent.Reset();
            Logger.log.Info("Blocking beatmap details caching thread");
        }

        /// <summary>
        /// Only used to stop the caching thread during OnApplicationQuit().
        /// </summary>
        public void CancelPopulatingCache()
        {
            if (!IsCaching || _cachingTokenSource == null || _cachingTask == null)
                return;

            _cachingTokenSource.Cancel();
            _cachingTask.Cancel();

            _cachingTokenSource = null;
            _cachingTask = null;
            IsCaching = false;
        }

        public async Task PopulateCacheFromFile()
        {
            var detailsList = await BeatmapDetails.GetBeatmapDetailsFromCacheAsync(CachedBeatmapDetailsFilePath);
            foreach (var detail in detailsList)
                _cache.TryAdd(detail.LevelID, detail);
        }

        /// <summary>
        /// Save the cached BeatmapDetails objects to a JSON file located at CachedBeatmapDetailsFilePath.
        /// </summary>
        public void SaveCacheToFile()
        {
            List<BeatmapDetails> cache = _cache.Values.ToList();

            // remove WIP levels from cache (don't save them, since they're likely to change often and
            // we don't delete any entries in the cache, it may cause the cache to balloon in size
            var wipLevels = Loader.CustomWIPLevels.Values.Select(x => x.levelID);
            cache = cache.Where(c => !wipLevels.Any(wip => wip == c.LevelID)).ToList();

            // remove beatmaps that are not loaded
            var customLevels = Loader.CustomLevels.Values.Select(x => x.levelID);
            cache = cache.Where(c => customLevels.Any(level => level == c.LevelID)).ToList();

            BeatmapDetails.SaveBeatmapDetailsToCache(CachedBeatmapDetailsFilePath, cache);
        }

        /// <summary>
        /// Loads a single custom beatmap level.
        /// </summary>
        /// <param name="level">The custom preview beatmap for which you want to load the IBeatmapLevel for.</param>
        /// <param name="onFinish">The function that is called when the IBeatmapLevel is retrieved.</param>
        /// <returns>An awaitable Task.</returns>
        public async Task LoadSingleBeatmapAsync(CustomPreviewBeatmapLevel level, Action<IBeatmapLevel> onFinish)
        {
            IBeatmapLevel beatmapLevel = await LoadCustomBeatmapLevelAsync(level, CancellationToken.None);

            onFinish?.Invoke(beatmapLevel);
        }

        /// <summary>
        /// Loads the beatmap details of a list of IPreviewBeatmapLevels.
        /// </summary>
        /// <param name="levels">A list of IPreviewBeatmaps.</param>
        /// <param name="update">A function that will run every 0.1s that gets the number of beatmaps currently loaded.</param>
        /// <param name="onFinish">The function that is called when the details of all beatmaps are retrieved.</param>
        public void LoadBeatmaps(IPreviewBeatmapLevel[] levels, Action<int> update = null, Action<BeatmapDetails[]> onFinish = null)
        {
            if (IsLoading)
                _loadingTask?.Cancel();

            IsLoading = true;

            if (IsCaching)
                PausePopulatingCache();

            _levels = levels;

            _update = update;
            StartCoroutine(UpdateCoroutine());

            if (_loadingTokenSource != null)
                _loadingTokenSource.Dispose();
            _loadingTokenSource = new CancellationTokenSource();

            _loadingTask = new HMTask(
                delegate ()
                {
                    Logger.log.Debug("Starting to load beatmap details");

                    GetBeatmapLevelsAsync().GetAwaiter().GetResult();
                },
                delegate ()
                {
                    _loadingTask = null;
                    _loadingTokenSource.Dispose();
                    _loadingTokenSource = null;
                    IsLoading = false;

                    if (IsCaching || !SongsAreCached)
                        StartPopulatingCache();

                    onFinish?.Invoke(_loadedLevels.ToArray());
                });

            _loadingTask.Run();
        }

        /// <summary>
        /// Load beatmaps that have already been cached or are OST. It is highly recommended that LoadBeatmaps is used instead.
        /// </summary>
        /// <param name="levels">Array of IPreviewBeatmapLevel objects to get the beatmap details of.</param>
        /// <returns>Array of BeatmapDetails objects that represent the passed IPreviewBeatmapLevel objects.</returns>
        public BeatmapDetails[] LoadBeatmapsInstant(IPreviewBeatmapLevel[] levels)
        {
            BeatmapDetails[] detailsList = new BeatmapDetails[levels.Length];
            int unloadable = 0;

            for (int i = 0; i < levels.Length; ++i)
            {
                IPreviewBeatmapLevel level = levels[i];

                if (level is IBeatmapLevel)
                {
                    detailsList[i] = new BeatmapDetails(level as IBeatmapLevel);
                }
                else if (_cache.ContainsKey(level.levelID))
                {
                    detailsList[i] = _cache[level.levelID];
                }
                else
                {
                    // unable to load from cache or convert directly to BeatmapDetails object
                    detailsList[i] = null;
                    ++unloadable;
                }
            }

            if (unloadable > 0)
                Logger.log.Warn($"LoadBeatmapsInstant was unable to retrieve all BeatmapDetails objects from cache ({unloadable} could not be loaded)");

            return detailsList;
        }

        public void CancelLoading()
        {
            if (!IsLoading || _loadingTokenSource == null || _loadingTask == null)
                return;

            IsLoading = false;

            _loadingTokenSource.Cancel();
            _loadingTask.Cancel();
            _loadingTokenSource = null;
            _loadingTask = null;

            if (IsCaching || !SongsAreCached)
                StartPopulatingCache();
        }

        private IEnumerator UpdateCoroutine()
        {
            while (IsLoading && _update != null)
            {
                _update.Invoke(_loadedLevelsUnsorted.Count);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void CacheAllBeatmapLevelsAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // load beatmap details from cache if it exists
            PopulateCacheFromFile().GetAwaiter().GetResult();

            // we don't have to cache OST levels, since they can immediately be cast into IBeatmapLevel objects
            List<IPreviewBeatmapLevel> allLevels = Loader.CustomLevelsCollection.beatmapLevels.ToList();

            List<Task> taskList = new List<Task>(WorkChunkSize);
            int index = 0;
            while (index < allLevels.Count)
            {
                _manualResetEvent.WaitOne();

                if (_cachingTokenSource.IsCancellationRequested)
                    return;

                for (int i = 0; i < WorkChunkSize && index < allLevels.Count; ++index)
                {
                    string levelID = allLevels[index].levelID;
                    if (!_cache.ContainsKey(levelID) || _cache[levelID].SongDuration == 0f)
                    {
                        if (Tweaks.SongDataCoreTweaks.GetBeatmapDetails(levelID, out var beatmapDetails))
                        {
                            // load the beatmap details manually if some data from BeatSaver is incomplete
                            if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                            {
                                Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. \n" +
                                    "Discarding and regenerating BeatmapDetails object from locally stored information instead.");
                                taskList.Add(CacheCustomBeatmapDetailsAsync(allLevels[index] as CustomPreviewBeatmapLevel));
                                ++i;
                            }
                            else
                            {
                                _cache[levelID] = beatmapDetails;
                            }
                        }
                        else
                        {
                            taskList.Add(CacheCustomBeatmapDetailsAsync(allLevels[index] as CustomPreviewBeatmapLevel));
                            ++i;
                        }
                    }
                }

                Task.WhenAll(taskList).GetAwaiter().GetResult();
                taskList.Clear();
            }

            sw.Stop();
            Logger.log.Info($"Finished caching the details of {allLevels.Count} beatmaps (took {sw.ElapsedMilliseconds/1000f} seconds).");
        }

        private async Task GetBeatmapLevelsAsync()
        {
            List<Task<Tuple<int, BeatmapDetails>>> taskList = new List<Task<Tuple<int, BeatmapDetails>>>(WorkChunkSize);
            _loadedLevelsUnsorted = new List<Tuple<int, BeatmapDetails>>(_levels.Length);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            int index = 0;
            while (index < _levels.Length)
            {
                for (int i = 0; i < WorkChunkSize && index < _levels.Length; ++index)
                {
                    IPreviewBeatmapLevel level = _levels[index];

                    if (level is IBeatmapLevel)
                    {
                        _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, new BeatmapDetails(level as IBeatmapLevel)));
                    }
                    else if (_cache.ContainsKey(level.levelID))
                    {
                        _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, _cache[level.levelID]));
                    }
                    else if (level is CustomPreviewBeatmapLevel)
                    {
                        if (Tweaks.SongDataCoreTweaks.GetBeatmapDetails(level.levelID, out var beatmapDetails))
                        {
                            // load the beatmap details manually if some data from BeatSaver is incomplete
                            if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                            {
                                Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. \n" +
                                    "Discarding and regenerating BeatmapDetails object from locally stored information instead.");
                                taskList.Add(GetCustomBeatmapDetailsAsync(level as CustomPreviewBeatmapLevel, index));
                                ++i;
                            }
                            else
                            {
                                _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, beatmapDetails));
                            }
                        }
                        else
                        {
                            taskList.Add(GetCustomBeatmapDetailsAsync(level as CustomPreviewBeatmapLevel, index));
                            ++i;
                        }
                    }
                    else
                    {
                        // add null details object if not convertable (possibly unbought DLC?)
                        _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, null));
                    }
                }

                while (taskList.Any())
                {
                    Task<Tuple<int, BeatmapDetails>> finished = await Task.WhenAny(taskList);
                    Tuple<int, BeatmapDetails> loadedBeatmap = await finished;

                    _loadedLevelsUnsorted.Add(loadedBeatmap);

                    if (_loadingTokenSource.IsCancellationRequested)
                        return;

                    taskList.Remove(finished);
                }
            }

            sw.Stop();
            Logger.log.Debug($"Finished loading the details of {_loadedLevelsUnsorted.Count} beatmaps (took {sw.ElapsedMilliseconds/1000f} seconds)");

            // all beatmaps are loaded, sort to maintain order
            _loadedLevelsUnsorted.Sort((x, y) => x.Item1 - y.Item1);

            _loadedLevels = _loadedLevelsUnsorted.Select((tuple) => tuple.Item2).ToList();
        }

        private async Task<CustomBeatmapLevel> LoadCustomBeatmapLevelAsync(CustomPreviewBeatmapLevel level, CancellationToken token)
        {
            CustomBeatmapLevel customLevel = new CustomBeatmapLevel(level, null, null);
            BeatmapLevelData beatmapData = await _customLevelLoader.LoadBeatmapLevelDataAsync(level.customLevelPath, customLevel, level.standardLevelInfoSaveData, token).ConfigureAwait(false);
            customLevel.SetBeatmapLevelData(beatmapData);

            return customLevel;
        }

        private async Task CacheCustomBeatmapDetailsAsync(CustomPreviewBeatmapLevel level)
        {
            CustomBeatmapLevel customLevel = await LoadCustomBeatmapLevelAsync(level, _cachingTokenSource.Token).ConfigureAwait(false);
            _cache[customLevel.levelID] = new BeatmapDetails(customLevel);
        }

        private async Task<Tuple<int, BeatmapDetails>> GetCustomBeatmapDetailsAsync(CustomPreviewBeatmapLevel level, int index)
        {
            try
            {
                CustomBeatmapLevel customLevel = await LoadCustomBeatmapLevelAsync(level, _loadingTokenSource.Token);

                var details = new BeatmapDetails(customLevel);
                _cache[customLevel.levelID] = details;

                return new Tuple<int, BeatmapDetails>(index, details);
            }
            catch (OperationCanceledException) { }

            return new Tuple<int, BeatmapDetails>(index, null);
        }
    }
}
