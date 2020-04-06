using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using EnhancedSearchAndFilters.Tweaks;

namespace EnhancedSearchAndFilters.SongData
{
    public partial class BeatmapDetailsLoader
    {
        #region Caching Beatmap Details
        private class ThreadedCacher : ICacher
        {
            public event Action CachingStarted;
            public event Action CachingFinished;

            public bool IsCaching => _thread != null;

            private Thread _thread;
            private ManualResetEvent _manualResetEvent;
            private bool _isOperationCancelled;

            public void Dispose()
            {
                if (IsCaching)
                    StopCaching();
            }

            public void StartCaching()
            {
                if (!IsCaching)
                {
                    _manualResetEvent = new ManualResetEvent(true);
                    _thread = new Thread(CachingThread);
                    _isOperationCancelled = false;

                    _thread.Start();
                    CachingStarted?.Invoke();
                }
                else
                {
                    _manualResetEvent.Set();
                }
            }

            public void PauseCaching()
            {
                _manualResetEvent.Reset();
            }

            public void StopCaching()
            {
                _isOperationCancelled = true;
                _manualResetEvent.Set();
                _thread.Join();
                _thread = null;

                Logger.log.Debug("Threaded caching operation successfully cancelled");
            }

            private void CachingThread()
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    // load cache from file
                    List<BeatmapDetails> loadedCache = BeatmapDetailsCache.GetBeatmapDetailsFromCache(BeatmapDetailsLoader.CachedBeatmapDetailsFilePath);
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

                    List<IPreviewBeatmapLevel> allCustomLevels = BeatmapDetailsLoader.GetAllCustomLevels();
                    List<SongDataCoreDataStatus> sdcErrorStatusList = new List<SongDataCoreDataStatus>(allCustomLevels.Count);
                    int errorCount = 0;
                    long elapsed = 0;
                    for (int index = 0; index < allCustomLevels.Count; ++index)
                    {
                        if (sw.ElapsedMilliseconds > 30000 + elapsed)
                        {
                            elapsed = sw.ElapsedMilliseconds;
                            Logger.log.Debug($"Caching thread has finished caching {index} beatmaps out of {allCustomLevels.Count} ({elapsed} ms elapsed)");
                        }

                        _manualResetEvent.WaitOne();
                        if (_isOperationCancelled)
                            return;

                        IPreviewBeatmapLevel level = allCustomLevels[index];
                        string levelID = GetSimplifiedLevelID(level);
                        BeatmapDetails beatmapDetails;

                        if (BeatmapDetailsLoader._cache.ContainsKey(levelID) && BeatmapDetailsLoader._cache[levelID].SongDuration > 0.01f)
                            continue;

                        SongDataCoreDataStatus status = SongDataCoreTweaks.GetBeatmapDetails(level as CustomPreviewBeatmapLevel, out beatmapDetails);

                        if (status == SongDataCoreDataStatus.Success)
                        {
                            if (beatmapDetails.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diff.NoteJumpMovementSpeed == 0)))
                            {
                                Logger.log.Debug($"BeatmapDetails object generated for '{beatmapDetails.SongName}' from BeatSaver data has some incomplete fields. " +
                                    "Discarding and generating BeatmapDetails object from locally stored information instead");
                            }
                            else
                            {
                                BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
                                continue;
                            }
                        }
                        else if (SongDataCoreTweaks.IsModAvailable)
                        {
                            sdcErrorStatusList.Add(status);
                        }

                        _manualResetEvent.WaitOne();
                        if (_isOperationCancelled)
                            return;

                        beatmapDetails = BeatmapDetails.CreateBeatmapDetailsFromFiles(level as CustomPreviewBeatmapLevel);
                        if (beatmapDetails != null)
                            BeatmapDetailsLoader._cache[beatmapDetails.LevelID] = beatmapDetails;
                        else
                            ++errorCount;
                    }

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

                    _thread = null;
                    HMMainThreadDispatcher.instance.Enqueue(() => CachingFinished?.Invoke());
                }
                catch (Exception e)
                {
                    Logger.log.Warn("Unexpected exception occurred in caching thread");
                    Logger.log.Debug(e);
                }
            }
        }
        #endregion

        #region Loading Beatmap Details
        private class ThreadedLoader : MonoBehaviour, ILoader
        {
            public bool IsLoading => _thread != null;

            private Thread _thread;
            private bool _isOperationCancelled = false;
            private IEnumerator _updateCoroutine;
            private int _levelsLoadedCount = 0;

            private static readonly WaitForSeconds _updateDelay = new WaitForSeconds(0.1f);

            public void Dispose()
            {
                if (IsLoading)
                    StopLoading();
            }

            public void StartLoading(IPreviewBeatmapLevel[] levels, Action<int> updateCallback, Action<BeatmapDetails[]> onFinish)
            {
                if (updateCallback != null)
                {
                    _levelsLoadedCount = 0;
                    _updateCoroutine = SendUpdate(updateCallback);
                    StartCoroutine(_updateCoroutine);
                }

                _isOperationCancelled = false;
                _thread = new Thread(LoadingThread);
                _thread.Start(new Tuple<IPreviewBeatmapLevel[], Action<BeatmapDetails[]>>(levels, onFinish));
            }

            public void StopLoading()
            {
                if (_updateCoroutine != null)
                {
                    StopCoroutine(_updateCoroutine);
                    _updateCoroutine = null;
                }

                _isOperationCancelled = true;
                _thread.Join();
                _thread = null;
            }

            private void LoadingThread(object obj)
            {
                try
                {
                    IPreviewBeatmapLevel[] levels = ((Tuple<IPreviewBeatmapLevel[], Action<BeatmapDetails[]>>)obj).Item1;
                    Action<BeatmapDetails[]> onFinish = ((Tuple<IPreviewBeatmapLevel[], Action<BeatmapDetails[]>>)obj).Item2;
                    List<BeatmapDetails> loadedLevels = new List<BeatmapDetails>(levels.Length);
                    List<SongDataCoreDataStatus> sdcErrorStatusList = new List<SongDataCoreDataStatus>(levels.Length);

                    var sw = Stopwatch.StartNew();

                    for (int index = 0; index < levels.Length; ++index)
                    {
                        if (_isOperationCancelled)
                            return;

                        _levelsLoadedCount = index;

                        IPreviewBeatmapLevel level = levels[index];
                        string levelID = GetSimplifiedLevelID(level);

                        if (level is IBeatmapLevel beatmapLevel)
                        {
                            loadedLevels.Add(new BeatmapDetails(beatmapLevel));
                        }
                        else if (BeatmapDetailsLoader._cache.ContainsKey(levelID))
                        {
                            loadedLevels.Add(BeatmapDetailsLoader._cache[levelID]);
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

                                    beatmapDetails = BeatmapDetails.CreateBeatmapDetailsFromFiles(customLevel);
                                    loadedLevels.Add(beatmapDetails);
                                    BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
                                }
                                else
                                {
                                    loadedLevels.Add(beatmapDetails);
                                    BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
                                }
                            }
                            else
                            {
                                if (SongDataCoreTweaks.IsModAvailable)
                                    sdcErrorStatusList.Add(status);

                                beatmapDetails = BeatmapDetails.CreateBeatmapDetailsFromFiles(customLevel);
                                loadedLevels.Add(beatmapDetails);
                                BeatmapDetailsLoader._cache[levelID] = beatmapDetails;
                            }
                        }
                        else
                        {
                            // not convertable (possible unbought DLC)
                            loadedLevels.Add(null);
                        }
                    }

                    _levelsLoadedCount = loadedLevels.Count;
                    sw.Stop();
                    Logger.log.Debug($"Finished loading the details of {loadedLevels.Count} beatmaps (took {sw.ElapsedMilliseconds / 1000f} seconds)");

                    int notLoadedCount = loadedLevels.Count(x => x == null);
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

                    _thread = null;
                    HMMainThreadDispatcher.instance.Enqueue(() => onFinish.Invoke(loadedLevels.ToArray()));
                }
                catch (Exception e)
                {
                    Logger.log.Warn("Unexpected exception occurred in loading thread");
                    Logger.log.Debug(e);
                }
            }

            private IEnumerator SendUpdate(Action<int> updateCallback)
            {
                yield return null;

                while (IsLoading)
                {
                    updateCallback.Invoke(_levelsLoadedCount);
                    yield return _updateDelay;
                }
            }
        }
        #endregion
    }
}
