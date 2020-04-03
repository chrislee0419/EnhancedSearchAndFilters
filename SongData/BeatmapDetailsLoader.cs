using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using SongCore;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.Tweaks;

namespace EnhancedSearchAndFilters.SongData
{
    class BeatmapDetailsLoader : PersistentSingleton<BeatmapDetailsLoader>
    {
        public event Action CachingStarted;
        public event Action CachingFinished;

        public bool IsLoading { get; private set; } = false;
        public bool IsCaching { get; private set; } = false;
        public bool SongsAreCached { get; private set; } = false;

        private CustomLevelLoader _levelLoader;
        private CustomLevelLoader LevelLoader
        {
            get
            {
                if (_levelLoader == null)
                {
                    var beatmapLevelsModel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First().GetPrivateField<BeatmapLevelsModel>("_beatmapLevelsModel");
                    var customLevelLoader = beatmapLevelsModel.GetPrivateField<CustomLevelLoader>("_customLevelLoader");

                    _levelLoader = Instantiate(customLevelLoader, this.transform);
                }

                return _levelLoader;
            }
        }

        private CachedMediaAsyncLoader _mediaLoader;
        private CachedMediaAsyncLoader MediaLoader
        {
            get
            {
                if (_mediaLoader == null)
                {
                    var beatmapLevelsModel = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First().GetPrivateField<BeatmapLevelsModel>("_beatmapLevelsModel");
                    var customLevelLoader = beatmapLevelsModel.GetPrivateField<CustomLevelLoader>("_customLevelLoader");
                    var cachedMediaAsyncLoader = customLevelLoader.GetPrivateField<CachedMediaAsyncLoader>("_cachedMediaAsyncLoaderSO");

                    _mediaLoader = Instantiate(cachedMediaAsyncLoader, this.transform);
                    _mediaLoader.name = "ESAFMediaLoader";
                }

                return _mediaLoader;
            }
        }

        private HMTask _cachingTask;
        private ManualResetEvent _manualResetEvent;
        private CancellationTokenSource _cachingTokenSource;

        private HMTask _loadingTask;
        private CancellationTokenSource _loadingTokenSource;

        private IPreviewBeatmapLevel[] _levels;
        private List<BeatmapDetails> _loadedLevels;
        private List<Tuple<int, BeatmapDetails>> _loadedLevelsUnsorted = new List<Tuple<int, BeatmapDetails>>();

        // maintain our own cache because we don't need load the preview audio or cover image of each level and the game's HMCache is limited to 30 songs
        private ConcurrentDictionary<string, BeatmapDetails> _cache = new ConcurrentDictionary<string, BeatmapDetails>();

        // load the details in batches (there is a noticable delay with queueing all async load tasks at once)
        private const int WorkChunkSize = 10;

        // 5 second timeout for loading tasks
        private static readonly TimeSpan TimeoutDelay = new TimeSpan(0, 0, 5);

        private static string CachedBeatmapDetailsFilePath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedFilterDetailsCache.json");

        /// <summary>
        /// Should be run whenever in the menu, to start/resume populating the cache for filter/search.
        /// </summary>
        public void StartPopulatingCache(bool force = false)
        {
            if (IsLoading || (SongsAreCached && !force && !IsCaching) || PluginConfig.DisableFilters)
                return;

            if (!IsCaching)
            {
                IsCaching = true;
                SongsAreCached = false;
                _manualResetEvent = new ManualResetEvent(true);
                _cachingTokenSource = new CancellationTokenSource();

                _cachingTask = new HMTask(
                    delegate ()
                    {
                        try
                        {
                            Logger.log.Info("Starting to cache all custom song details");

                            CacheAllBeatmapLevelsAsync().GetAwaiter().GetResult();
                            SongsAreCached = true;
                            SaveCacheToFile();

                            Logger.log.Info("Finished caching and storing all custom song details");
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.log.Debug("Caching task cancelled");
                        }
                        catch (Exception e)
                        {
                            Logger.log.Warn($"Uncaught exception occurred in the caching thread");
                            Logger.log.Debug(e);
                        }
                    },
                    delegate ()
                    {
                        _manualResetEvent = null;
                        _cachingTask = null;

                        IsCaching = false;

                        if (!_cachingTokenSource.IsCancellationRequested)
                            CachingFinished?.Invoke();

                        _cachingTokenSource.Dispose();
                        _cachingTokenSource = null;
                    });

                _cachingTask.Run();
                CachingStarted?.Invoke();
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

            _cachingTask = null;
            IsCaching = false;
        }

        public async Task PopulateCacheFromFile()
        {
            var detailsList = await BeatmapDetailsCache.GetBeatmapDetailsFromCacheAsync(CachedBeatmapDetailsFilePath);

            if (detailsList.Count > 0)
                Logger.log.Info($"Retrieved {detailsList.Count} cached beatmap details from file");

            foreach (var detail in detailsList)
                _cache.TryAdd(detail.LevelID, detail);
        }

        /// <summary>
        /// Save the cached BeatmapDetails objects to a JSON file located at CachedBeatmapDetailsFilePath.
        /// </summary>
        public void SaveCacheToFile()
        {
            if (PluginConfig.DisableFilters)
                return;

            List<BeatmapDetails> cache = _cache.Values.ToList();

            // remove WIP levels from cache (don't save them, since they're likely to change often and
            // if we don't delete any entries in the cache, it may cause the cache to balloon in size)
            var wipLevels = Loader.CustomWIPLevels.Values.Select(x => GetCustomLevelIDWithoutDirectory(x.levelID));
            cache = cache.Where(c => !wipLevels.Any(wip => wip == c.LevelID)).ToList();

            // remove beatmaps that are not loaded
            var customLevels = Loader.CustomLevels.Values.Select(x => GetCustomLevelIDWithoutDirectory(x.levelID));
            cache = cache.Where(c => customLevels.Any(level => level == c.LevelID)).ToList();

            // repeat the above checks for user-added folders
            foreach (var folder in Loader.SeperateSongFolders)
            {
                var levels = folder.Levels.Values.Select(x => GetCustomLevelIDWithoutDirectory(x.levelID));

                if (folder.SongFolderEntry.WIP)
                    cache = cache.Where(c => !levels.Any(wip => wip == c.LevelID)).ToList();
                else
                    cache = cache.Where(c => levels.Any(level => level == c.LevelID)).ToList();
            }

            if (cache.Count > 0)
                BeatmapDetailsCache.SaveBeatmapDetailsToCache(CachedBeatmapDetailsFilePath, cache);
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
            _loadedLevels = null;

            StartCoroutine(UpdateCoroutine(update));

            if (_loadingTokenSource != null)
                _loadingTokenSource.Dispose();
            _loadingTokenSource = new CancellationTokenSource();

            _loadingTask = new HMTask(
                delegate ()
                {
                    Logger.log.Debug("Starting to load beatmap details");

                    try
                    {
                        GetBeatmapLevelsAsync().GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.log.Debug("Loading task cancelled");
                    }
                    catch (Exception e)
                    {
                        Logger.log.Warn("Uncaught exception in occurred in the loading thread");
                        Logger.log.Debug(e);
                    }
                },
                delegate ()
                {
                    _loadingTask = null;
                    IsLoading = false;

                    if (IsCaching || !SongsAreCached)
                        StartPopulatingCache();

                    if (!_loadingTokenSource.IsCancellationRequested && _loadedLevels != null)
                        onFinish?.Invoke(_loadedLevels.ToArray());

                    _loadingTokenSource.Dispose();
                    _loadingTokenSource = null;
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
            int notLoadedCount = 0;

            for (int i = 0; i < levels.Length; ++i)
            {
                IPreviewBeatmapLevel level = levels[i];
                string levelID = GetSimplifiedLevelID(level);

                if (level is IBeatmapLevel)
                {
                    detailsList[i] = new BeatmapDetails(level as IBeatmapLevel);
                }
                else if (_cache.ContainsKey(levelID))
                {
                    detailsList[i] = _cache[levelID];
                }
                else
                {
                    // unable to load from cache or convert directly to BeatmapDetails object
                    detailsList[i] = null;
                    ++notLoadedCount;
                }
            }

            if (notLoadedCount > 0)
                Logger.log.Warn($"LoadBeatmapsInstant was unable to retrieve all BeatmapDetails objects from cache ({notLoadedCount} could not be loaded)");

            return detailsList;
        }

        public void CancelLoading()
        {
            if (!IsLoading || _loadingTokenSource == null || _loadingTask == null)
                return;

            IsLoading = false;

            _loadingTokenSource.Cancel();
            _loadingTask.Cancel();
            _loadingTask = null;

            if (IsCaching || !SongsAreCached)
                StartPopulatingCache();
        }

        private IEnumerator UpdateCoroutine(Action<int> action)
        {
            if (action == null)
                yield break;

            while (IsLoading)
            {
                action.Invoke(_loadedLevelsUnsorted.Count);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private async Task CacheAllBeatmapLevelsAsync()
        {
            var sw = Stopwatch.StartNew();

            // load beatmap details from cache if it exists
            PopulateCacheFromFile().GetAwaiter().GetResult();

            // we don't have to cache OST levels, since they can immediately be cast into IBeatmapLevel objects
            List<IPreviewBeatmapLevel> allLevels = Loader.CustomLevelsCollection.beatmapLevels.ToList();
            foreach (var folder in Loader.SeperateSongFolders)
            {
                if (!folder.SongFolderEntry.WIP)
                    allLevels.AddRange(folder.Levels.Values);
            }

            // record errors from SongDataCore for logging
            List<SongDataCoreDataStatus> sdcErrorStatuses = new List<SongDataCoreDataStatus>(allLevels.Count);

            List<Task<bool>> taskList = new List<Task<bool>>(WorkChunkSize);
            int index = 0;
            int errorCount = 0;
            long elapsed = 0;
            while (index < allLevels.Count)
            {
                _manualResetEvent.WaitOne();

                if (sw.ElapsedMilliseconds > 60000 + elapsed)
                {
                    Logger.log.Debug($"Caching thread has finished caching {index} beatmaps out of {allLevels.Count} ({elapsed} ms elapsed)");
                    elapsed += sw.ElapsedMilliseconds;
                }

                if (_cachingTokenSource.IsCancellationRequested)
                    _cachingTokenSource.Token.ThrowIfCancellationRequested();

                for (int i = 0; i < WorkChunkSize && index < allLevels.Count; ++index)
                {
                    string levelID = GetSimplifiedLevelID(allLevels[index]);

                    if (!_cache.ContainsKey(levelID) || _cache[levelID].SongDuration == 0f)
                    {
                        SongDataCoreDataStatus status = SongDataCoreTweaks.GetBeatmapDetails(allLevels[index] as CustomPreviewBeatmapLevel, out var beatmapDetails);

                        if (status == SongDataCoreDataStatus.Success)
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
                            if (SongDataCoreTweaks.IsModAvailable)
                                sdcErrorStatuses.Add(status);

                            taskList.Add(CacheCustomBeatmapDetailsAsync(allLevels[index] as CustomPreviewBeatmapLevel));
                            ++i;
                        }
                    }
                }

                while (taskList.Any())
                {
                    Task<bool> cacheTask = await Task.WhenAny(taskList).ConfigureAwait(false);
                    bool wasSuccessful = await cacheTask;

                    if (!wasSuccessful)
                        ++errorCount;

                    taskList.Remove(cacheTask);
                }
            }

            sw.Stop();
            Logger.log.Info($"Finished caching the details of {allLevels.Count} beatmaps (took {sw.ElapsedMilliseconds/1000f} seconds).");

            if (errorCount > 0)
                Logger.log.Warn($"Unable to cache the beatmap details for {errorCount} songs");

            if (sdcErrorStatuses.Count > 0)
            {
                // NOTE: this will need to be updated if i ever add more error status markers
                Logger.log.Debug($"Unable to retrieve some data from SongDataCore: (" +
                    $"NoData = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.NoData)}, " +
                    $"InvalidBPM = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidBPM)}, " +
                    $"InvalidDuration = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidDuration)}, " +
                    $"InvalidCharacteristicString = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidCharacteristicString)}, " +
                    $"InvalidDifficultyString = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidDifficultyString)}, " +
                    $"ExceptionThrown = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.ExceptionThrown)})");
            }
        }

        private async Task GetBeatmapLevelsAsync()
        {
            List<Task<Tuple<int, BeatmapDetails>>> taskList = new List<Task<Tuple<int, BeatmapDetails>>>(WorkChunkSize);
            _loadedLevelsUnsorted = new List<Tuple<int, BeatmapDetails>>(_levels.Length);

            // record errors from SongDataCore for logging
            List<SongDataCoreDataStatus> sdcErrorStatuses = new List<SongDataCoreDataStatus>(_levels.Length);

            var sw = Stopwatch.StartNew();

            int index = 0;
            while (index < _levels.Length)
            {
                for (int i = 0; i < WorkChunkSize && index < _levels.Length; ++index)
                {
                    IPreviewBeatmapLevel level = _levels[index];
                    string levelID = GetSimplifiedLevelID(level);

                    if (level is IBeatmapLevel beatmapLevel)
                    {
                        _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, new BeatmapDetails(beatmapLevel)));
                    }
                    else if (_cache.ContainsKey(levelID))
                    {
                        _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, _cache[levelID]));
                    }
                    else if (level is CustomPreviewBeatmapLevel customLevel)
                    {
                        SongDataCoreDataStatus status = SongDataCoreTweaks.GetBeatmapDetails(customLevel, out var beatmapDetails);
                        if (status == SongDataCoreDataStatus.Success)
                        {
                            // load the beatmap details manually if some data from BeatSaver is incomplete
                            if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                            {
                                Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. \n" +
                                    "Discarding and regenerating BeatmapDetails object from locally stored information instead.");
                                taskList.Add(GetCustomBeatmapDetailsAsync(customLevel, index));
                                ++i;
                            }
                            else
                            {
                                _loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, beatmapDetails));
                            }
                        }
                        else
                        {
                            if (SongDataCoreTweaks.IsModAvailable)
                                sdcErrorStatuses.Add(status);

                            taskList.Add(GetCustomBeatmapDetailsAsync(customLevel, index));
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
                    Task<Tuple<int, BeatmapDetails>> finished = await Task.WhenAny(taskList).ConfigureAwait(false);
                    Tuple<int, BeatmapDetails> loadedBeatmap = await finished;

                    _loadedLevelsUnsorted.Add(loadedBeatmap);

                    if (_loadingTokenSource.IsCancellationRequested)
                        return;

                    taskList.Remove(finished);
                }
            }

            sw.Stop();
            Logger.log.Debug($"Finished loading the details of {_loadedLevelsUnsorted.Count} beatmaps (took {sw.ElapsedMilliseconds/1000f} seconds)");

            int notLoadedCount = _loadedLevelsUnsorted.Count(x => x.Item2 == null);
            if (notLoadedCount > 0)
                Logger.log.Warn($"Unable to load the beatmap details for {notLoadedCount} songs");

            if (sdcErrorStatuses.Count > 0)
            {
                // NOTE: this will need to be updated if i ever add more error status markers
                Logger.log.Debug($"Unable to retrieve some data from SongDataCore: (" +
                    $"NoData = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.NoData)}, " +
                    $"InvalidBPM = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidBPM)}, " +
                    $"InvalidDuration = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidDuration)}, " +
                    $"InvalidCharacteristicString = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidCharacteristicString)}, " +
                    $"InvalidDifficultyString = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.InvalidDifficultyString)}, " +
                    $"ExceptionThrown = {sdcErrorStatuses.Count(x => x == SongDataCoreDataStatus.ExceptionThrown)})");
            }

            // all beatmaps are loaded, sort to maintain order
            _loadedLevelsUnsorted.Sort((x, y) => x.Item1 - y.Item1);

            _loadedLevels = _loadedLevelsUnsorted.Select((tuple) => tuple.Item2).ToList();
        }

        /// <summary>
        /// Load a CustomBeatmapLevel from a CustomPreviewBeatmapLevel asynchronously. This method should not be used in a separate thread.
        /// </summary>
        /// <param name="level">CustomPreviewBeatmapLevel to load</param>
        /// <param name="token">Token for cancelling this task</param>
        /// <returns>A task that gets the associated CustomBeatmapLevel.</returns>
        private async Task<CustomBeatmapLevel> LoadCustomBeatmapLevelAsync(CustomPreviewBeatmapLevel level, CancellationToken token)
        {
            CancellationTokenSource linkedTokenSource = CreateLinkedCancellationTokenSource(token, out var timedTokenSource, TimeoutDelay);
            CustomBeatmapLevel customLevel = new CustomBeatmapLevel(CreateLevelCopyWithReplacedMediaLoader(level, MediaLoader), null, null);

            try
            {
                BeatmapLevelData beatmapData = await LevelLoader.LoadBeatmapLevelDataAsync(level.customLevelPath, customLevel, level.standardLevelInfoSaveData, linkedTokenSource.Token);
                if (beatmapData != null)
                {
                    customLevel.SetBeatmapLevelData(beatmapData);
                    return customLevel;
                }
                else
                {
                    Logger.log.Debug($"Unable to load beatmap level data for '{level.songName}' (no data returned)");
                }
            }
            catch (OperationCanceledException)
            {
                if (timedTokenSource.IsCancellationRequested && !token.IsCancellationRequested)
                    Logger.log.Debug($"Unable to load beatmap level data for '{level.songName}' (load task timed out)");
            }

            return null;
        }

        private async Task<bool> CacheCustomBeatmapDetailsAsync(CustomPreviewBeatmapLevel level)
        {
            CancellationTokenSource linkedTokenSource = CreateLinkedCancellationTokenSource(_cachingTokenSource.Token, out var timedTokenSource, TimeoutDelay);

            try
            {
                BeatmapDetails beatmapDetails = await BeatmapDetails.CreateBeatmapDetailsFromFilesAsync(level, linkedTokenSource.Token);
                if (beatmapDetails != null)
                {
                    _cache[GetSimplifiedLevelID(level)] = beatmapDetails;
                    return true;
                }
                else
                {
                    Logger.log.Debug($"Unable to load beatmap details for '{level.songName}' (invalid data)");
                }
            }
            catch (OperationCanceledException)
            {
                if (timedTokenSource.IsCancellationRequested && !_cachingTokenSource.IsCancellationRequested)
                    Logger.log.Debug($"Unable to load beatmap details for '{level.songName}' (load task timed out)");
                else
                    throw;
            }
            catch (Exception e)
            {
                Logger.log.Debug($"Exception encountered while trying to cache '{level.songName}'");
                Logger.log.Debug(e);
            }

            return false;
        }

        private async Task<Tuple<int, BeatmapDetails>> GetCustomBeatmapDetailsAsync(CustomPreviewBeatmapLevel level, int index)
        {
            CancellationTokenSource linkedTokenSource = CreateLinkedCancellationTokenSource(_loadingTokenSource.Token, out var timedTokenSource, TimeoutDelay);

            try
            {
                BeatmapDetails beatmapDetails = await BeatmapDetails.CreateBeatmapDetailsFromFilesAsync(level, linkedTokenSource.Token);

                if (beatmapDetails != null)
                {
                    _cache[GetSimplifiedLevelID(level)] = beatmapDetails;
                    return new Tuple<int, BeatmapDetails>(index, beatmapDetails);
                }
            }
            catch (OperationCanceledException)
            {
                if (timedTokenSource.IsCancellationRequested && !_loadingTokenSource.IsCancellationRequested)
                    Logger.log.Debug($"Unable to load beatmap details for '{level.songName}' (load task timed out)");
                else
                    throw;
            }
            catch (Exception e)
            {
                Logger.log.Debug($"Exception encountered while trying to load '{level.songName}'");
                Logger.log.Debug(e);
            }

            return new Tuple<int, BeatmapDetails>(index, null);
        }

        private CustomPreviewBeatmapLevel CreateLevelCopyWithReplacedMediaLoader(CustomPreviewBeatmapLevel level, CachedMediaAsyncLoader mediaLoader)
        {
            // recreate the CustomPreviewBeatmapLevel, but replace the original CachedMediaAsyncLoader with our own copy
            // this is necessary since game version 1.6.0, otherwise custom level cover image loading breaks in the LevelCollectionViewController
            // the problem seems to occur because this mod loads/caches information on another separate thread, and there is some
            // kind of incompatibility with the original supplied CachedMediaAsyncLoader with multithreading
            return new CustomPreviewBeatmapLevel(level.defaultCoverImageTexture2D,
                level.standardLevelInfoSaveData, level.customLevelPath,
                mediaLoader, mediaLoader,
                level.levelID, level.songName, level.songSubName, level.songAuthorName, level.levelAuthorName,
                level.beatsPerMinute, level.songTimeOffset, level.shuffle, level.shufflePeriod, level.previewStartTime,
                level.previewDuration, level.environmentInfo, level.allDirectionsEnvironmentInfo, level.previewDifficultyBeatmapSets);
        }

        /// <summary>
        /// Get the level ID of an IPreviewBeatmapLevel. Removes the directory from a custom level's ID if it exists.
        /// </summary>
        /// <param name="level">A preview beatmap level.</param>
        /// <returns>The level ID of the provided IPreviewBeatmapLevel.</returns>
        public static string GetSimplifiedLevelID(IPreviewBeatmapLevel level)
        {
            // Since custom levels have their IDs formatted like "custom_level_(hash)[_(directory)]", where the "_(directory)" part is optional,
            // we have to remove that part to get a consistent naming. Also, we don't care about duplicate songs;
            // if they have the same hash, we can use the same BeatmapDetails object.
            if (!(level is CustomPreviewBeatmapLevel) && !level.levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
                return level.levelID;
            else
                return GetCustomLevelIDWithoutDirectory(level.levelID);
        }

        /// <summary>
        /// Get the simplified level ID from a level ID string. Removes the directory from a custom level's ID if it exists.
        /// </summary>
        /// <param name="levelID">A string containing some level's ID.</param>
        /// <returns>The level ID, minus the directory when applicable.</returns>
        public static string GetSimplifiedLevelID(string levelID)
        {
            if (levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
                return GetCustomLevelIDWithoutDirectory(levelID);
            else
                return levelID;
        }

        private static string GetCustomLevelIDWithoutDirectory(string levelID)
        {
            // The hash is always 40 characters long
            return levelID.Substring(0, CustomLevelLoader.kCustomLevelPrefixId.Length + 40);
        }

        private static CancellationTokenSource CreateLinkedCancellationTokenSource(CancellationToken origToken, out CancellationTokenSource newTokenSource, TimeSpan? delay = null)
        {
            if (delay.HasValue)
                newTokenSource = new CancellationTokenSource(delay.Value);
            else
                newTokenSource = new CancellationTokenSource();

            return CancellationTokenSource.CreateLinkedTokenSource(origToken, newTokenSource.Token);
        }
    }
}
