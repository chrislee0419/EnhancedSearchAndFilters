using System.Linq;
using System.Collections.Generic;
using IPA;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using IPAPluginManager = IPA.Loader.PluginManager;
using SemVer;
using SongCore;
using BeatSaberMarkupLanguage.Settings;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.UI;
using WordPredictionEngine = EnhancedSearchAndFilters.Search.WordPredictionEngine;

namespace EnhancedSearchAndFilters
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static Version Version => IPAPluginManager.GetPluginFromId("EnhancedSearchAndFilters")?.Version;

        [Init]
        public void Init(IPALogger logger)
        {
            Logger.log = logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            Loader.DeletingSong += SongCoreLoaderDeletingSong;
            Loader.LoadingStartedEvent += SongCoreLoaderLoadingStarted;
            Loader.SongsLoadedEvent += SongCoreLoaderFinishedLoading;

            BSMLSettings.instance.AddSettingsMenu("<size=75%>Enhanced Search And Filters</size>", "EnhancedSearchAndFilters.UI.Views.SettingsView.bsml", SettingsMenu.instance);
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            WordPredictionEngine.instance.CancelTasks();

            BeatmapDetailsLoader.instance.CancelLoading();
            BeatmapDetailsLoader.instance.CancelPopulatingCache();
            BeatmapDetailsLoader.instance.SaveCacheToFile();
        }

        private void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            if (nextScene.name == "MenuCore")
            {
                WordPredictionEngine.instance.ResumeTasks();

                if (BeatmapDetailsLoader.instance.IsCaching)
                    BeatmapDetailsLoader.instance.StartPopulatingCache();
            }

            if (prevScene.name == "MenuCore" || nextScene.name == "GameCore")
            {
                WordPredictionEngine.instance.PauseTasks();

                if (BeatmapDetailsLoader.instance.IsCaching)
                    BeatmapDetailsLoader.instance.PausePopulatingCache();
            }
        }

        private void OnMenuSceneLoadedFresh()
        {
#pragma warning disable CS0618 // remove PluginManager.Plugins is obsolete warning
            SongBrowserTweaks.ModLoaded = IPAPluginManager.GetPluginFromId("SongBrowser") != null || IPAPluginManager.GetPlugin("Song Browser") != null || IPAPluginManager.Plugins.Any(x => x.Name == "Song Browser");
            SongDataCoreTweaks.ModLoaded = IPAPluginManager.GetPluginFromId("SongDataCore") != null;
            SongDataCoreTweaks.ModVersion = IPAPluginManager.GetPluginFromId("SongDataCore")?.Version;
#pragma warning restore CS0618

            if (SongBrowserTweaks.ModLoaded)
                Logger.log.Debug("SongBrowser detected");
            if (SongDataCoreTweaks.ModLoaded)
                Logger.log.Debug($"SongDataCore detected (Is correct version = {SongDataCoreTweaks.IsModAvailable})");

            // reset initialization status if settings were applied
            SongBrowserTweaks.Initialized = false;

            SongListUI.instance.OnMenuSceneLoadedFresh();
        }
        private void SongCoreLoaderDeletingSong()
        {
            WordPredictionEngine.instance.CancelTasks();
            BeatmapDetailsLoader.instance.CancelPopulatingCache();
            Loader.OnLevelPacksRefreshed += SongCoreLoaderOnLevelPacksRefreshed;
        }

        private void SongCoreLoaderOnLevelPacksRefreshed()
        {
            Loader.OnLevelPacksRefreshed -= SongCoreLoaderOnLevelPacksRefreshed;
            BeatmapDetailsLoader.instance.StartPopulatingCache();

            WordPredictionEngine.instance.ClearCache();
        }

        private void SongCoreLoaderLoadingStarted(Loader loader)
        {
            WordPredictionEngine.instance.CancelTasks();
            BeatmapDetailsLoader.instance.CancelPopulatingCache();
        }

        private void SongCoreLoaderFinishedLoading(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> beatmaps)
        {
            // force load, since there might be new songs that can be cached
            BeatmapDetailsLoader.instance.StartPopulatingCache(true);

            WordPredictionEngine.instance.ClearCache();
        }
    }
}
