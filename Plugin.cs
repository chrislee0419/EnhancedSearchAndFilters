using System.Linq;
using IPA;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using IPAPluginManager = IPA.Loader.PluginManager;
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
        }

        public void OnApplicationQuit()
        {
        }

        public void OnFixedUpdate()
        {

        }

        public void OnUpdate()
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

        }

        private void OnMenuSceneLoadedFresh()
        {
            BSEvents.menuSceneLoadedFresh -= OnMenuSceneLoadedFresh;

#pragma warning disable CS0618 // remove PluginManager.Plugins is obsolete warning
            Tweaks.BeatSaverDownloaderTweaks.ModLoaded = IPAPluginManager.AllPlugins.Any(x => x.Metadata.Id == "BeatSaverDownloader") || IPAPluginManager.Plugins.Any(x => x.Name == "BeatSaver Downloader");
            Tweaks.SongBrowserTweaks.ModLoaded = IPAPluginManager.AllPlugins.Any(x => x.Metadata.Id == "SongBrowser" || x.Metadata.Name == "Song Browser") || IPAPluginManager.Plugins.Any(x => x.Name == "Song Browser");
#pragma warning restore CS0618

            UI.SongListUI.Instance.OnMenuSceneLoadedFresh();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {

        }

        public void OnSceneUnloaded(Scene scene)
        {

        }
    }
}
