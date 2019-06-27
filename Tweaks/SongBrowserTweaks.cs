using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using IPA.Loader;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SongBrowser;
using SongBrowser.UI;
using SongBrowser.DataAccess;
using EnhancedSearchAndFilters.UI;
using Version = SemVer.Version;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongBrowserTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static bool Initialized { get; private set; } = false;
        public static bool IsOldVersion { get; private set; } = false;

        private static object _songBrowserUI = null;

        public static bool Init()
        {
            if (!ModLoaded)
                return false;

            Logger.log.Info("SongBrowser mod found. Attempting to replace button behaviour.");

            Version version;
#pragma warning disable CS0618 // remove PluginManager.Plugin is obsolete warning
            var ipaMod = PluginManager.Plugins.FirstOrDefault(x => x.Name == "Song Browser");
#pragma warning restore CS0618
            var bsipaMod = PluginManager.AllPlugins.FirstOrDefault(x => x.Metadata.Id == "SongBrowser" || x.Metadata.Name == "Song Browser");
            if (ipaMod != null)
            {
                version = new Version(ipaMod.Version);
            }
            else if (bsipaMod != null)
            {
                version = bsipaMod.Metadata.Version;
            }
            else
            {
                Logger.log.Warn("Unable to find the version of the SongBrowser mod.");
                return false;
            }

            if (version.Major <= 5 && version.Minor < 2)
                return OldVersionInit();
            else if (version.Major >= 5 && version.Minor >= 2)
                return NewVersionInit();

            // there's no way it should get here, unless i'm a bad programmer
            return false;
        }

        /// <summary>
        /// Adapt this mod to SongBrowser before the 5.2.0 update.
        /// </summary>
        /// <returns>A boolean indicating whether the tweaks were done correctly.</returns>
        private static bool OldVersionInit()
        {
            // acquire all the UI elements we need to change before modifying
            Button searchButton;
            Button cancelSortButton;
            try
            {
                var buttonParentViewController = SongListUI.Instance.ButtonParentViewController;

                searchButton = buttonParentViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterSearchButton");
                // cancel sort button is found using x position (will need to be changed if button position changes)
                cancelSortButton = buttonParentViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 17.5f);
            }
            catch (InvalidOperationException)
            {
                Logger.log.Debug("Unable to find the buttons created by SongBrowser");
                return false;
            }

            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(SongListUI.Instance.SearchButtonPressed);

            cancelSortButton.onClick.AddListener(SongListUI.Instance.ClearButtonPressed);

            SongListUI.Instance.SearchButton = searchButton;
            SongListUI.Instance.ClearButton = cancelSortButton;

            Initialized = true;
            IsOldVersion = true;
            Logger.log.Info("Modified SongBrowser's search and clear sort buttons");
            return true;
        }

        /// <summary>
        /// Adapt this mod to SongBrowser version 5.2.0 and onwards.
        /// </summary>
        /// <returns>A boolean indicating whether the tweaks were done correctly.</returns>
        private static bool NewVersionInit()
        {
            // acquire all the UI elements we need to change before modifying

            Button xButton;
            Button filterByButton;
            Button clearFilterButton;

            Button searchButton;
            Button playlistButton;
            Button favoritesButton;

            var levelsViewController = SongListUI.Instance.LevelsViewController;
            try
            {
                _songBrowserUI = Resources.FindObjectsOfTypeAll<SongBrowserUI>().First();

                searchButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterSearchButton");
                playlistButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterPlaylistButton");
                favoritesButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterFavoritesButton");

                // these buttons are found using their respective x positions (will need to be changed if button position changes)
                xButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == -32.5f);
                filterByButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 30.5);
                clearFilterButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 54.5f);
            }
            catch (InvalidOperationException)
            {
                Logger.log.Debug("Unable to find the buttons created by SongBrowser");
                return false;
            }

            // SongBrowser filter buttons
            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(delegate ()
            {
                SongListUI.Instance.SearchButtonPressed();
                SongListUI.Instance.FilterButton.gameObject.SetActive(false);
                _songBrowserUI.InvokePrivateMethod("RefreshOuterUIState", new object[] { UIState.Main });
            });
            playlistButton.onClick.AddListener(delegate ()
            {
                // search button should be hidden already via RefreshOuterUIState
                SongListUI.Instance.FilterButton.gameObject.SetActive(false);
                SongListUI.Instance.UnapplyFilters(true);
            });
            favoritesButton.onClick.AddListener(delegate ()
            {
                // search button should be hidden already via RefreshOuterUIState
                SongListUI.Instance.FilterButton.gameObject.SetActive(false);
                SongListUI.Instance.UnapplyFilters(true);
            });
            Button filterButton = BeatSaberUI.CreateUIButton(levelsViewController.rectTransform, "ApplyButton", new Vector2(19.25f, 37f), new Vector2(16.75f, 5f),
                delegate ()
                {
                    SongListUI.Instance.FilterButtonPressed();
                    SongListUI.Instance.FilterButton.gameObject.SetActive(false);
                    _songBrowserUI.InvokePrivateMethod("RefreshOuterUIState", new object[] { UIState.Main });
                },
                "Other Filters");
            filterButton.SetButtonTextSize(2.25f);
            filterButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(4, 4, 2, 2);
            filterButton.ToggleWordWrapping(false);
            filterButton.gameObject.SetActive(false);

            // SongBrowser outer UI buttons
            filterByButton.onClick.AddListener(delegate ()
            {
                SongListUI.Instance.FilterButton.gameObject.SetActive(true);
            });
            clearFilterButton.onClick.RemoveAllListeners();
            clearFilterButton.onClick.AddListener(delegate ()
            {
                if ((_songBrowserUI as SongBrowserUI).Model.Settings.filterMode == SongFilterMode.Custom)
                    SongListUI.Instance.ClearButtonPressed();

                (_songBrowserUI as SongBrowserUI).CancelFilter();
                (_songBrowserUI as SongBrowserUI).RefreshSongUI();
            });
            xButton.onClick.AddListener(delegate ()
            {
                SongListUI.Instance.FilterButton.gameObject.SetActive(false);
            });

            // custom filter handler when the same level pack is selected
            SongBrowserModel.CustomFilterHandler = delegate (IBeatmapLevelPack levelPack)
            {
                IPreviewBeatmapLevel[] filteredSongs = SongListUI.Instance.ApplyFilters(levelPack.beatmapLevelCollection.beatmapLevels);
                return filteredSongs.ToList();
            };

            // on first load, SongBrowser uses the previously applied settings
            // if this mod's filters was applied last, we have to disable it, since our filters aren't saved across sessions
            if ((_songBrowserUI as SongBrowserUI).Model.Settings.filterMode == SongFilterMode.Custom)
            {
                (_songBrowserUI as SongBrowserUI).CancelFilter();
                (_songBrowserUI as SongBrowserUI).RefreshSongUI();
            }

            SongListUI.Instance.SearchButton = searchButton;
            SongListUI.Instance.FilterButton = filterButton;
            SongListUI.Instance.ClearButton = clearFilterButton;

            Initialized = true;
            IsOldVersion = false;
            Logger.log.Info("Modified SongBrowser's search, filter, and clear filter buttons");
            return true;
        }

        public static void FiltersApplied()
        {
            if (!ModLoaded || !Initialized || IsOldVersion)
                return;

            (_songBrowserUI as SongBrowserUI).Model.Settings.filterMode = SongFilterMode.Custom;
            SongListUI.Instance.ClearButton.SetButtonText("Other");
            (_songBrowserUI as SongBrowserUI).RefreshSongUI(false);
        }

        public static void FiltersUnapplied()
        {
            if (!ModLoaded || !Initialized || IsOldVersion)
                return;

            (_songBrowserUI as SongBrowserUI).CancelFilter();
            (_songBrowserUI as SongBrowserUI).RefreshSongUI();
        }

        public static void OnModeSelection()
        {
            if (!ModLoaded || !Initialized || IsOldVersion)
                return;

            // UIState is always reset to Main, so we need to disable the filter button
            SongListUI.Instance.FilterButton.gameObject.SetActive(false);
        }
    }
}
