using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using VRUI;
using TableView = HMUI.TableView;
using TableViewScroller = HMUI.TableViewScroller;
using NoTransitionsButton = HMUI.NoTransitionsButton;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.UI.FlowCoordinators;
using EnhancedSearchAndFilters.UI.ViewControllers;

namespace EnhancedSearchAndFilters.UI
{
    internal enum FreePlayMode
    {
        Solo,
        Party,
        Campaign
    }

    class SongListUI : MonoBehaviour
    {
        private FlowCoordinator _freePlayFlowCoordinator;
        private SearchFlowCoordinator _searchFlowCoordinator;
        private FilterViewController _filterViewController;

        private LevelPackLevelsTableView _levelsTableViewContainer;
        private TableView _levelsTableView;
        private IBeatmapLevelPack _lastPack;

        public LevelPackLevelsViewController LevelsViewController { get; private set; } = null;
        public DismissableNavigationController ButtonParentViewController { get; private set; } = null;

        public Button SearchButton { get; set; } = null;
        public Button FilterButton { get; set; } = null;
        public Button ClearButton { get; set; } = null;

        private static readonly Vector2 DefaultSearchButtonPosition = new Vector2(12f, 35.5f);
        private static readonly Vector2 DefaultFilterButtonPosition = new Vector2(30f, 35.5f);
        private static readonly Vector2 DefaultClearButtonPosition = new Vector2(48f, 35.5f);
        private static readonly Vector2 DefaultButtonSize = new Vector2(18f, 6f);
        private const string FilterButtonText = "<color=#FFFFCC>Filter</color>";
        private const string FilterButtonHighlightedText = "<color=#444400>Filter</color>";
        private const string FilterButtonAppliedText = "<color=#DDFFDD>Filter\n(Applied)</color>";
        private const string FilterButtonHighlightedAppliedText = "<color=#004400>Filter\n(Applied)</color>";
        private const string ClearFilterButtonText = "<color=#FFFFCC>Clear\nFilters</color>";
        private const string ClearFilterButtonHighlightedText = "<color=#444400>Clear\nFilters</color>";
        private const string ClearFilterButtonAppliedText = "<color=#FFDDDD>Clear\nFilters</color>";
        private const string ClearFilterButtonHighlightedAppliedText = "<color=#440000>Clear\nFilters</color>";
        public const string FilteredSongsPackName = "Filtered Songs";

        private static SongListUI _instance;

        public static SongListUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("EnhancedSearchAndFiltersUI").AddComponent<SongListUI>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public void OnMenuSceneLoadedFresh()
        {
            // get view controller which will contain our buttons
            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");
            ButtonParentViewController = viewControllersContainer.GetComponentInChildren<DismissableNavigationController>(true);

            _levelsTableViewContainer = viewControllersContainer.GetComponentInChildren<LevelPackLevelsTableView>(true);
            _levelsTableView = _levelsTableViewContainer.GetPrivateField<TableView>("_tableView");
            LevelsViewController = viewControllersContainer.GetComponentInChildren<LevelPackLevelsViewController>(true);

            var levelPacksViewController = viewControllersContainer.GetComponentInChildren<LevelPacksViewController>(true);
            levelPacksViewController.didSelectPackEvent += LevelPackSelected;
            
            Button soloFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "SoloFreePlayButton");
            Button partyFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PartyFreePlayButton");
            Button campaignButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "CampaignButton");

            soloFreePlayButton.onClick.AddListener(() => OnModeSelection(FreePlayMode.Solo));
            partyFreePlayButton.onClick.AddListener(() => OnModeSelection(FreePlayMode.Party));
            campaignButton.onClick.AddListener(() => OnModeSelection(FreePlayMode.Campaign));

            if (SongBrowserTweaks.ModLoaded)
            {
                // delay building UI until SongBrowser elements are built (after the user selects mode)
            }
            else if (BeatSaverDownloaderTweaks.ModLoaded)
            {
                StartCoroutine(GetBeatSaverDownloaderButtons());
            }
            else
            {
                CreateSearchButton(DefaultSearchButtonPosition, DefaultButtonSize);
                CreateFilterButton(DefaultFilterButtonPosition, DefaultButtonSize);
                CreateClearButton(DefaultClearButtonPosition, DefaultButtonSize);
                ToggleButtonsActive(false);
            }
        }

        private void OnModeSelection(FreePlayMode mode)
        {
            if (SongBrowserTweaks.ModLoaded && !SongBrowserTweaks.Initialized && mode != FreePlayMode.Campaign)
                StartCoroutine(GetSongBrowserButtons());

            if (mode == FreePlayMode.Solo)
            {
                _freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                ToggleButtonsActive(true);
                BeatSaverDownloaderTweaks.SetTopButtons(false);
            }
            else if (mode == FreePlayMode.Party)
            {
                _freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                ToggleButtonsActive(true);
                BeatSaverDownloaderTweaks.SetTopButtons(false);
            }
            else if (mode == FreePlayMode.Campaign)
            {
                _freePlayFlowCoordinator = FindObjectOfType<CampaignFlowCoordinator>();
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                ToggleButtonsActive(false);
                BeatSaverDownloaderTweaks.HideTopButtons();
            }

            SongBrowserTweaks.OnModeSelection();
        }

        IEnumerator GetSongBrowserButtons()
        {
            Logger.log.Info("SongBrowser mod found. Attempting to modify button behaviour.");

            int tries;
            for (tries = 10; tries > 0; --tries)
            {
                if (SongBrowserTweaks.Init())
                    break;

                yield return new WaitForSeconds(0.5f);
            }

            if (tries <= 0)
            {
                Logger.log.Warn("SongBrowser buttons were not found. Creating new buttons, which may overlap with other UI elements.");
                CreateSearchButton(DefaultSearchButtonPosition, DefaultButtonSize);
                CreateFilterButton(DefaultFilterButtonPosition, DefaultButtonSize);
                CreateClearButton(DefaultClearButtonPosition, DefaultButtonSize);
            }
        }

        IEnumerator GetBeatSaverDownloaderButtons()
        {
            Logger.log.Info("BeatSaverDownloader mod found. Attempting to replace button behaviour and positions.");

            int tries;
            Vector2 newButtonSize = new Vector2(20f, 6f);

            for (tries = 10; tries > 0; --tries)
            {
                if (BeatSaverDownloaderTweaks.Init(newButtonSize))
                {
                    CreateFilterButton(new Vector2(-12f, 36.75f), newButtonSize, "CreditsButton");
                    CreateClearButton(new Vector2(8f, 36.75f), newButtonSize, "CreditsButton");

                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (tries <= 0)
            {
                Logger.log.Warn("BeatSaverDownloader buttons were not found. Creating new buttons, which may overlap with other UI elements.");
                CreateSearchButton(DefaultSearchButtonPosition, DefaultButtonSize);
                CreateFilterButton(DefaultFilterButtonPosition, DefaultButtonSize);
                CreateClearButton(DefaultClearButtonPosition, DefaultButtonSize);
            }

            ToggleButtonsActive(false);
        }

        private void OnFreePlayFlowCoordinatorFinished(FlowCoordinator unused)
        {
            if (_freePlayFlowCoordinator is SoloFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is PartyFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is CampaignFlowCoordinator)
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;

            ToggleButtonsActive(false);
            BeatSaverDownloaderTweaks.HideTopButtons();

            // unapply filters before leaving the screen
            if (_filterViewController?.IsFilterApplied == true)
            {
                UnapplyFilters();

                SongBrowserTweaks.FiltersUnapplied();
            }

            _freePlayFlowCoordinator = null;
        }

        public void CreateSearchButton(Vector2 anchoredPosition, Vector2 sizeDelta, string buttonTemplate = "CancelButton")
        {
            if (SearchButton != null || ButtonParentViewController == null)
                return;

            SearchButton = ButtonParentViewController.CreateUIButton(buttonTemplate, anchoredPosition, sizeDelta, SearchButtonPressed, "Search");
            SearchButton.SetButtonTextSize(3f);
            SearchButton.ToggleWordWrapping(false);
            SearchButton.name = "EnhancedSearchButton";

            Logger.log.Debug("Created search button.");
        }

        public void CreateFilterButton(Vector2 anchoredPosition, Vector2 sizeDelta, string buttonTemplate = "CancelButton")
        {
            if (FilterButton != null || ButtonParentViewController == null)
                return;

            FilterButton = ButtonParentViewController.CreateUIButton(buttonTemplate, anchoredPosition, sizeDelta, FilterButtonPressed, FilterButtonText);
            FilterButton.SetButtonTextSize(3f);
            FilterButton.ToggleWordWrapping(false);
            FilterButton.name = "EnhancedFilterButton";

            // change colour of text
            (FilterButton as NoTransitionsButton).selectionStateDidChangeEvent += delegate (NoTransitionsButton.SelectionState selectionState)
            {
                var filterApplied = _filterViewController?.IsFilterApplied ?? false;
                var text = FilterButton.GetComponentInChildren<TextMeshProUGUI>();

                if (selectionState == NoTransitionsButton.SelectionState.Highlighted)
                    text.text = filterApplied ? FilterButtonHighlightedAppliedText : FilterButtonHighlightedText;
                else
                    text.text = filterApplied ? FilterButtonAppliedText : FilterButtonText;
            };

            Logger.log.Debug("Created filter button.");
        }

        public void CreateClearButton(Vector2 anchoredPosition, Vector2 sizeDelta, string buttonTemplate = "CancelButton")
        {
            if (ClearButton != null || ButtonParentViewController == null)
                return;

            ClearButton = ButtonParentViewController.CreateUIButton(buttonTemplate, anchoredPosition, sizeDelta, ClearButtonPressed, "Clear\nFilters");
            ClearButton.SetButtonTextSize(2.3f);
            ClearButton.ToggleWordWrapping(false);
            ClearButton.name = "EnhancedClearFilterButton";

            // change colour of text
            (ClearButton as NoTransitionsButton).selectionStateDidChangeEvent += delegate (NoTransitionsButton.SelectionState selectionState)
            {
                var filterApplied = _filterViewController?.IsFilterApplied ?? false;
                var text = ClearButton.GetComponentInChildren<TextMeshProUGUI>();

                if (selectionState == NoTransitionsButton.SelectionState.Highlighted)
                    text.text = filterApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText;
                else
                    text.text = filterApplied ? ClearFilterButtonAppliedText : ClearFilterButtonText;
            };

            Logger.log.Debug("Created clear filter button.");
        }

        /// <summary>
        /// Used by SongBrowserTweaks to apply an existing filter onto another set of beatmaps.
        /// </summary>
        public List<IPreviewBeatmapLevel> ApplyFiltersForSongBrowser(IPreviewBeatmapLevel[] levels)
        {
            return _filterViewController.ApplyFiltersForSongBrowser(levels);
        }

        /// <summary>
        /// Unapplies the filters in the FilterViewController, but saves their current status.
        /// </summary>
        /// <param name="songBrowserFilterSelected">Used only by the SongBrowser mod. Set this to true when another filter (Favorites/Playlist) was selected.</param>
        public void UnapplyFilters(bool songBrowserFilterSelected = false)
        {
            if (_filterViewController != null)
                _filterViewController.UnapplyFilters(false);

            if (!SongBrowserTweaks.ModLoaded)
            {
                FilterButton.SetButtonText(FilterButtonText);
                FilterButton.SetButtonTextSize(3f);

                if ((ClearButton as NoTransitionsButton).selectionState == NoTransitionsButton.SelectionState.Highlighted)
                    ClearButton.SetButtonText(ClearFilterButtonHighlightedText);
                else
                    ClearButton.SetButtonText(ClearFilterButtonText);
            }
            else if (SongBrowserTweaks.Initialized && !songBrowserFilterSelected)
            {
                LevelsViewController.SetData(_lastPack);
            }
        }

        public void SearchButtonPressed()
        {
            if (_searchFlowCoordinator == null)
            {
                _searchFlowCoordinator = new GameObject("EnhancedSearchFlowCoordinator").AddComponent<SearchFlowCoordinator>();
                _searchFlowCoordinator.BackButtonPressed += DismissSearchFlowCoordinator;
                _searchFlowCoordinator.SongSelected += SelectSongFromSearchResult;
            }

            // TODO?: toggle to search every level pack instead of just the current?
            IBeatmapLevelPack levelPack = LevelsViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
            _searchFlowCoordinator.Activate(_freePlayFlowCoordinator, levelPack);

            Logger.log.Debug("'Search' button pressed.");
        }

        public void FilterButtonPressed()
        {
            if (_filterViewController == null)
            {
                _filterViewController = new GameObject("FilterViewController").AddComponent<FilterViewController>();
                _filterViewController.BackButtonPressed += DismissFilterViewController;
                _filterViewController.LevelsModified += FilterViewControllerSetFilteredSongs;
                _filterViewController.FiltersUnapplied += FilterViewControllerFiltersUnapplied;
            }

            if (_lastPack == null || LevelsViewController.levelPack.packName != FilteredSongsPackName)
                _lastPack = LevelsViewController.levelPack;

            IPreviewBeatmapLevel[] levels = _lastPack.beatmapLevelCollection.beatmapLevels;

            _filterViewController.Activate(_freePlayFlowCoordinator, levels);

            Logger.log.Debug("'Filter' button pressed.");
        }

        public void ClearButtonPressed()
        {
            if (_filterViewController?.IsFilterApplied == true)
                _filterViewController?.UnapplyFilters();

            Logger.log.Debug("'Clear Filter' button pressed.");
        }

        public void ToggleButtonsActive(bool active)
        {
            if (SearchButton != null)
                SearchButton.gameObject.SetActive(active);
            if (FilterButton != null)
                FilterButton.gameObject.SetActive(active);
            if (ClearButton != null)
                ClearButton.gameObject.SetActive(active);

            if (!active)
                BeatSaverDownloaderTweaks.HideTopButtons();
        }

        private void LevelPackSelected(LevelPacksViewController viewController, IBeatmapLevelPack levelPack)
        {
            if (levelPack.packName != FilteredSongsPackName)
                _lastPack = levelPack;

            if (!SongBrowserTweaks.ModLoaded || !SongBrowserTweaks.Initialized)
            {
                // SongBrowser can now apply filters to OST levels and switch between different level packs
                // so our old behaviour of cancelling the filters is no longer needed
                // that being said, without SongBrowser, we are still going to cancel filters upon switching level packs
                // because i'd rather the player have to go into the FilterViewController,
                // so that it can check if all the beatmap details have been loaded
                Logger.log.Debug("Another level pack has been selected, unapplying filters");
                UnapplyFilters();
            }
        }

        private void DismissSearchFlowCoordinator()
        {
            _freePlayFlowCoordinator.InvokeMethod("DismissFlowCoordinator", _searchFlowCoordinator, null, false);
        }

        private void DismissFilterViewController()
        {
            _freePlayFlowCoordinator.InvokeMethod("DismissViewController", _filterViewController, null, false);
        }

        private void SelectSongFromSearchResult(IPreviewBeatmapLevel level)
        {
            Logger.log.Debug($"Level selected from search: {level.songName} {level.songSubName} - {level.songAuthorName}");
            DismissSearchFlowCoordinator();

            IPreviewBeatmapLevel[] levels = LevelsViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels;

            int row = Array.IndexOf(levels, level);
            if (row >= 0)
            {
                if (_levelsTableViewContainer.GetPrivateField<bool>("_showLevelPackHeader"))
                    ++row;

                _levelsTableView.ScrollToCellWithIdx(row, TableViewScroller.ScrollPositionType.Beginning, false);
                _levelsTableView.SelectCellWithIdx(row);
            }

            LevelsViewController.HandleLevelPackLevelsTableViewDidSelectLevel(null, level);
        }

        private void FilterViewControllerSetFilteredSongs(IPreviewBeatmapLevel[] levels)
        {
            // filter application should be handled by FilterViewController calling stuff in SongBrowserTweaks
            if (SongBrowserTweaks.Initialized)
                return;

            BeatmapLevelPack levelPack = new BeatmapLevelPack("", FilteredSongsPackName, "", LevelsViewController.levelPack.coverImage, new BeatmapLevelCollection(levels));
            LevelsViewController.SetData(levelPack);

            FilterButton.SetButtonText(FilterButtonAppliedText);
            FilterButton.SetButtonTextSize(2.3f);

            ClearButton.SetButtonText(ClearFilterButtonAppliedText);
        }

        private void FilterViewControllerFiltersUnapplied()
        {
            LevelsViewController.SetData(_lastPack);

            if (!SongBrowserTweaks.ModLoaded)
            {
                FilterButton.SetButtonText(FilterButtonText);
                FilterButton.SetButtonTextSize(3f);

                // if the clear button is shown, then that was pressed to clear filters
                // therefore, it should currently be highlighted
                if ((ClearButton as NoTransitionsButton).selectionState == NoTransitionsButton.SelectionState.Highlighted)
                    ClearButton.SetButtonText(ClearFilterButtonHighlightedText);
                else
                    ClearButton.SetButtonText(ClearFilterButtonText);
            }
            else
            {
                SongBrowserTweaks.FiltersUnapplied();
            }
        }
    }
}
