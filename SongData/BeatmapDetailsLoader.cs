using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BS_Utils.Utilities;

namespace EnhancedSearchAndFilters.SongData
{
    public partial class BeatmapDetailsLoader : PersistentSingleton<BeatmapDetailsLoader>
    {

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

        // maintain our own cache because we don't need the cover image for each level or process notes/events
        // and the game's HMCache is limited to 30 songs
        private Dictionary<string, BeatmapDetails> _cache = new Dictionary<string, BeatmapDetails>();

        // load the details in batches (there is a noticable delay with queueing all async load tasks at once)
        private const int WorkChunkSize = 5;

        private const int WorkQueryChunkSize = 50;

        // 5 second timeout for loading tasks
        private static readonly TimeSpan TimeoutDelay = new TimeSpan(0, 0, 5);

        private static string CachedBeatmapDetailsFilePath = Path.Combine(Environment.CurrentDirectory, "UserData", "EnhancedFilterDetailsCache.json");

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
            }
            catch (OperationCanceledException)
            {
                Logger.log.Debug($"Unable to load beatmap level data for '{level.songName}' (loading timed out)");
            }

            tokenSource.Dispose();
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

        #region Static Utilities
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

        private static CancellationTokenSource CreateLinkedCancellationTokenSource(CancellationToken origToken, out CancellationTokenSource newTokenSource, TimeSpan? delay = null)
        {
            if (delay.HasValue)
                newTokenSource = new CancellationTokenSource(delay.Value);
            else
                newTokenSource = new CancellationTokenSource();

            return CancellationTokenSource.CreateLinkedTokenSource(origToken, newTokenSource.Token);
        }
        #endregion // Static Utilities
    }
}
