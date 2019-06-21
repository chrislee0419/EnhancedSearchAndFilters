using System.Linq;
using System.Collections.Generic;
using IPA;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using IPAPluginManager = IPA.Loader.PluginManager;
using SongCore;
using CustomUI.Utilities;

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
            Loader.SongsLoadedEvent += SongCoreLoaderFinishedLoading;
        }

        public void OnApplicationQuit()
        {
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
            if (nextScene.name == "MenuCore" && BeatmapDetailsLoader.Instance.IsCaching)
                BeatmapDetailsLoader.Instance.StartPopulatingCache();
            if ((prevScene.name == "MenuCore" || nextScene.name == "GameCore") && BeatmapDetailsLoader.Instance.IsCaching)
                BeatmapDetailsLoader.Instance.PausePopulatingCache();
        }

        private void OnMenuSceneLoadedFresh()
        {
#pragma warning disable CS0618 // remove PluginManager.Plugins is obsolete warning
            Tweaks.BeatSaverDownloaderTweaks.ModLoaded = IPAPluginManager.AllPlugins.Any(x => x.Metadata.Id == "BeatSaverDownloader") || IPAPluginManager.Plugins.Any(x => x.Name == "BeatSaver Downloader");
            Tweaks.SongBrowserTweaks.ModLoaded = IPAPluginManager.AllPlugins.Any(x => x.Metadata.Id == "SongBrowser" || x.Metadata.Name == "Song Browser") || IPAPluginManager.Plugins.Any(x => x.Name == "Song Browser");
#pragma warning restore CS0618

            UI.SongListUI.Instance.OnMenuSceneLoadedFresh();
        }

        public void SongCoreLoaderFinishedLoading(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> beatmaps)
        {
            BeatmapDetailsLoader.Instance.StartPopulatingCache();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {

        }

        public void OnSceneUnloaded(Scene scene)
        {

        }
    }
}
