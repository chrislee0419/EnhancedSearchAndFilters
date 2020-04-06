using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using EnhancedSearchAndFilters.Tweaks;

namespace EnhancedSearchAndFilters.SongData
{
    public partial class BeatmapDetailsLoader
    {
        #region Caching Beatmap Details
        private class CoroutineCacher : MonoBehaviour, ICacher
        {
            public event Action CachingStarted;
            public event Action CachingFinished;

            public bool IsCaching => _cachingCoroutine != null;

            private IEnumerator _cachingCoroutine = null;
            private bool _cachingPaused = false;

            public void Dispose()
            {
                foreach (Delegate d in CachingStarted.GetInvocationList())
                    CachingStarted -= (Action)d;
                foreach (Delegate d in CachingFinished.GetInvocationList())
                    CachingFinished -= (Action)d;

                if (IsCaching)
                    StopCaching();

                Destroy(this.gameObject);
            }

            public void StartCaching()
            {
                _cachingPaused = false;

                if (!IsCaching)
                {
                    _cachingCoroutine = CacheAllBeatmapDetailsCoroutine();
                    StartCoroutine(_cachingCoroutine);
                }
                else
                {
                    // resume old populate cache job
                    Logger.log.Info("Resuming beatmap details caching thread");
                }
            }

            public void PauseCaching()
            {
                _cachingPaused = true;
            }

            public void StopCaching()
            {
                StopCoroutine(_cachingCoroutine);
                _cachingCoroutine = null;
                _cachingPaused = false;
            }

            private IEnumerator PopulateCacheFromFileCoroutine()
            {
                var coroutine = BeatmapDetailsCache.GetBeatmapDetailsFromCacheCoroutine(BeatmapDetailsLoader.CachedBeatmapDetailsFilePath);

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
                        if (!BeatmapDetailsLoader._cache.ContainsKey(detail.LevelID))
                            BeatmapDetailsLoader._cache.Add(detail.LevelID, detail);
                    }
                }
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
                List<IPreviewBeatmapLevel> allCustomLevels = BeatmapDetailsLoader.GetAllCustomLevels();

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
                        elapsed = sw.ElapsedMilliseconds;
                        Logger.log.Debug($"Caching coroutine has finished caching {index} beatmaps out of {allCustomLevels.Count} ({elapsed} ms elapsed)");
                    }

                    while (_cachingPaused)
                        yield return null;

                    int startingIndex = index;
                    for (int i = 0; i < WorkChunkSize && index < allCustomLevels.Count && index - startingIndex < WorkQueryChunkSize; ++index)
                    {
                        string levelID = GetSimplifiedLevelID(allCustomLevels[index]);

                        if (BeatmapDetailsLoader._cache.ContainsKey(levelID) && BeatmapDetailsLoader._cache[levelID].SongDuration > 0.01f)
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
                                BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
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
                                    BeatmapDetailsLoader._cache[beatmapDetails.LevelID] = beatmapDetails;
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

                // check for pause before writing to disk
                while (_cachingPaused)
                    yield return null;

                sw.Stop();
                Logger.log.Info($"Finished caching the details of {allCustomLevels.Count} beatmaps (took {sw.ElapsedMilliseconds / 1000f} seconds)");

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

                BeatmapDetailsLoader.instance.SaveCacheToFile();

                _cachingCoroutine = null;
                CachingFinished?.Invoke();
            }
        }
        #endregion // Caching Beatmap Details

        #region Loading Beatmap Details
        private class CoroutineLoader : MonoBehaviour, ILoader
        {
            public bool IsLoading { get => _loadingCoroutine != null; }

            private IEnumerator _loadingCoroutine = null;

            public void Dispose()
            {
                if (IsLoading)
                    StopLoading();

                Destroy(this.gameObject);
            }

            public void StartLoading(IPreviewBeatmapLevel[] levels, Action<int> updateCallback, Action<BeatmapDetails[]> onFinish)
            {
                _loadingCoroutine = LoadBeatmapsCoroutine(levels, updateCallback, onFinish);
                StartCoroutine(_loadingCoroutine);
            }

            public void StopLoading()
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
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
                    if (updateCallback != null && elapsed - sw.ElapsedMilliseconds > 1000)
                    {
                        updateCallback.Invoke(loadedLevelsUnsorted.Count);
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
                        else if (BeatmapDetailsLoader._cache.ContainsKey(levelID))
                        {
                            loadedLevelsUnsorted.Add(new Tuple<int, BeatmapDetails>(index, BeatmapDetailsLoader._cache[levelID]));
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
                                    BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
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
}
