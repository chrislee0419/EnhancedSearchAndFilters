using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        public static bool Initialized { get; private set; } = false;
        /// <summary>
        /// Not really used anymore, will be deleted at some point
        /// </summary>
        public static bool IsOldVersion { get; private set; } = false;

        private static object _songBrowserUI = null;

        public static bool Init()
        {
            if (!ModLoaded)
                return false;

            Logger.log.Info("SongBrowser mod found. Attempting to replace button behaviour.");

            return NewVersionInit();
        }

        /// <summary>
        /// Adapt this mod to SongBrowser before the 5.2.0 update. 
        /// Legacy code, will remove this at some point.
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

            CreateIconFilterButton();
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

        /// <summary>
        /// Alternative filter button intended to sit alongside the SongBrowser mod's filter buttons.
        /// </summary>
        private static void CreateIconFilterButton()
        {
            if (SongListUI.Instance.FilterButton != null || SongListUI.Instance.ButtonParentViewController == null)
                return;

            // modify button design to fit with other SongBrowser icon buttons
            const float iconSize = 2.5f;
            const float iconScale = 1f;
            Vector2 buttonPos = new Vector2(52.625f, 37.25f);
            Vector2 buttonSize = new Vector2(5f, 5f);

            var filterButton = SongListUI.Instance.ButtonParentViewController.CreateUIButton("PracticeButton", buttonPos, buttonSize, SongListUI.Instance.FilterButtonPressed);
            UnityEngine.Object.Destroy(filterButton.GetComponentInChildren<TextMeshProUGUI>(true));

            Sprite filterIcon = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");
            Image icon = filterButton.GetComponentsInChildren<Image>(true).First(x => x.name == "Icon");

            HorizontalLayoutGroup hgroup = icon.rectTransform.parent.GetComponent<HorizontalLayoutGroup>();
            hgroup.padding = new RectOffset(1, 1, 0, 0);

            icon.sprite = filterIcon;
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.rectTransform.localScale = new Vector2(iconScale, iconScale);

            SongListUI.Instance.FilterButton = filterButton;

            Logger.log.Debug("Created icon filter button.");
        }

        /// <summary>
        /// Used to refresh the UI for SongBrowser after our filter has been applied.
        /// </summary>
        public static void FiltersApplied()
        {
            if (!ModLoaded || !Initialized || IsOldVersion)
                return;
            _FiltersApplied();
        }

        private static void _FiltersApplied()
        {
            (_songBrowserUI as SongBrowserUI).Model.Settings.filterMode = SongFilterMode.Custom;
            SongListUI.Instance.ClearButton.SetButtonText("Other");
            (_songBrowserUI as SongBrowserUI).RefreshSongUI(false);
        }

        /// <summary>
        /// Resets the UI for SongBrowser after our filter has been unapplied.
        /// </summary>
        public static void FiltersUnapplied()
        {
            if (!ModLoaded || !Initialized || IsOldVersion)
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
            if (!ModLoaded || !Initialized || IsOldVersion)
                return;

            // UIState is always reset to Main, so we need to disable the filter button
            SongListUI.Instance.FilterButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set the levels shown by the LevelsPackLevelsViewController.
        /// </summary>
        /// <param name="levels">Array of levels to set in the view controller.</param>
        /// <returns>A boolean representing whether this was successful.</returns>
        public static bool SetFilteredSongs(IPreviewBeatmapLevel[] levels)
        {
            if (!ModLoaded)
                return false;

            // SongBrowser does some weird checks when sorting that we have to accomodate for
            if (levels.Any())
            {
                var levelsViewController = SongListUI.Instance.LevelsViewController;

                if (levels[0] is CustomPreviewBeatmapLevel)
                {
                    SongCoreCustomBeatmapLevelPack customLevelPack = new SongCoreCustomBeatmapLevelPack("", SongListUI.FilteredSongsPackName, levelsViewController.levelPack.coverImage, new CustomBeatmapLevelCollection(levels.Cast<CustomPreviewBeatmapLevel>().ToArray()));
                    levelsViewController.SetData(customLevelPack);

                    if (SongBrowserTweaks.Initialized && !SongBrowserTweaks.IsOldVersion)
                        SongBrowserTweaks._FiltersApplied();
                    return true;
                }
                else if (levels[0] is BeatmapLevelSO)
                {
                    BeatmapLevelCollectionSO levelCollection = ScriptableObject.CreateInstance<BeatmapLevelCollectionSO>();
                    levelCollection.SetPrivateField("_beatmapLevels", levels.Cast<BeatmapLevelSO>().ToArray());

                    BeatmapLevelPackSO beatmapLevelPack = ScriptableObject.CreateInstance<BeatmapLevelPackSO>();
                    beatmapLevelPack.SetPrivateField("_packID", "");
                    beatmapLevelPack.SetPrivateField("_packName", SongListUI.FilteredSongsPackName);
                    beatmapLevelPack.SetPrivateField("_coverImage", levelsViewController.levelPack.coverImage);
                    beatmapLevelPack.SetPrivateField("_beatmapLevelCollection", levelCollection);

                    levelsViewController.SetData(beatmapLevelPack);

                    if (SongBrowserTweaks.Initialized && !SongBrowserTweaks.IsOldVersion)
                        SongBrowserTweaks._FiltersApplied();
                    return true;
                }
                else if (levels[0] is PreviewBeatmapLevelSO)
                {
                    PreviewBeatmapLevelCollectionSO levelCollection = ScriptableObject.CreateInstance<PreviewBeatmapLevelCollectionSO>();
                    levelCollection.SetPrivateField("_beatmapLevels", levels.Cast<PreviewBeatmapLevelSO>().ToArray());

                    PreviewBeatmapLevelPackSO previewLevelPack = ScriptableObject.CreateInstance<PreviewBeatmapLevelPackSO>();
                    previewLevelPack.SetPrivateField("_packID", "");
                    previewLevelPack.SetPrivateField("_packName", SongListUI.FilteredSongsPackName);
                    previewLevelPack.SetPrivateField("_coverImage", levelsViewController.levelPack.coverImage);
                    previewLevelPack.SetPrivateField("_previewBeatmapLevelCollection", levelCollection);

                    levelsViewController.SetData(previewLevelPack);

                    if (SongBrowserTweaks.Initialized && !SongBrowserTweaks.IsOldVersion)
                        SongBrowserTweaks._FiltersApplied();
                    return true;
                }

                Logger.log.Warn("Filtered song list could not be cast to any type used by SongBrowser's sort feature. Sorting this filtered song list will probably not work.");
                // fallback to default implementation
            }

            return false;
        }
    }
}
