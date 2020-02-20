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
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.UI.FlowCoordinators;
using EnhancedSearchAndFilters.SongData;

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
        private IAnnotatedBeatmapLevelCollection _lastPack;
        private IAnnotatedBeatmapLevelCollection _levelsToApply;

        public LevelSelectionNavigationController LevelSelectionNavigationController { get; private set; } = null;

        public const string FilteredSongsCollectionName = "EnhancedFilterFilteredSongs";
        public const string FilteredSongsPackName = "Filtered Songs";
        public const string SortedLevelPackIDSuffix = "EnhancedSearchAndFiltersSorted";

        public void OnMenuSceneLoadedFresh()
        {
            // get view controller which will contain our buttons
            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

            _levelCollectionTableView = viewControllersContainer.GetComponentInChildren<LevelCollectionTableView>(true);
            _levelsTableView = _levelCollectionTableView.GetPrivateField<TableView>("_tableView");
            LevelSelectionNavigationController = viewControllersContainer.GetComponentInChildren<LevelSelectionNavigationController>(true);

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
            else if (!PluginConfig.DisableSearch || !PluginConfig.DisableFilters)
            {
                Logger.log.Debug("Creating button panel");
                InitializeButtonPanel(true);
            }
            else
            {
                Logger.log.Debug("Disabling button panel");
                ButtonPanel.instance.DisablePanel();
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

                StartCoroutine(PrepareLevelPackSelectedEvent());
            }
            else if (mode == FreePlayMode.Party)
            {
                _freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                StartCoroutine(PrepareLevelPackSelectedEvent());
            }
            else if (mode == FreePlayMode.Campaign)
            {
                _freePlayFlowCoordinator = FindObjectOfType<CampaignFlowCoordinator>();
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;
            }

            SongBrowserTweaks.OnModeSelection();
        }

        private IEnumerator PrepareLevelPackSelectedEvent()
        {
            yield return null;

            for (int tries = 10; tries > 0; --tries)
            {
                try
                {
                    var levelFilteringNavigationController = FindObjectsOfType<LevelFilteringNavigationController>().First();
                    levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= LevelPackSelected;
                    levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += LevelPackSelected;
                    yield break;
                }
                catch { }

                yield return new WaitForSeconds(0.5f);
            }

            Logger.log.Warn("Unable to get PlaylistsViewController. Filters may not work as intended");
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
                InitializeButtonPanel();
            }
        }

        private void InitializeButtonPanel(bool forceReinit = false)
        {
            ButtonPanel.instance.Setup(forceReinit);

            ButtonPanel.instance.SearchButtonPressed -= SearchButtonPressed;
            ButtonPanel.instance.FilterButtonPressed -= FilterButtonPressed;
            ButtonPanel.instance.ClearFilterButtonPressed -= ClearButtonPressed;
            ButtonPanel.instance.SortButtonPressed -= SortButtonPressed;
            ButtonPanel.instance.ApplyQuickFilterPressed -= ApplyQuickFilterPressed;

            ButtonPanel.instance.SearchButtonPressed += SearchButtonPressed;
            ButtonPanel.instance.FilterButtonPressed += FilterButtonPressed;
            ButtonPanel.instance.ClearFilterButtonPressed += ClearButtonPressed;
            ButtonPanel.instance.SortButtonPressed += SortButtonPressed;
            ButtonPanel.instance.ApplyQuickFilterPressed += ApplyQuickFilterPressed;
        }

        private void OnFreePlayFlowCoordinatorFinished(FlowCoordinator unused)
        {
            if (_freePlayFlowCoordinator is SoloFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is PartyFreePlayFlowCoordinator)
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;
            else if (_freePlayFlowCoordinator is CampaignFlowCoordinator)
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent -= OnFreePlayFlowCoordinatorFinished;

            // unapply filters before leaving the screen
            if (FilterList.AnyApplied == true)
            {
                UnapplyFilters();

                SongBrowserTweaks.FiltersUnapplied();
            }

            _freePlayFlowCoordinator = null;
        }

        /// <summary>
        /// Unapplies the filters in the FilterViewController, but saves their current status.
        /// </summary>
        public void UnapplyFilters()
        {
            if (_filterFlowCoordinator != null)
                _filterFlowCoordinator.UnapplyFilters(false);

            ButtonPanel.instance.SetFilterStatus(false);
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

            IBeatmapLevelPack levelPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
            _searchFlowCoordinator.Activate(_freePlayFlowCoordinator, levelPack);

            if (!ButtonPanel.instance.Initialized)
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
            if (_lastPack == null || (levelPack.shortPackName != FilteredSongsCollectionName && !levelPack.packID.Contains(SortedLevelPackIDSuffix)))
            {
                _lastPack = levelPack;
                Logger.log.Debug($"Storing '{levelPack.packName}' (id = '{levelPack.packID}') level pack as last pack");
            }

            IPreviewBeatmapLevel[] levels = _lastPack.beatmapLevelCollection.beatmapLevels;

            _filterFlowCoordinator.Activate(_freePlayFlowCoordinator, levels);

            if (!ButtonPanel.instance.Initialized)
                Logger.log.Debug("'Filter' button pressed.");
        }

        public void ClearButtonPressed()
        {
            if (FilterList.AnyApplied == true)
                _filterFlowCoordinator?.UnapplyFilters();

            if (_levelsToApply != null)
            {
                LevelSelectionNavigationController.SetData(_levelsToApply,
                    true,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"),
                    null);

                _levelsToApply = null;
            }

            if (!ButtonPanel.instance.Initialized)
                Logger.log.Debug("'Clear Filter' button pressed.");
        }

        private void SortButtonPressed()
        {
            if (_lastPack == null)
                _lastPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");

            if (FilterList.AnyApplied)
            {
                FilterList.ApplyFilter(_lastPack.beatmapLevelCollection.beatmapLevels, out IEnumerable<IPreviewBeatmapLevel> filteredLevels, false);

                var filteredAndSortedLevels = new BeatmapLevelPack(
                    "",
                    FilteredSongsPackName,
                    FilteredSongsCollectionName,
                    Sprite.Create(Texture2D.whiteTexture, Rect.zero, Vector2.zero),
                    new BeatmapLevelCollection(SongSortModule.SortSongs(filteredLevels.ToArray())));

                LevelSelectionNavigationController.SetData(
                    filteredAndSortedLevels,
                    true,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
            }
            else
            {
                if (_lastPack != null && _lastPack is IBeatmapLevelPack beatmapLevelPack)
                {
                    LevelSelectionNavigationController.SetData(
                        CreateSortedBeatmapLevelPack(beatmapLevelPack),
                        true,
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
                }
                else
                {
                    var lastLevels = _levelCollectionTableView.GetPrivateField<IPreviewBeatmapLevel[]>("_previewBeatmapLevels");

                    if (lastLevels != null)
                    {
                        LevelSelectionNavigationController.SetData(
                            new BeatmapLevelCollection(SongSortModule.SortSongs(lastLevels)),
                            LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                            LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"),
                            null);
                    }
                    else
                    {
                        Logger.log.Warn("Unable to find songs to sort");
                    }

                }
            }
        }

        private void ApplyQuickFilterPressed(QuickFilter quickFilter)
        {
            FilterList.ApplyQuickFilter(quickFilter);

            IPreviewBeatmapLevel[] unfilteredLevels = null;
            if (_lastPack == null)
                _lastPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
            if (_lastPack == null)
                unfilteredLevels = _levelCollectionTableView.GetPrivateField<IPreviewBeatmapLevel[]>("_previewBeatmapLevels");
            else
                unfilteredLevels = _lastPack.beatmapLevelCollection.beatmapLevels;

            if (unfilteredLevels == null)
            {
                Logger.log.Warn("Unable to apply quick filter (could not find songs to filter)");
                return;
            }

            FilterList.ApplyFilter(unfilteredLevels, out IEnumerable<IPreviewBeatmapLevel> filteredLevels);

            var filteredAndSortedLevels = new BeatmapLevelPack(
                "",
                FilteredSongsPackName,
                FilteredSongsCollectionName,
                Sprite.Create(Texture2D.whiteTexture, Rect.zero, Vector2.zero),
                new BeatmapLevelCollection(SongSortModule.SortSongs(filteredLevels.ToArray())));

            LevelSelectionNavigationController.SetData(
                filteredAndSortedLevels,
                true,
                LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));

            ButtonPanel.instance.SetFilterStatus(true);
        }

        private BeatmapLevelPack CreateSortedBeatmapLevelPack(IBeatmapLevelPack levelPack)
        {
            return new BeatmapLevelPack(
                levelPack.packID + SortedLevelPackIDSuffix,
                levelPack.packName,
                levelPack.shortPackName,
                levelPack.coverImage,
                new BeatmapLevelCollection(SongSortModule.SortSongs(levelPack.beatmapLevelCollection.beatmapLevels)));
        }

        private void LevelPackSelected(LevelFilteringNavigationController navController, IAnnotatedBeatmapLevelCollection levelPack, GameObject noDataInfoPrefab, BeatmapCharacteristicSO preferredCharacteristic)
        {
            if (levelPack.collectionName != FilteredSongsCollectionName)
            {
                _lastPack = levelPack;

                SongSortModule.ResetSortMode();
                ButtonPanel.instance.UpdateSortButtons();

                if (levelPack is IBeatmapLevelPack beatmapLevelPack)
                    Logger.log.Debug($"Storing '{beatmapLevelPack.packName}' (id = '{beatmapLevelPack.packID}') level pack as last pack");
                else
                    Logger.log.Debug($"Storing '{levelPack.collectionName}' level collection as last pack");
            }

            if (!SongBrowserTweaks.ModLoaded || !SongBrowserTweaks.Initialized)
            {
                // SongBrowser can now apply filters to OST levels and switch between different level packs
                // so our old behaviour of cancelling the filters is no longer needed
                // that being said, without SongBrowser, we are still going to cancel filters upon switching level packs
                // because i'd rather the player have to go into the FilterViewController,
                // so that it can check if all the beatmap details have been loaded
                if (FilterList.AnyApplied)
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

            // instead of applying filters inside the filter flow coordinator, apply the filters when the flow coordinator is dismissed
            // that way, we don't get the unity complaining about the LevelSelectionNavigationController being not active
            if (SongBrowserTweaks.Initialized && FilterList.AnyApplied)
            {
                SongBrowserTweaks.ApplyFilters();
            }
            else if (_levelsToApply != null)
            {
                // NOTE: levels should already be sorted
                LevelSelectionNavigationController.SetData(
                    _levelsToApply,
                    true,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"),
                    null);
                _levelsToApply = null;
            }
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
            // filter application should be handled by FilterFlowCoordinator calling stuff in SongBrowserTweaks
            if (SongBrowserTweaks.Initialized)
                return;

            // the filter view controller is always provided a default-sorted array of levels,
            // so we apply sorting here
            _levelsToApply = new BeatmapLevelPack(
                "",
                FilteredSongsPackName,
                FilteredSongsCollectionName,
                Sprite.Create(Texture2D.whiteTexture, Rect.zero, Vector2.zero),
                new BeatmapLevelCollection(SongSortModule.SortSongs(levels)));

            ButtonPanel.instance.SetFilterStatus(true);
        }

        private void FilterFlowCoordinatorFiltersUnapplied()
        {
            if (SongBrowserTweaks.ModLoaded)
            {
                SongBrowserTweaks.FiltersUnapplied();
            }
            else
            {
                if (_lastPack is IBeatmapLevelPack)
                {
                    _levelsToApply = CreateSortedBeatmapLevelPack(_lastPack as IBeatmapLevelPack);
                }
                else
                {
                    SongSortModule.ResetSortMode();
                    ButtonPanel.instance.UpdateSortButtons();

                    _levelsToApply = _lastPack;

                    Logger.log.Warn("Unable to sort level pack while disabling filters. Resetting to default sort mode.");
                }

                ButtonPanel.instance.SetFilterStatus(false);
            }
        }
    }
}
