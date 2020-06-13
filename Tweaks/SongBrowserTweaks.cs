using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SemVer;
using BS_Utils.Utilities;
using SongBrowser;
using SongBrowser.Internals;
using SongBrowser.UI;
using SongBrowser.DataAccess;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.UI;
using Version = SemVer.Version;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongBrowserTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static Version ModVersion { get; set; }
        public static bool IsModAvailable
        {
            get
            {
                try
                {
                    return ModLoaded && ValidVersionRange.IsSatisfied(ModVersion);
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }
        }
        public static bool Initialized { get; set; } = false;

        private static object _songBrowserUI = null;

        private static Button _searchButton;
        private static Button _filterButton;
        private static Button _clearFiltersButton;

        public static readonly Range ValidVersionRange = new Range("^6.0.6");

        public static bool Init()
        {
            if (!IsModAvailable)
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
            LevelCollectionViewController levelCollectionViewController;
            Button xButton;
            Button filterByButton;
            Button[] existingFilterButtons;

            var levelSelectionNavigationController = SongListUI.instance.LevelSelectionNavigationController;
            try
            {
                levelCollectionViewController = levelSelectionNavigationController.GetPrivateField<LevelCollectionViewController>("_levelCollectionViewController");

                _songBrowserUI = Resources.FindObjectsOfTypeAll<SongBrowserUI>().First();

                _searchButton = levelCollectionViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterSearchButton");
                existingFilterButtons = levelCollectionViewController.GetComponentsInChildren<Button>(true).Where(x => x.name.StartsWith("Filter") && x.name.EndsWith("Button")).ToArray();

                // these buttons are found using their respective x positions (will need to be changed if button position changes)
                xButton = levelCollectionViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == -32.5f);
                filterByButton = levelCollectionViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 30.5);
                _clearFiltersButton = levelCollectionViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 54.5f);
            }
            catch (InvalidOperationException)
            {
                Logger.log.Debug("Unable to find the buttons created by SongBrowser");
                return false;
            }

            // SongBrowser filter buttons
            if (!PluginConfig.DisableSearch)
            {
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate ()
                {
                    SongListUI.instance.SearchButtonPressed();
                    _filterButton.gameObject.SetActive(false);
                    _songBrowserUI.InvokeMethod("RefreshOuterUIState", new object[] { UIState.Main });
                });
            }
            else
            {
                Logger.log.Info("Enhanced search functionality is disabled. SongBrowser's \"Search\" button is not modified");
            }

            if (!PluginConfig.DisableFilters)
            {
                foreach (var button in existingFilterButtons)
                {
                    if (button.name == "FilterSearchButton")
                        continue;

                    button.onClick.AddListener(delegate ()
                    {
                        // search button should be hidden already via RefreshOuterUIState
                        _filterButton.gameObject.SetActive(false);
                        SongListUI.instance.UnapplyFilters();
                    });
                }

                // create this mod's filter button
                _filterButton = BeatSaberUI.CreateUIButton(levelCollectionViewController.rectTransform, "ApplyButton", new Vector2(-18f + (12.75f * existingFilterButtons.Length), 37f), new Vector2(16.75f, 5f),
                    delegate ()
                    {
                        SongListUI.instance.FilterButtonPressed();
                        _filterButton.gameObject.SetActive(false);
                        _songBrowserUI.InvokeMethod("RefreshOuterUIState", new object[] { UIState.Main });
                    },
                    "Other Filters");
                _filterButton.SetButtonTextSize(2.25f);
                _filterButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(4, 4, 2, 2);
                _filterButton.ToggleWordWrapping(false);
                _filterButton.gameObject.SetActive(false);

                // SongBrowser outer UI buttons
                filterByButton.onClick.AddListener(delegate ()
                {
                    _filterButton.gameObject.SetActive(true);
                });
                _clearFiltersButton.onClick.RemoveAllListeners();
                _clearFiltersButton.onClick.AddListener(delegate ()
                {
                    SongListUI.instance.ClearButtonPressed();

                    // filters are cancelled by ClearButtonPressed -> UnapplyFilters -> FilterFlowCoordinatorFiltersUnapplied
                    // but only if custom filters are applied, otherwise we do it here
                    if ((_songBrowserUI as SongBrowserUI).Model.Settings.filterMode != SongFilterMode.None)
                        _FiltersUnapplied();
                });
                xButton.onClick.AddListener(delegate ()
                {
                    _filterButton.gameObject.SetActive(false);
                });

                // custom filter handler when the same level pack is selected
                SongBrowserModel.CustomFilterHandler = delegate (IAnnotatedBeatmapLevelCollection levelPack)
                {
                    FilterList.ApplyFilter(levelPack.beatmapLevelCollection.beatmapLevels, out var filteredLevels, false);
                    return filteredLevels.ToList();
                };

                // on first load, SongBrowser uses the previously applied settings
                // if this mod's filters was applied last, we have to disable it, since our filters aren't saved across sessions
                if ((_songBrowserUI as SongBrowserUI).Model.Settings.filterMode == SongFilterMode.Custom)
                {
                    (_songBrowserUI as SongBrowserUI).CancelFilter();
                    (_songBrowserUI as SongBrowserUI).ProcessSongList();
                    (_songBrowserUI as SongBrowserUI).RefreshSongUI();
                }
            }
            else
            {
                Logger.log.Info("Filter functionality is disabled. SongBrowser's buttons are not modified");
            }

            Initialized = true;
            Logger.log.Info("Modified SongBrowser's search, filter, and clear filter buttons");
            return true;
        }

        /// <summary>
        /// Used to apply the filter and refresh the UI for SongBrowser.
        /// </summary>
        public static void ApplyFilters()
        {
            if (!IsModAvailable || !Initialized)
                return;
            _ApplyFilters();
        }

        private static void _ApplyFilters()
        {
            // unapply any existing filters first
            //(_songBrowserUI as SongBrowserUI).CancelFilter();

            (_songBrowserUI as SongBrowserUI).Model.Settings.filterMode = SongFilterMode.Custom;
            _clearFiltersButton.SetButtonText("Other");
            (_songBrowserUI as SongBrowserUI).ProcessSongList();
            (_songBrowserUI as SongBrowserUI).RefreshSongUI();
        }

        /// <summary>
        /// Resets the UI for SongBrowser after our filter has been unapplied.
        /// </summary>
        public static void FiltersUnapplied()
        {
            if (!IsModAvailable || !Initialized)
                return;
            _FiltersUnapplied();
        }

        private static void _FiltersUnapplied()
        {
            (_songBrowserUI as SongBrowserUI).CancelFilter();
            (_songBrowserUI as SongBrowserUI).ProcessSongList();
            (_songBrowserUI as SongBrowserUI).RefreshSongUI();
        }

        /// <summary>
        /// Gets whether the filter mode is set by SongBrowser.
        /// </summary>
        /// <returns>A boolean indicating whether a SongBrowser filter is applied.</returns>
        public static bool IsFilterApplied()
        {
            if (!IsModAvailable || !Initialized)
                return false;

            return _IsFilterApplied();
        }

        private static bool _IsFilterApplied()
        {
            return (_songBrowserUI as SongBrowserUI).Model.Settings.filterMode != SongFilterMode.None;
        }

        public static void DisableOtherFiltersButton()
        {
            if (!IsModAvailable || !Initialized || _filterButton == null)
                return;

            _filterButton.gameObject.SetActive(false);
        }
    }
}
