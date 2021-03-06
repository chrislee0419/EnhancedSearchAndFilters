﻿using System.Linq;
using System.Collections.Generic;
using System.Text;
using IPA;
using IPA.Loader;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using IPAPluginManager = IPA.Loader.PluginManager;
using SemVer;
using HarmonyLib;
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
        public static Version Version { get; private set; }

        public const string HarmonyId = "com.chrislee0419.BeatSaber.EnhancedSearchAndFilters";

        [Init]
        public void Init(IPALogger logger, PluginMetadata metadata)
        {
            Logger.log = logger;
            Version = metadata.Version;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            BSEvents.earlyMenuSceneLoadedFresh += OnEarlyMenuSceneLoadedFresh;
            BSEvents.lateMenuSceneLoadedFresh += OnLateMenuSceneLoadedFresh;
            BSEvents.levelSelected += OnLevelSelected;
            Loader.DeletingSong += SongCoreLoaderDeletingSong;
            Loader.LoadingStartedEvent += SongCoreLoaderLoadingStarted;
            Loader.SongsLoadedEvent += SongCoreLoaderFinishedLoading;

            BSMLSettings.instance.AddSettingsMenu("<size=75%>Enhanced Search And Filters</size>", "EnhancedSearchAndFilters.UI.Views.SettingsView.bsml", SettingsMenu.instance);

            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            WordPredictionEngine.instance.CancelTasks();

            BeatmapDetailsLoader.instance.StopLoading();
            BeatmapDetailsLoader.instance.StopCaching();
            BeatmapDetailsLoader.instance.SaveCacheToFile();
        }

        private void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            if (nextScene.name == "MenuCore")
            {
                WordPredictionEngine.instance.ResumeTasks();

                if (BeatmapDetailsLoader.instance.IsCaching)
                    BeatmapDetailsLoader.instance.StartCaching();
            }
            else if (nextScene.name == "GameCore")
            {
                WordPredictionEngine.instance.PauseTasks();

                if (BeatmapDetailsLoader.instance.IsCaching)
                    BeatmapDetailsLoader.instance.PauseCaching();
            }
        }

        private void OnEarlyMenuSceneLoadedFresh(ScenesTransitionSetupDataSO setupData)
        {
            const string SongBrowserPluginId = "SongBrowser";
            const string SongDataCorePluginId = "SongDataCore";
            const string BeatSaverVotingPluginId = "BeatSaverVoting";

#pragma warning disable CS0618 // remove PluginManager.Plugins is obsolete warning
            SongBrowserTweaks.ModLoaded = IPAPluginManager.GetPluginFromId(SongBrowserPluginId) != null;
            SongBrowserTweaks.ModVersion = IPAPluginManager.GetPluginFromId(SongBrowserPluginId)?.Version;

            SongDataCoreTweaks.ModLoaded = IPAPluginManager.GetPluginFromId(SongDataCorePluginId) != null;
            SongDataCoreTweaks.ModVersion = IPAPluginManager.GetPluginFromId(SongDataCorePluginId)?.Version;

            BeatSaverVotingTweaks.ModLoaded = IPAPluginManager.GetPluginFromId(BeatSaverVotingPluginId) != null;
#pragma warning restore CS0618

            if (SongBrowserTweaks.ModLoaded)
                Logger.log.Debug($"SongBrowser detected (Is correct version = {SongBrowserTweaks.IsModAvailable})");
            if (SongDataCoreTweaks.ModLoaded)
                Logger.log.Debug($"SongDataCore detected (Is correct version = {SongDataCoreTweaks.IsModAvailable})");
            if (BeatSaverVotingTweaks.ModLoaded)
                Logger.log.Debug("BeatSaverVoting detected");

            // reset initialization status if settings were applied
            SongBrowserTweaks.Initialized = false;

            SongListUI.instance.OnEarlyMenuSceneLoadedFresh();
        }

        private void OnLateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO setupData)
        {
            SongListUI.instance.OnLateMenuSceneLoadedFresh();
        }

        private void OnLevelSelected(LevelCollectionViewController _, IPreviewBeatmapLevel level)
        {
            int capacity = level.levelID.Length + PluginConfig.LastLevelIDSeparator.Length * 2 + level.songName.Length + level.levelAuthorName.Length;
            StringBuilder sb = new StringBuilder(level.levelID, capacity);
            sb.Append(PluginConfig.LastLevelIDSeparator);
            sb.Append(level.songName);
            sb.Append(PluginConfig.LastLevelIDSeparator);
            sb.Append(level.levelAuthorName);

            PluginConfig.LastLevelID = sb.ToString();
        }

        private void SongCoreLoaderDeletingSong()
        {
            WordPredictionEngine.instance.CancelTasks();
            BeatmapDetailsLoader.instance.StopCaching();
            Loader.OnLevelPacksRefreshed += SongCoreLoaderOnLevelPacksRefreshed;
        }

        private void SongCoreLoaderOnLevelPacksRefreshed()
        {
            Loader.OnLevelPacksRefreshed -= SongCoreLoaderOnLevelPacksRefreshed;
            BeatmapDetailsLoader.instance.StartCaching();

            WordPredictionEngine.instance.ClearCache();
        }

        private void SongCoreLoaderLoadingStarted(Loader loader)
        {
            WordPredictionEngine.instance.CancelTasks();
            BeatmapDetailsLoader.instance.StopCaching();
        }

        private void SongCoreLoaderFinishedLoading(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> beatmaps)
        {
            // force load, since there might be new songs that can be cached
            BeatmapDetailsLoader.instance.StartCaching(true);

            WordPredictionEngine.instance.ClearCache();
        }
    }
}
