using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using TableView = HMUI.TableView;
using TableViewScroller = HMUI.TableViewScroller;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.UI.FlowCoordinators;

namespace EnhancedSearchAndFilters.UI
{
    internal enum FreePlayMode
    {
        Solo,
        Party,
        Campaign
    }

    class SongListUI : PersistentSingleton<SongListUI>
    {
        private FlowCoordinator _freePlayFlowCoordinator;
        private SearchFlowCoordinator _searchFlowCoordinator;
        private FilterFlowCoordinator _filterFlowCoordinator;

        private LevelCollectionTableView _levelCollectionTableView;
        private TableView _levelsTableView;
        private IBeatmapLevelPack _lastPack;

        public LevelSelectionNavigationController LevelSelectionNavigationController { get; private set; } = null;
        public DismissableNavigationController ButtonParentViewController { get; private set; } = null;

        public const string FilteredSongsPackName = "Filtered Songs";

        public void OnMenuSceneLoadedFresh()
        {
            // get view controller which will contain our buttons
            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");
            ButtonParentViewController = viewControllersContainer.GetComponentInChildren<DismissableNavigationController>(true);

            _levelCollectionTableView = viewControllersContainer.GetComponentInChildren<LevelCollectionTableView>(true);
            _levelsTableView = _levelCollectionTableView.GetPrivateField<TableView>("_tableView");
            LevelSelectionNavigationController = viewControllersContainer.GetComponentInChildren<LevelSelectionNavigationController>(true);

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
            /* TODO: re-enable this when BeatSaverDownloader is available again (or whatever is gonna replace its UI)
            else if (BeatSaverDownloaderTweaks.ModLoaded)
            {
                StartCoroutine(GetBeatSaverDownloaderButtons());
            }
            */
            else
            {
                ButtonPanel.instance.Setup(PluginConfig.DisableSearch, PluginConfig.DisableFilter, true);
                ButtonPanel.instance.SearchButtonPressed += SearchButtonPressed;
                ButtonPanel.instance.FilterButtonPressed += FilterButtonPressed;
                ButtonPanel.instance.ClearFilterButtonPressed += ClearButtonPressed;
            }

            LevelSelectionNavigationController.didActivateEvent += (_, __) => ButtonPanel.instance.ShowPanel();
            LevelSelectionNavigationController.didDeactivateEvent += (_) => ButtonPanel.instance.HidePanel();
        }

        private void OnModeSelection(FreePlayMode mode)
        {
            if (SongBrowserTweaks.ModLoaded && !SongBrowserTweaks.Initialized && mode != FreePlayMode.Campaign)
                StartCoroutine(GetSongBrowserButtons());

            if (mode == FreePlayMode.Solo)
            {
                _freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                //ButtonPanel.instance.ShowPanel();
                //BeatSaverDownloaderTweaks.SetTopButtons(false);
            }
            else if (mode == FreePlayMode.Party)
            {
                _freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                //ButtonPanel.instance.ShowPanel();
                //BeatSaverDownloaderTweaks.SetTopButtons(false);
            }
            else if (mode == FreePlayMode.Campaign)
            {
                _freePlayFlowCoordinator = FindObjectOfType<CampaignFlowCoordinator>();
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                //ToggleButtonsActive(false);
                //BeatSaverDownloaderTweaks.HideTopButtons();
            }

            SongBrowserTweaks.OnModeSelection();
        }

        private IEnumerator GetSongBrowserButtons()
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
                Logger.log.Warn("SongBrowser buttons could not be found. Creating default buttons panel");
                ButtonPanel.instance.Setup(PluginConfig.DisableSearch, PluginConfig.DisableFilter);
            }
        }

        /*
        private IEnumerator GetBeatSaverDownloaderButtons()
        {
            Logger.log.Info("BeatSaverDownloader mod found. Attempting to replace button behaviour and positions.");

            int tries;
            Vector2 newButtonSize = new Vector2(20f, 6f);

            for (tries = 10; tries > 0; --tries)
            {
                if (BeatSaverDownloaderTweaks.Init(newButtonSize))
                {
                    if (!PluginConfig.DisableFilter)
                    {
                        CreateFilterButton(new Vector2(-12f, 36.75f), newButtonSize, "CreditsButton");
                        CreateClearButton(new Vector2(8f, 36.75f), newButtonSize, "CreditsButton");
                    }

                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            if (tries <= 0)
            {
                Logger.log.Warn("BeatSaverDownloader buttons could not be found. Creating default buttons panel");
                ButtonPanel.instance.Setup();
            }

            ToggleButtonsActive(false);
        }
        */

        private void OnFreePlayFlowCoordinatorFinished(FlowCoordinator unused)
        {
            if (_freePlayFlowCoordinator is SoloFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is PartyFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is CampaignFlowCoordinator)
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;

            //ButtonPanel.instance.HidePanel();
            //BeatSaverDownloaderTweaks.HideTopButtons();

            // unapply filters before leaving the screen
            if (_filterFlowCoordinator?.AreFiltersApplied == true)
            {
                UnapplyFilters();

                SongBrowserTweaks.FiltersUnapplied();
            }

            _freePlayFlowCoordinator = null;
        }

        /// <summary>
        /// Used by SongBrowserTweaks to apply an existing filter onto another set of beatmaps.
        /// </summary>
        public List<IPreviewBeatmapLevel> ApplyFiltersForSongBrowser(IPreviewBeatmapLevel[] levels)
        {
            return _filterFlowCoordinator.ApplyFiltersFromExternalViewController(levels);
        }

        /// <summary>
        /// Unapplies the filters in the FilterViewController, but saves their current status.
        /// </summary>
        /// <param name="songBrowserFilterSelected">Used only by the SongBrowser mod. Set this to true when another filter (Favorites/Playlist) was selected.</param>
        public void UnapplyFilters(bool songBrowserFilterSelected = false)
        {
            if (_filterFlowCoordinator != null)
                _filterFlowCoordinator.UnapplyFilters(false);

            ButtonPanel.instance.SetFilterStatus(false);

            if (SongBrowserTweaks.Initialized && !songBrowserFilterSelected)
                LevelSelectionNavigationController.SetData(_lastPack, true, true, true);
        }

        public void SearchButtonPressed()
        {
            if (_searchFlowCoordinator == null)
            {
                _searchFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<SearchFlowCoordinator>();
                _searchFlowCoordinator.name = "EnhancedSearchFlowCoordinator";

                _searchFlowCoordinator.BackButtonPressed += DismissSearchFlowCoordinator;
                _searchFlowCoordinator.SongSelected += SelectSongFromSearchResult;
            }

            // TODO?: toggle to search every level pack instead of just the current?
            IBeatmapLevelPack levelPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
            _searchFlowCoordinator.Activate(_freePlayFlowCoordinator, levelPack);

            Logger.log.Debug("'Search' button pressed.");
        }

        public void FilterButtonPressed()
        {
            if (_filterFlowCoordinator == null)
            {
                _filterFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<FilterFlowCoordinator>();
                _filterFlowCoordinator.name = "FilterFlowCoordinator";

                _filterFlowCoordinator.BackButtonPressed += DismissFilterFlowCoordinator;
                _filterFlowCoordinator.FilterApplied += FilterFlowCoordinatorSetFilteredSongs;
                _filterFlowCoordinator.FiltersUnapplied += FilterFlowCoordinatorFiltersUnapplied;
            }

            var levelPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
            if (_lastPack == null || levelPack.packName != FilteredSongsPackName)
            {
                _lastPack = levelPack;
                Logger.log.Debug($"Storing '{levelPack.packName}' level pack as last pack");
            }

            IPreviewBeatmapLevel[] levels = _lastPack.beatmapLevelCollection.beatmapLevels;

            _filterFlowCoordinator.Activate(_freePlayFlowCoordinator, levels);

            Logger.log.Debug("'Filter' button pressed.");
        }

        public void ClearButtonPressed()
        {
            if (_filterFlowCoordinator?.AreFiltersApplied == true)
                _filterFlowCoordinator?.UnapplyFilters();

            Logger.log.Debug("'Clear Filter' button pressed.");
        }

        public void ToggleButtonsActive(bool active)
        {
            /* TODO: revisit this when BeatSaverDownloader is updated, since only that mod uses this function now
            if (SearchButton != null)
                SearchButton.gameObject.SetActive(active);
            if (FilterButton != null)
                FilterButton.gameObject.SetActive(active);
            if (ClearButton != null)
                ClearButton.gameObject.SetActive(active);

            if (!active)
                BeatSaverDownloaderTweaks.HideTopButtons();
            */
        }

        private void LevelPackSelected(LevelPacksViewController viewController, IBeatmapLevelPack levelPack)
        {
            if (levelPack.packName != FilteredSongsPackName)
            {
                _lastPack = levelPack;
                Logger.log.Debug($"Storing '{levelPack.packName}' level pack as last pack");
            }

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
            ButtonPanel.instance.ShowPanel();
        }

        private void DismissFilterFlowCoordinator()
        {
            _freePlayFlowCoordinator.InvokeMethod("DismissFlowCoordinator", _filterFlowCoordinator, null, false);
            ButtonPanel.instance.ShowPanel();
        }

        private void SelectSongFromSearchResult(IPreviewBeatmapLevel level)
        {
            Logger.log.Debug($"Level selected from search: {level.songName} {level.songSubName} - {level.songAuthorName}");
            DismissSearchFlowCoordinator();

            IPreviewBeatmapLevel[] levels = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels;

            int row = Array.IndexOf(levels, level);
            if (row >= 0)
            {
                if (_levelCollectionTableView.GetPrivateField<bool>("_showLevelPackHeader"))
                    ++row;

                _levelsTableView.ScrollToCellWithIdx(row, TableViewScroller.ScrollPositionType.Beginning, false);
                _levelsTableView.SelectCellWithIdx(row);
            }

            LevelSelectionNavigationController.HandleLevelCollectionViewControllerDidSelectLevel(null, level);
        }

        private void FilterFlowCoordinatorSetFilteredSongs(IPreviewBeatmapLevel[] levels)
        {
            // filter application should be handled by FilterViewController calling stuff in SongBrowserTweaks
            if (SongBrowserTweaks.Initialized)
                return;

            BeatmapLevelPack levelPack = new BeatmapLevelPack("", FilteredSongsPackName, "", Sprite.Create(Texture2D.whiteTexture, Rect.zero, Vector2.zero), new BeatmapLevelCollection(levels));
            LevelSelectionNavigationController.SetData(levelPack, true, true, true);

            ButtonPanel.instance.SetFilterStatus(true);
        }

        private void FilterFlowCoordinatorFiltersUnapplied()
        {
            LevelSelectionNavigationController.SetData(_lastPack, true, true, true);

            if (SongBrowserTweaks.ModLoaded)
                SongBrowserTweaks.FiltersUnapplied();
            else
                ButtonPanel.instance.SetFilterStatus(false);
        }
    }
}
