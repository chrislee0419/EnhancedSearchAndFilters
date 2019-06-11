using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using TMPro;
using VRUI;
using TableView = HMUI.TableView;
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
        private LevelPackLevelsViewController _levelsViewController;

        public DismissableNavigationController ButtonParentViewController { get; private set; } = null;

        public Button SearchButton { get; set; } = null;
        public Button FilterButton { get; set; } = null;
        public Button ClearButton { get; set; } = null;

        private static readonly Vector2 DefaultSearchButtonPosition = new Vector2(12f, 35.5f);
        private static readonly Vector2 DefaultFilterButtonPosition = new Vector2(30f, 35.5f);
        private static readonly Vector2 DefaultClearButtonPosition = new Vector2(48f, 35.5f);
        private static readonly Vector2 DefaultButtonSize = new Vector2(18f, 6f);

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
            _levelsViewController = viewControllersContainer.GetComponentInChildren<LevelPackLevelsViewController>(true);

            var levelPacksViewController = viewControllersContainer.GetComponentInChildren<LevelPacksViewController>(true);
            
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
            }
        }

        private void OnModeSelection(FreePlayMode mode)
        {
            if (SongBrowserTweaks.ModLoaded && !SongBrowserTweaks.Initialized)
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
        }

        IEnumerator GetSongBrowserButtons()
        {
            Logger.log.Info("SongBrowser mod found. Attempting to modify search and clear button behaviour.");

            int tries;
            for (tries = 10; tries > 0; --tries)
            {
                try
                {
                    // modify SongBrowser's Search button to create our FlowCoordinator
                    SearchButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "FilterSearchButton");
                    SearchButton.onClick = new Button.ButtonClickedEvent();
                    SearchButton.onClick.AddListener(SearchButtonPressed);

                    // modify SongBrowser's clear sort button to also clear filters
                    // TODO

                    CreateIconFilterButton();

                    Logger.log.Info("Modified SongBrowser's search and clear button");
                    break;
                }
                catch (InvalidOperationException) { }

                yield return new WaitForSeconds(0.1f);
            }

            if (tries <= 0)
            {
                Logger.log.Warn("SongBrowser Search button was not found. Creating Search button, which may overlap with other UI elements.");
                CreateSearchButton(DefaultSearchButtonPosition, DefaultButtonSize);
                CreateFilterButton(DefaultFilterButtonPosition, DefaultButtonSize);
                CreateClearButton(DefaultClearButtonPosition, DefaultButtonSize);
            }
        }

        IEnumerator GetBeatSaverDownloaderButtons()
        {
            Logger.log.Info("BeatSaverDownloader mod found. Attempting to replace button behaviour and positions.");

            int tries;
            Vector2 newButtonSize = new Vector2(22f, 6f);

            for (tries = 10; tries > 0; --tries)
            {
                if (BeatSaverDownloaderTweaks.Init(newButtonSize))
                {
                    CreateFilterButton(new Vector2(-30f, 36.5f), newButtonSize, "CreditsButton");
                    CreateClearButton(new Vector2(-8f, 36.5f), newButtonSize, "CreditsButton");

                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (tries <= 0)
            {
                Logger.log.Warn("BeatSaverDownloader Search button was not found. Creating Search button, which may overlap with other UI elements.");
                CreateSearchButton(DefaultSearchButtonPosition, DefaultButtonSize);
                CreateFilterButton(DefaultFilterButtonPosition, DefaultButtonSize);
                CreateClearButton(DefaultClearButtonPosition, DefaultButtonSize);
            }
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

        /// <summary>
        /// Alternative filter button intended to sit alongside the SongBrowser mod's filter buttons.
        /// </summary>
        public void CreateIconFilterButton()
        {
            if (FilterButton != null || ButtonParentViewController == null)
                return;

            // TODO: fix this up after SongBrowser is updated for v1.0.0, icon scale is scuffed

            // modify button design to fit with other SongBrowser icon buttons
            const float iconSize = 3.5f;
            const float iconScale = 1f;
            Vector2 buttonPos = new Vector2(53.625f, 34.75f);
            Vector2 buttonSize = new Vector2(5f, 5f);

            FilterButton = ButtonParentViewController.CreateUIButton("PracticeButton", buttonPos, buttonSize, FilterButtonPressed);
            Destroy(FilterButton.GetComponentInChildren<TextMeshProUGUI>(true));

            Sprite filterIcon = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");
            Image icon = FilterButton.GetComponentsInChildren<Image>(true).First(x => x.name == "Icon");

            HorizontalLayoutGroup hgroup = icon.rectTransform.parent.GetComponent<HorizontalLayoutGroup>();
            hgroup.padding = new RectOffset(1, 1, 0, 0);

            icon.sprite = filterIcon;
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.rectTransform.localScale = new Vector2(iconScale, iconScale);

            Logger.log.Info($"Filter Sprite info: texw={filterIcon.texture.width}, texh={filterIcon.texture.height}, ppu={filterIcon.pixelsPerUnit}, pivot={filterIcon.pivot.ToString()}");
        }

        public void CreateFilterButton(Vector2 anchoredPosition, Vector2 sizeDelta, string buttonTemplate = "CancelButton")
        {
            if (FilterButton != null || ButtonParentViewController == null)
                return;

            FilterButton = ButtonParentViewController.CreateUIButton(buttonTemplate, anchoredPosition, sizeDelta, FilterButtonPressed, "Filter");
            FilterButton.SetButtonTextSize(3f);
            FilterButton.ToggleWordWrapping(false);
            FilterButton.name = "EnhancedFilterButton";

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

            Logger.log.Debug("Created clear filter button.");
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
            IPreviewBeatmapLevel[] levels = _levelsViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels;
            _searchFlowCoordinator.Activate(_freePlayFlowCoordinator, levels);

            Logger.log.Debug("'Search' button pressed.");
        }

        public void FilterButtonPressed()
        {
            if (_filterViewController == null)
            {
                _filterViewController = new GameObject("FilterViewController").AddComponent<FilterViewController>();
                _filterViewController.BackButtonPressed += DismissFilterViewController;
            }

            IPreviewBeatmapLevel[] levels = _levelsViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels;
            _filterViewController.Activate(_freePlayFlowCoordinator, levels);

            Logger.log.Debug("'Filter' button pressed.");
        }

        public void ClearButtonPressed()
        {
            Logger.log.Info("'Clear Filter' button pressed.");
        }

        public void ToggleButtonsActive(bool active)
        {
            SearchButton.gameObject.SetActive(active);
            FilterButton.gameObject.SetActive(active);
            ClearButton.gameObject.SetActive(active);
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
            Logger.log.Debug($"Level selected from search: {level.songName} - {level.songSubName}");
            DismissSearchFlowCoordinator();

            IPreviewBeatmapLevel[] levels = _levelsViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels;

            int row = Array.IndexOf(levels, level);
            if (row >= 0)
            {
                if (_levelsTableViewContainer.GetPrivateField<bool>("_showLevelPackHeader"))
                    ++row;

                _levelsTableView.ScrollToCellWithIdx(row, TableView.ScrollPositionType.Beginning, false);
                _levelsTableView.SelectCellWithIdx(row);
            }

            _levelsViewController.HandleLevelPackLevelsTableViewDidSelectLevel(null, level);
        }
    }
}
