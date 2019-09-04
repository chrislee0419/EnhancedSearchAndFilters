using System.Linq;
using System.Collections.Generic;
using IPA;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using IPAPluginManager = IPA.Loader.PluginManager;
using SongCore;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.SongData;
using WordPredictionEngine = EnhancedSearchAndFilters.Search.WordPredictionEngine;

namespace EnhancedSearchAndFilters
{
    public class Plugin : IBeatSaberPlugin
    {
        public void Init(IPALogger logger)
        {
            Logger.log = logger;
        }

        public void OnApplicationStart()
        {
            BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            Loader.DeletingSong += SongCoreLoaderDeletingSong;
            Loader.LoadingStartedEvent += SongCoreLoaderLoadingStarted;
            Loader.SongsLoadedEvent += SongCoreLoaderFinishedLoading;
        }

        public void OnApplicationQuit()
        {
            WordPredictionEngine.Instance.CancelTasks();

            BeatmapDetailsLoader.Instance.CancelLoading();
            BeatmapDetailsLoader.Instance.CancelPopulatingCache();
            BeatmapDetailsLoader.Instance.SaveCacheToFile();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            if (nextScene.name == "MenuCore")
            {
                WordPredictionEngine.Instance.ResumeTasks();

                if (BeatmapDetailsLoader.Instance.IsCaching)
                    BeatmapDetailsLoader.Instance.StartPopulatingCache();
            }

            if (prevScene.name == "MenuCore" || nextScene.name == "GameCore")
            {
                WordPredictionEngine.Instance.PauseTasks();

                if (BeatmapDetailsLoader.Instance.IsCaching)
                    BeatmapDetailsLoader.Instance.PausePopulatingCache();
            }
        }

        private void OnMenuSceneLoadedFresh()
        {
#pragma warning disable CS0618 // remove PluginManager.Plugins is obsolete warning
            BeatSaverDownloaderTweaks.ModLoaded = IPAPluginManager.GetPluginFromId("BeatSaverDownloader") != null || IPAPluginManager.GetPlugin("BeatSaver Downloader") != null;
            SongBrowserTweaks.ModLoaded = IPAPluginManager.GetPluginFromId("SongBrowser") != null || IPAPluginManager.GetPlugin("Song Browser") != null || IPAPluginManager.Plugins.Any(x => x.Name == "Song Browser");
            SongDataCoreTweaks.ModLoaded = IPAPluginManager.GetPluginFromId("SongDataCore") != null;
#pragma warning restore CS0618

            // reset initialization status if settings were applied
            BeatSaverDownloaderTweaks.Initialized = false;
            SongBrowserTweaks.Initialized = false;

            UI.SongListUI.Instance.OnMenuSceneLoadedFresh();
        }
        public void SongCoreLoaderDeletingSong()
        {
            WordPredictionEngine.Instance.CancelTasks();
            BeatmapDetailsLoader.Instance.CancelPopulatingCache();
            Loader.OnLevelPacksRefreshed += SongCoreLoaderOnLevelPacksRefreshed;
        }

        public void SongCoreLoaderOnLevelPacksRefreshed()
        {
            Loader.OnLevelPacksRefreshed -= SongCoreLoaderOnLevelPacksRefreshed;
            BeatmapDetailsLoader.Instance.StartPopulatingCache();

            WordPredictionEngine.Instance.ClearCache();
        }

        public void SongCoreLoaderLoadingStarted(Loader loader)
        {
            WordPredictionEngine.Instance.CancelTasks();
            BeatmapDetailsLoader.Instance.CancelPopulatingCache();
        }

        public void SongCoreLoaderFinishedLoading(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> beatmaps)
        {
            // force load, since there might be new songs that can be cached
            BeatmapDetailsLoader.Instance.StartPopulatingCache(true);

            WordPredictionEngine.Instance.ClearCache();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }
    }
}
