using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BS_Utils.Utilities;
using SongCore;

namespace EnhancedSearchAndFilters.SongData
{
    public partial class BeatmapDetailsLoader : PersistentSingleton<BeatmapDetailsLoader>
    {
        public event Action CachingStarted;
        public event Action CachingFinished;

        public bool IsCaching => _cacher?.IsCaching ?? false;
        public bool IsLoading => _loader.IsLoading;
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

        private ICacher _cacher;
        private ILoader _loader;

        // maintain our own cache because we don't need the cover image for each level or process notes/events
        // and the game's HMCache is limited to 30 songs
        // not using ConcurrentDictionary, since caching/loading/using this dictionary can only occur at one thread at a time anyways
        // (loading operation pauses caching operation)
        private static Dictionary<string, BeatmapDetails> _cache = new Dictionary<string, BeatmapDetails>();

        // load the details in batches (there is a noticable delay with queueing all async load tasks at once)
        private const int WorkChunkSize = 20;

        private const int WorkQueryChunkSize = 50;

        // 5 second timeout for loading tasks
        private static readonly TimeSpan TimeoutDelay = new TimeSpan(0, 0, 5);

        private static string CachedBeatmapDetailsFilePath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedFilterDetailsCache.json");

        public void Awake()
        {
            SelectCacher(CacherType.SeparateThread);
            SelectLoader(LoaderType.SeparateThread);
        }

        internal void SelectCacher(CacherType type)
        {
            if (_cacher != null)
            {
                _cacher.Dispose();
                _cacher = null;
            }

            switch (type)
            {
                case CacherType.Coroutine:
                    GameObject go = new GameObject("ESAFCoroutineCacher");
                    go.transform.SetParent(this.transform);
                    _cacher = go.AddComponent<CoroutineCacher>();

                    Logger.log.Debug("Using coroutines to cache BeatmapDetails objects");
                    break;

                case CacherType.SeparateThread:
                    _cacher = new ThreadedCacher();

                    Logger.log.Debug("Using separate thread to cache BeatmapDetails objects");
                    break;

                default:
                    return;
            }

            _cacher.CachingStarted += () => this.CachingStarted?.Invoke();
            _cacher.CachingFinished += delegate ()
            {
                SongsAreCached = true;
                this.CachingFinished?.Invoke();
            };
        }

        internal void SelectLoader(LoaderType type)
        {
            if (_loader != null)
                _loader.Dispose();

            switch (type)
            {
                case LoaderType.Coroutine:
                    GameObject go = new GameObject("ESAFCoroutineLoader");
                    go.transform.SetParent(this.transform);
                    _loader = go.AddComponent<CoroutineLoader>();

                    Logger.log.Debug("Using coroutines to load BeatmapDetails objects");
                    break;

                case LoaderType.SeparateThread:
                    go = new GameObject("ESAFThreadedLoader");
                    go.transform.SetParent(this.transform);
                    _loader = go.AddComponent<ThreadedLoader>();

                    Logger.log.Debug("Using separate thread to load BeatmapDetails objects");
                    break;
            }
        }

        /// <summary>
        /// Should be run whenever in the menu, to start/resume populating the beatmap details cache for filters.
        /// </summary>
        public void StartCaching(bool force = false)
        {
            if (IsLoading || (SongsAreCached && !force && !IsCaching) || PluginConfig.DisableFilters || _cacher == null)
                return;

            if (!IsCaching)
            {
                SongsAreCached = false;
                Logger.log.Info("Starting beatmap details caching operation");
                _cacher.StartCaching();
            }
            else
            {
                Logger.log.Info("Resuming beatmap details caching operation");
                _cacher.ResumeCaching();
            }
        }

        /// <summary>
        /// Blocks the thread that is populating the cache. Can be resumed with <see cref="StartCaching(bool)"/>.
        /// </summary>
        public void PauseCaching()
        {
            if (!IsCaching || _cacher == null)
                return;

            Logger.log.Info("Pausing beatmap details caching operation");
            _cacher.PauseCaching();
        }

        /// <summary>
        /// Only used to stop the caching thread during <see cref="Plugin.OnApplicationQuit()"/>.
        /// </summary>
        public void StopCaching()
        {
            if (!IsCaching || _cacher == null)
                return;

            Logger.log.Info("Cancelling ongoing beatmap details caching operation");
            _cacher.StopCaching();
        }

        /// <summary>
        /// Loads the beatmap details of a list of <see cref="IPreviewBeatmapLevel"/>.
        /// </summary>
        /// <param name="levels">A list of IPreviewBeatmaps.</param>
        /// <param name="updateCallback">A function that will run every 0.1s that gets the number of beatmaps currently loaded.</param>
        /// <param name="onFinish">The function that is called when the details of all beatmaps are retrieved.</param>
        public void StartLoading(IPreviewBeatmapLevel[] levels, Action<int> updateCallback = null, Action<BeatmapDetails[]> onFinish = null)
        {
            if (onFinish == null)
                return;

            if (IsLoading)
            {
                Logger.log.Warn("Unable to start beatmap details loading operation (another loading operation already exists)");
                return;
            }

            if (IsCaching)
                PauseCaching();

            Action<BeatmapDetails[]> loadingFinishedCallback = delegate (BeatmapDetails[] beatmapDetailsArray)
            {
                StartCaching();
                onFinish.Invoke(beatmapDetailsArray);
            };
            _loader.StartLoading(levels, updateCallback, loadingFinishedCallback);
        }

        /// <summary>
        /// Cancels an ongoing loading operation.
        /// </summary>
        public void StopLoading()
        {
            if (!IsLoading)
                return;

            _loader.StopLoading();

            if (IsCaching || !SongsAreCached)
                StartCaching();
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
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutDelay);
            CustomBeatmapLevel customLevel = new CustomBeatmapLevel(CreateLevelCopyWithReplacedMediaLoader(level, MediaLoader), null, null);

            try
            {
                BeatmapLevelData beatmapData = await LevelLoader.LoadBeatmapLevelDataAsync(level.customLevelPath, customLevel, level.standardLevelInfoSaveData, tokenSource.Token);
                if (beatmapData != null)
                {
                    customLevel.SetBeatmapLevelData(beatmapData);

                    string levelID = GetSimplifiedLevelID(customLevel);
                    if (!_cache.ContainsKey(levelID))
                        _cache[GetSimplifiedLevelID(customLevel)] = new BeatmapDetails(customLevel);

                    try
                    {
                        onFinish?.Invoke(customLevel);
                    }
                    catch (Exception e)
                    {
                        Logger.log.Warn("Unexpected exception occurred in delegate after loading beatmap");
                        Logger.log.Debug(e);
                    }
                }
                else
                {
                    Logger.log.Debug($"Unable to load beatmap level data for '{level.songName}' (no data returned)");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.log.Debug($"Unable to load beatmap level data for '{level.songName}' (load task timed out)");
            }

            tokenSource.Dispose();
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

        #region Static Utilities
        private static List<IPreviewBeatmapLevel> GetAllCustomLevels()
        {
            List<IPreviewBeatmapLevel> allCustomLevels = Loader.CustomLevelsCollection.beatmapLevels.ToList();
            foreach (var folder in Loader.SeperateSongFolders)
            {
                if (!folder.SongFolderEntry.WIP)
                    allCustomLevels.AddRange(folder.Levels.Values);
            }

            return allCustomLevels;
        }

        private static IEnumerator<OrderedBeatmapDetails> GetOrderedCustomBeatmapDetailsCoroutine(CustomPreviewBeatmapLevel level, int index)
        {
            IEnumerator<BeatmapDetails> loadingCoroutine = BeatmapDetails.CreateBeatmapDetailsFromFilesCoroutine(level);

            while (loadingCoroutine.MoveNext())
            {
                BeatmapDetails beatmapDetails = loadingCoroutine.Current;
                if (beatmapDetails != null)
                {
                    _cache[beatmapDetails.LevelID] = beatmapDetails;
                    yield return new OrderedBeatmapDetails(index, beatmapDetails);
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }

            yield return new OrderedBeatmapDetails(index, null);
        }

        private static CustomPreviewBeatmapLevel CreateLevelCopyWithReplacedMediaLoader(CustomPreviewBeatmapLevel level, CachedMediaAsyncLoader mediaLoader)
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
        #endregion // Static Utilities

        #region Private classes and interfaces
        private class OrderedBeatmapDetails
        {
            public int Position { get; set; }
            public BeatmapDetails Details { get; set; }
            public string LevelID => Details?.LevelID;

            public OrderedBeatmapDetails(int position, BeatmapDetails beatmapDetails)
            {
                this.Position = position;
                this.Details = beatmapDetails;
            }
        }
        private interface ICacher : IDisposable
        {
            event Action CachingStarted;
            event Action CachingFinished;

            bool IsCaching { get; }

            void StartCaching();
            void StopCaching();
            void ResumeCaching();
            void PauseCaching();
        }

        private interface ILoader : IDisposable
        {
            bool IsLoading { get; }

            void StartLoading(IPreviewBeatmapLevel[] levels, Action<int> updateCallback, Action<BeatmapDetails[]> onFinish);
            void StopLoading();
        }

        internal enum CacherType
        {
            None,
            Coroutine,
            SeparateThread
        }

        internal enum LoaderType
        {
            Coroutine,
            SeparateThread
        }
        #endregion // Private classes and interfaces
    }
}
