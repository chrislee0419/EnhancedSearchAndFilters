using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SongCore;
using EnhancedSearchAndFilters.Tweaks;

namespace EnhancedSearchAndFilters.SongData
{
    #region Caching Beatmap Details
    public partial class BeatmapDetailsLoader
    {
        public event Action CachingStarted;
        public event Action CachingFinished;

        public bool IsCaching { get => _cachingCoroutine != null; }
        public bool SongsAreCached { get; private set; } = false;

        private IEnumerator _cachingCoroutine = null;
        private bool _cachingPaused = false;

        /// <summary>
        /// Should be run whenever in the menu, to start/resume populating the cache for filter/search.
        /// </summary>
        public void StartPopulatingCache(bool force = false)
        {
            if (IsLoading || (SongsAreCached && !force && !IsCaching) || PluginConfig.DisableFilters)
                return;

            _cachingPaused = false;

            if (!IsCaching)
            {
                SongsAreCached = false;

                _cachingCoroutine = CacheAllBeatmapDetailsCoroutine();
                StartCoroutine(_cachingCoroutine);
            }
            else
            {
                // resume old populate cache job
                Logger.log.Info("Resuming beatmap details caching thread");
            }
        }

        /// <summary>
        /// Blocks the thread that is populating the cache. Can be resumed with StartPopulatingCache().
        /// </summary>
        public void PausePopulatingCache()
        {
            if (!IsCaching)
                return;

            _cachingPaused = true;
        }

        /// <summary>
        /// Only used to stop the caching thread during OnApplicationQuit().
        /// </summary>
        public void CancelPopulatingCache()
        {
            if (!IsCaching)
                return;

            StopCoroutine(_cachingCoroutine);
            _cachingCoroutine = null;
            _cachingPaused = false;
        }

        private IEnumerator PopulateCacheFromFileCoroutine()
        {
            var coroutine = BeatmapDetailsCache.GetBeatmapDetailsFromCacheCoroutine(CachedBeatmapDetailsFilePath);

            List<BeatmapDetails> loadedCache = null;
            while (coroutine.MoveNext())
            {
                loadedCache = coroutine.Current;

                if (loadedCache == null)
                    yield return null;
            }

            if (loadedCache != null)
            {
                if (loadedCache.Count > 0)
                    Logger.log.Info($"Retrieved {loadedCache.Count} cached beatmap details from file");

                foreach (var detail in loadedCache)
                {
                    if (!_cache.ContainsKey(detail.LevelID))
                        _cache.Add(detail.LevelID, detail);
                }
            }
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

        private IEnumerator CacheAllBeatmapDetailsCoroutine()
        {
            CachingStarted?.Invoke();

            var sw = Stopwatch.StartNew();

            // load beatmap details from cache if it exists
            var loadCache = PopulateCacheFromFileCoroutine();
            while (loadCache.MoveNext())
                yield return loadCache.Current;

            // we don't have to cache OST levels, since they can be immediately cast into IBeatmapLevel objects
            List<IPreviewBeatmapLevel> allCustomLevels = Loader.CustomLevelsCollection.beatmapLevels.ToList();
            foreach (var folder in Loader.SeperateSongFolders)
            {
                if (!folder.SongFolderEntry.WIP)
                    allCustomLevels.AddRange(folder.Levels.Values);
            }

            // record errors from SongDataCore for logging
            List<SongDataCoreDataStatus> sdcErrorStatusList = new List<SongDataCoreDataStatus>(allCustomLevels.Count);

            List<IEnumerator<BeatmapDetails>> taskList = new List<IEnumerator<BeatmapDetails>>(WorkChunkSize);
            int index = 0;
            int errorCount = 0;
            long elapsed = 0;
            while (index < allCustomLevels.Count)
            {
                if (sw.ElapsedMilliseconds > 30000 + elapsed)
                {
                    Logger.log.Debug($"Caching coroutine has finished caching {index} beatmaps out of {allCustomLevels.Count} ({elapsed} ms elapsed)");
                    elapsed += sw.ElapsedMilliseconds;
                }

                while (_cachingPaused)
                    yield return null;

                int startingIndex = index;
                for (int i = 0; i < WorkChunkSize && index < allCustomLevels.Count && index - startingIndex < WorkQueryChunkSize; ++index)
                {
                    string levelID = GetSimplifiedLevelID(allCustomLevels[index]);

                    if (_cache.ContainsKey(levelID) && _cache[levelID].SongDuration < 0.001f)
                        continue;

                    SongDataCoreDataStatus status = SongDataCoreTweaks.GetBeatmapDetails(allCustomLevels[index] as CustomPreviewBeatmapLevel, out var beatmapDetails);

                    if (status == SongDataCoreDataStatus.Success)
                    {
                        // load the beatmap details manually if some data from BeatSaver is incomplete
                        if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                        {
                            Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. " +
                                "Discarding and generating BeatmapDetails object from locally stored information instead");
                            taskList.Add(BeatmapDetails.CreateBeatmapDetailsFromFilesCoroutine(allCustomLevels[index] as CustomPreviewBeatmapLevel));
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
                            sdcErrorStatusList.Add(status);

                        taskList.Add(BeatmapDetails.CreateBeatmapDetailsFromFilesCoroutine(allCustomLevels[index] as CustomPreviewBeatmapLevel));
                        ++i;
                    }
                }

                if (taskList.Any())
                    yield return null;

                while (taskList.Any())
                {
                    while (_cachingPaused)
                        yield return null;

                    for (int i = 0; i < taskList.Count; ++i)
                    {
                        IEnumerator<BeatmapDetails> loadCoroutine = taskList[i];

                        if (loadCoroutine.MoveNext())
                        {
                            BeatmapDetails beatmapDetails = loadCoroutine.Current;
                            if (beatmapDetails != null)
                            {
                                _cache[beatmapDetails.LevelID] = beatmapDetails;
                                taskList.Remove(loadCoroutine);
                                --i;
                            }
                        }
                        else
                        {
                            ++errorCount;
                            taskList.Remove(loadCoroutine);
                            --i;
                        }
                    }

                    if (taskList.Any())
                        yield return null;
                }

                if (index < allCustomLevels.Count)
                    yield return null;
            }

            sw.Stop();
            Logger.log.Info($"Finished caching the details of {allCustomLevels.Count} beatmaps (took {sw.ElapsedMilliseconds / 1000f} seconds).");

            if (errorCount > 0)
                Logger.log.Warn($"Unable to cache the beatmap details for {errorCount} songs");

            if (sdcErrorStatusList.Count > 0)
            {
                // NOTE: this will need to be updated if i ever add more error status markers
                Logger.log.Debug($"Unable to retrieve some data from SongDataCore: (" +
                    $"NoData = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.NoData)}, " +
                    $"InvalidBPM = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidBPM)}, " +
                    $"InvalidDuration = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidDuration)}, " +
                    $"InvalidCharacteristicString = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidCharacteristicString)}, " +
                    $"InvalidDifficultyString = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidDifficultyString)}, " +
                    $"ExceptionThrown = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.ExceptionThrown)})");
            }

            SongsAreCached = true;

            // check for pause before writing to disk
            while (_cachingPaused)
                yield return null;

            SaveCacheToFile();

            _cachingCoroutine = null;
            CachingFinished?.Invoke();
        }
    }
    #endregion // Caching Beatmap Details

    #region Loading Beatmap Details
    public partial class BeatmapDetailsLoader
    {
        public bool IsLoading { get => _loadingCoroutine != null; }

        private IEnumerator _loadingCoroutine = null;

        /// <summary>
        /// Loads the beatmap details of a list of IPreviewBeatmapLevels.
        /// </summary>
        /// <param name="levels">A list of IPreviewBeatmaps.</param>
        /// <param name="updateCallback">A function that will run every 0.1s that gets the number of beatmaps currently loaded.</param>
        /// <param name="onFinish">The function that is called when the details of all beatmaps are retrieved.</param>
        public void LoadBeatmaps(IPreviewBeatmapLevel[] levels, Action<int> updateCallback = null, Action<BeatmapDetails[]> onFinish = null)
        {
            if (onFinish == null)
                return;

            if (IsLoading)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
            }

            if (IsCaching)
                PausePopulatingCache();

            _loadingCoroutine = LoadBeatmapsCoroutine(levels, updateCallback, onFinish);
            StartCoroutine(_loadingCoroutine);
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
            if (!IsLoading)
                return;

            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;

            if (IsCaching || !SongsAreCached)
                StartPopulatingCache();
        }

        private IEnumerator LoadBeatmapsCoroutine(IPreviewBeatmapLevel[] levels, Action<int> updateCallback, Action<BeatmapDetails[]> onFinish)
        {
            List<IEnumerator<Tuple<int, BeatmapDetails>>> taskList = new List<IEnumerator<Tuple<int, BeatmapDetails>>>(WorkChunkSize);
            List<Tuple<int, BeatmapDetails>> loadedLevelsUnsorted = new List<Tuple<int, BeatmapDetails>>(levels.Length);

            // reocrd errors from SongDataCore for logging
            List<SongDataCoreDataStatus> sdcErrorStatusList = new List<SongDataCoreDataStatus>(levels.Length);

            var sw = Stopwatch.StartNew();

            int index = 0;
            long elapsed = 0;
            while (index < levels.Length)
            {
                if (elapsed - sw.ElapsedMilliseconds > 1000)
                {
                    updateCallback?.Invoke(loadedLevelsUnsorted.Count);
                    elapsed = sw.ElapsedMilliseconds;
                }

                int startingIndex = index;
                for (int i = 0; i < WorkChunkSize && index < levels.Length && index - startingIndex < WorkQueryChunkSize; ++index)
                {
                    IPreviewBeatmapLevel level = levels[index];
                    string levelID = GetSimplifiedLevelID(level);

                    if (level is IBeatmapLevel beatmapLevel)
                    {
                        loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, new BeatmapDetails(beatmapLevel)));
                    }
                    else if (_cache.ContainsKey(levelID))
                    {
                        loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, _cache[levelID]));
                    }
                    else if (level is CustomPreviewBeatmapLevel customLevel)
                    {
                        SongDataCoreDataStatus status = SongDataCoreTweaks.GetBeatmapDetails(customLevel, out var beatmapDetails);
                        if (status == SongDataCoreDataStatus.Success)
                        {
                            // load the beatmap details manually if some data from BeatSaver is incomplete
                            if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                            {
                                Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. " +
                                    "Discarding and generating BeatmapDetails object from locally stored information instead");
                                taskList.Add(GetCustomBeatmapDetailsCoroutine(customLevel, index));
                                ++i;
                            }
                            else
                            {
                                loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, beatmapDetails));
                            }
                        }
                        else
                        {
                            if (SongDataCoreTweaks.IsModAvailable)
                                sdcErrorStatusList.Add(status);

                            taskList.Add(GetCustomBeatmapDetailsCoroutine(customLevel, index));
                            ++i;
                        }
                    }
                    else
                    {
                        // add null details object if not convertable (possibly unbought DLC?)
                        loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, null));
                    }
                }

                if (taskList.Any())
                    yield return null;

                while (taskList.Any())
                {
                    for (int i = 0; i < taskList.Count; ++i)
                    {
                        IEnumerator<Tuple<int, BeatmapDetails>> loadCoroutine = taskList[i];

                        if (loadCoroutine.MoveNext())
                        {
                            Tuple<int, BeatmapDetails> tuple = loadCoroutine.Current;
                            if (tuple != null)
                            {
                                loadedLevelsUnsorted.Add(tuple);
                                taskList.Remove(loadCoroutine);
                                --i;
                            }
                        }
                        else
                        {
                            taskList.Remove(loadCoroutine);
                            --i;
                        }
                    }

                    if (taskList.Any())
                        yield return null;
                }

                if (index < levels.Length)
                    yield return null;
            }

            sw.Stop();
            Logger.log.Debug($"Finished loading the details of {loadedLevelsUnsorted.Count} beatmaps (took {sw.ElapsedMilliseconds / 1000f} seconds)");

            int notLoadedCount = loadedLevelsUnsorted.Count(x => x.Item2 == null);
            if (notLoadedCount > 0)
                Logger.log.Warn($"Unable to load the beatmap details for {notLoadedCount} songs");

            if (sdcErrorStatusList.Count > 0)
            {
                // NOTE: this will need to be updated if i ever add more error status markers
                Logger.log.Debug($"Unable to retrieve some data from SongDataCore: (" +
                    $"NoData = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.NoData)}, " +
                    $"InvalidBPM = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidBPM)}, " +
                    $"InvalidDuration = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidDuration)}, " +
                    $"InvalidCharacteristicString = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidCharacteristicString)}, " +
                    $"InvalidDifficultyString = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.InvalidDifficultyString)}, " +
                    $"ExceptionThrown = {sdcErrorStatusList.Count(x => x == SongDataCoreDataStatus.ExceptionThrown)})");
            }

            // all beatmaps are loaded, sort to maintain order
            loadedLevelsUnsorted.Sort((x, y) => x.Item1 - y.Item1);

            _loadingCoroutine = null;

            onFinish.Invoke(loadedLevelsUnsorted.Select((tuple) => tuple.Item2).ToArray());
        }

        private IEnumerator<Tuple<int, BeatmapDetails>> GetCustomBeatmapDetailsCoroutine(CustomPreviewBeatmapLevel level, int index)
        {
            IEnumerator<BeatmapDetails> loadingCoroutine = BeatmapDetails.CreateBeatmapDetailsFromFilesCoroutine(level);

            while (loadingCoroutine.MoveNext())
            {
                BeatmapDetails beatmapDetails = loadingCoroutine.Current;
                if (beatmapDetails != null)
                {
                    _cache[beatmapDetails.LevelID] = beatmapDetails;
                    yield return new Tuple<int, BeatmapDetails>(index, beatmapDetails);
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }

            yield return new Tuple<int, BeatmapDetails>(index, null);
        }
    }
    #endregion // Loading Beatmap Details
}
