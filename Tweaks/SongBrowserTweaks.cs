using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SongCore.OverrideClasses;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SongBrowser;
using SongBrowser.UI;
using SongBrowser.DataAccess;
using EnhancedSearchAndFilters.UI;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongBrowserTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static bool Initialized { get; set; } = false;

        private static object _songBrowserUI = null;

        public static bool Init()
        {
            if (!ModLoaded)
                return false;

            Logger.log.Info("Attempting to initialize SongBrowser tweaks.");

            return _Init();
        }

        /// <summary>
        /// Adapt this mod to SongBrowser version 5.4.0 and onwards.
        /// </summary>
        /// <returns>A boolean indicating whether the tweaks were done correctly.</returns>
        private static bool _Init()
        {
            // acquire all the UI elements we need to change before modifying
            Button xButton;
            Button filterByButton;
            Button clearFilterButton;

            Button searchButton;
            Button[] filterButtons;

            var levelsViewController = SongListUI.Instance.LevelsViewController;
            try
            {
                _songBrowserUI = Resources.FindObjectsOfTypeAll<SongBrowserUI>().First();

                searchButton = levelsViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterSearchButton");
                filterButtons = levelsViewController.GetComponentsInChildren<Button>(true).Where(x => x.name.StartsWith("Filter") && x.name.EndsWith("Button")).ToArray();

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

            foreach (var button in filterButtons)
            {
                if (button.name == "FilterSearchButton")
                    continue;

                button.onClick.AddListener(delegate ()
                {
                    // search button should be hidden already via RefreshOuterUIState
                    SongListUI.Instance.FilterButton.gameObject.SetActive(false);
                    SongListUI.Instance.UnapplyFilters(true);
                });
            }

            Button filterButton = BeatSaberUI.CreateUIButton(levelsViewController.rectTransform, "ApplyButton", new Vector2(-18f + (12.75f * filterButtons.Length), 37f), new Vector2(16.75f, 5f),
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
                return SongListUI.Instance.ApplyFiltersForSongBrowser(levelPack.beatmapLevelCollection.beatmapLevels);
            };

            // on first load, SongBrowser uses the previously applied settings
            // if this mod's filters was applied last, we have to disable it, since our filters aren't saved across sessions
            if ((_songBrowserUI as SongBrowserUI).Model.Settings.filterMode == SongFilterMode.Custom)
            {
                (_songBrowserUI as SongBrowserUI).CancelFilter();
                (_songBrowserUI as SongBrowserUI).ProcessSongList();
                (_songBrowserUI as SongBrowserUI).RefreshSongUI();
            }

            SongListUI.Instance.SearchButton = searchButton;
            SongListUI.Instance.FilterButton = filterButton;
            SongListUI.Instance.ClearButton = clearFilterButton;

            Initialized = true;
            Logger.log.Info("Modified SongBrowser's search, filter, and clear filter buttons");
            return true;
        }

        /// <summary>
        /// Used to apply the filter and refresh the UI for SongBrowser.
        /// </summary>
        /// <returns>A boolean representing whether filters have been applied successfully.</returns>
        public static bool FiltersApplied()
        {
            if (!ModLoaded || !Initialized)
                return false;
            return _FiltersApplied();
        }

        private static bool _FiltersApplied()
        {
            // unapply any existing filters first
            (_songBrowserUI as SongBrowserUI).CancelFilter();

            (_songBrowserUI as SongBrowserUI).Model.Settings.filterMode = SongFilterMode.Custom;
            SongListUI.Instance.ClearButton.SetButtonText("Other");
            (_songBrowserUI as SongBrowserUI).ProcessSongList();
            (_songBrowserUI as SongBrowserUI).RefreshSongUI();

            return true;
        }

        /// <summary>
        /// Resets the UI for SongBrowser after our filter has been unapplied.
        /// </summary>
        public static void FiltersUnapplied()
        {
            if (!ModLoaded || !Initialized)
                return;
            _FiltersUnapplied();
        }

        private static void _FiltersUnapplied()
        {
            (_songBrowserUI as SongBrowserUI).CancelFilter();
            (_songBrowserUI as SongBrowserUI).RefreshSongUI();
        }

        public static void OnModeSelection()
        {
            if (!ModLoaded || !Initialized)
                return;

            // UIState is always reset to Main, so we need to disable the filter button
            SongListUI.Instance.FilterButton.gameObject.SetActive(false);
        }
    }
}
