using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using BS_Utils.Utilities;
using SongCore;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.UI.FlowCoordinators;
using EnhancedSearchAndFilters.SongData;
using UIUtilities = EnhancedSearchAndFilters.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.UI
{
    internal enum FreePlayMode
    {
        Solo,
        Party,
        Campaign
    }

    internal class SongListUI : PersistentSingleton<SongListUI>
    {
        private FlowCoordinator _freePlayFlowCoordinator;
        private SearchFlowCoordinator _searchFlowCoordinator;
        private FilterFlowCoordinator _filterFlowCoordinator;

        private SongListUIAdditions _uiAdditions;

        private LevelCollectionTableView _levelCollectionTableView;

        private IAnnotatedBeatmapLevelCollection _lastPack;
        private IAnnotatedBeatmapLevelCollection _levelsToApply;
        private FilteredLevelsLevelPack _filteredLevelPack = new FilteredLevelsLevelPack();
        private SortedLevelsLevelPack _sortedLevelsLevelPack = new SortedLevelsLevelPack();

        private bool _isSelectingInitialLevelPack = false;
        private bool _isDeletingSongInModOwnedLevelPack = false;

        public LevelSelectionNavigationController LevelSelectionNavigationController { get; private set; } = null;
        public LevelFilteringNavigationController LevelFilteringNavigationController { get; private set; } = null;

        public void OnMenuSceneLoadedFresh()
        {
            // get view controller which will contain our buttons
            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

            _levelCollectionTableView = viewControllersContainer.GetComponentInChildren<LevelCollectionTableView>(true);
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
            if (mode == FreePlayMode.Solo)
            {
                _freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                if (!SongBrowserTweaks.ModLoaded)
                {
                    PrepareLevelPackSelectedEvent();
                    StartCoroutine(UIUtilities.DelayedAction(SelectSavedLevelPack));
                }
                else if (!SongBrowserTweaks.Initialized)
                {
                    StartCoroutine(GetSongBrowserButtons());
                }
            }
            else if (mode == FreePlayMode.Party)
            {
                _freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (_freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;

                if (!SongBrowserTweaks.ModLoaded)
                {
                    PrepareLevelPackSelectedEvent();
                    StartCoroutine(UIUtilities.DelayedAction(SelectSavedLevelPack));
                }
                else if (!SongBrowserTweaks.Initialized)
                {
                    StartCoroutine(GetSongBrowserButtons());
                }
            }
            else if (mode == FreePlayMode.Campaign)
            {
                _freePlayFlowCoordinator = FindObjectOfType<CampaignFlowCoordinator>();
                (_freePlayFlowCoordinator as CampaignFlowCoordinator).didFinishEvent += OnFreePlayFlowCoordinatorFinished;
            }

            SongBrowserTweaks.OnModeSelection();
        }

        private void PrepareLevelPackSelectedEvent()
        {
            // we don't need the LevelPackSelected event if using SongBrowser, since it passes the songs it needs filtered
            // directly to us (so we don't need to store last pack at all)
            _isSelectingInitialLevelPack = true;
            LevelFilteringNavigationController = _freePlayFlowCoordinator.GetPrivateField<LevelFilteringNavigationController>("_levelFilteringNavigationController", typeof(LevelSelectionFlowCoordinator));
            LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= LevelPackSelected;
            LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += LevelPackSelected;
        }

        private void SelectSavedLevelPack()
        {
            string lastLevelPackString = PluginConfig.LastLevelPackID;
            int separatorPos = lastLevelPackString.IndexOf(PluginConfig.LastLevelPackIDSeparator);
            if (separatorPos < 0 || separatorPos + PluginConfig.LastLevelPackIDSeparator.Length >= lastLevelPackString.Length)
                goto OnError;

            string lastLevelPackCollectionTitle = lastLevelPackString.Substring(0, separatorPos);
            string lastLevelPackID = lastLevelPackString.Substring(separatorPos + PluginConfig.LastLevelPackIDSeparator.Length);

            TabBarViewController tabBarVC = LevelFilteringNavigationController.GetPrivateField<TabBarViewController>("_tabBarViewController");
            TabBarViewController.TabBarItem[] tabBarItems = tabBarVC.GetPrivateField<TabBarViewController.TabBarItem[]>("_items");
            var item = tabBarItems.FirstOrDefault(x => x.title == lastLevelPackCollectionTitle);
            if (item == null)
                goto OnError;

            int itemIndex = Array.IndexOf(tabBarItems, item);
            if (itemIndex < 0)
                goto OnError;

            var tabBarDatas = LevelFilteringNavigationController.GetPrivateField<object[]>("_tabBarDatas");
            if (itemIndex >= tabBarDatas.Length)
                goto OnError;

            var levelPacks = tabBarDatas[itemIndex].GetField<IAnnotatedBeatmapLevelCollection[]>("annotatedBeatmapLevelCollections");
            IAnnotatedBeatmapLevelCollection levelPack = levelPacks.FirstOrDefault(x => x.collectionName == lastLevelPackID || (x is IBeatmapLevelPack && ((IBeatmapLevelPack)x).packID == lastLevelPackID));
            if (levelPack == null)
                goto OnError;

            // this should trigger the LevelPackSelected() delegate and sort the level pack as well
            LevelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(levelPack);
            return;

        OnError:
            SongSortModule.ResetSortMode();
            ButtonPanel.instance.UpdateSortButtons();
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
            _uiAdditions = LevelSelectionNavigationController.gameObject.AddComponent<SongListUIAdditions>();

            ButtonPanel.instance.SearchButtonPressed -= SearchButtonPressed;
            ButtonPanel.instance.FilterButtonPressed -= FilterButtonPressed;
            ButtonPanel.instance.ClearFilterButtonPressed -= ClearButtonPressed;
            ButtonPanel.instance.SortButtonPressed -= SortButtonPressed;
            ButtonPanel.instance.ApplyQuickFilterPressed -= ApplyQuickFilterPressed;
            ButtonPanel.instance.ReportButtonPressed -= ReportButtonPressed;

            ButtonPanel.instance.SearchButtonPressed += SearchButtonPressed;
            ButtonPanel.instance.FilterButtonPressed += FilterButtonPressed;
            ButtonPanel.instance.ClearFilterButtonPressed += ClearButtonPressed;
            ButtonPanel.instance.SortButtonPressed += SortButtonPressed;
            ButtonPanel.instance.ApplyQuickFilterPressed += ApplyQuickFilterPressed;
            ButtonPanel.instance.ReportButtonPressed += ReportButtonPressed;

            _uiAdditions.ConfirmDeleteButtonPressed += ConfirmDeleteButtonPressed;
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

            if (!ButtonPanel.IsSingletonAvailable || !ButtonPanel.instance.Initialized)
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

            if (_lastPack == null)
            {
                var levelPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
                if (levelPack != null && levelPack.packID != FilteredLevelsLevelPack.PackID && !levelPack.packID.Contains(SortedLevelsLevelPack.PackIDSuffix))
                {
                    _lastPack = levelPack;
                    Logger.log.Debug($"Storing '{levelPack.packName}' (id = '{levelPack.packID}') level pack as last pack");
                }
                else
                {
                    var levelCollectionsViewController = Resources.FindObjectsOfTypeAll<AnnotatedBeatmapLevelCollectionsViewController>().FirstOrDefault();
                    if (levelCollectionsViewController != null)
                        _lastPack = levelCollectionsViewController.selectedAnnotatedBeatmapLevelCollection;

                    if (_lastPack == null)
                    {
                        Logger.log.Error("Unable to find currently selected level pack for filtering. Will not display FilterFlowCoordinator");
                        return;
                    }
                    else
                    {
                        Logger.log.Debug($"Storing '{_lastPack.collectionName}' level collection as last pack");
                    }
                }
            }

            IPreviewBeatmapLevel[] levels = _lastPack.beatmapLevelCollection.beatmapLevels;

            _filterFlowCoordinator.Activate(_freePlayFlowCoordinator, levels);

            if (!ButtonPanel.IsSingletonAvailable || !ButtonPanel.instance.Initialized)
                Logger.log.Debug("'Filter' button pressed.");
        }

        public void ClearButtonPressed()
        {
            if (FilterList.AnyApplied)
            {
                if (_filterFlowCoordinator != null)
                {
                    _filterFlowCoordinator.UnapplyFilters(false);
                }
                else
                {
                    foreach (var filter in FilterList.ActiveFilters)
                        filter.ApplyDefaultValues();
                }
            }
            else
            {
                return;
            }

            if (SongBrowserTweaks.Initialized)
            {
                Logger.log.Debug("'Clear Filter' button pressed.");
                return;
            }

            if (_lastPack == null)
            {
                var levelCollectionsViewController = Resources.FindObjectsOfTypeAll<AnnotatedBeatmapLevelCollectionsViewController>().FirstOrDefault();

                if (levelCollectionsViewController != null)
                    _lastPack = levelCollectionsViewController.selectedAnnotatedBeatmapLevelCollection;
            }

            if (_lastPack != null)
            {
                if (_lastPack is IBeatmapLevelPack levelPack)
                {
                    LevelSelectionNavigationController.SetData(_sortedLevelsLevelPack.SetupFromLevelPack(levelPack),
                        true,
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
                }
                else if (!SongSortModule.IsDefaultSort)
                {
                    LevelSelectionNavigationController.SetData(_sortedLevelsLevelPack.SetupFromLevels(_lastPack.beatmapLevelCollection.beatmapLevels),
                        true,
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
                }
                else
                {
                    LevelSelectionNavigationController.SetData(_lastPack,
                        true,
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"),
                        null);
                }
            }
            else
            {
                Logger.log.Warn("Unable to find the last level pack");
            }

            if (ButtonPanel.IsSingletonAvailable && ButtonPanel.instance.Initialized)
                ButtonPanel.instance.SetFilterStatus(false);
        }

        private void SortButtonPressed()
        {
            if (_lastPack == null)
                _lastPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");

            if (FilterList.AnyApplied)
            {
                _filteredLevelPack.SetupFromUnfilteredLevels(_lastPack.beatmapLevelCollection.beatmapLevels, _lastPack.coverImage, false);
                LevelSelectionNavigationController.SetData(_filteredLevelPack,
                    true,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
            }
            else
            {
                if (_lastPack != null && _lastPack is IBeatmapLevelPack beatmapLevelPack)
                {
                    LevelSelectionNavigationController.SetData(
                        _sortedLevelsLevelPack.SetupFromLevelPack(beatmapLevelPack),
                        true,
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                        LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
                }
                else
                {
                    var lastLevels = _levelCollectionTableView.GetPrivateField<IPreviewBeatmapLevel[]>("_previewBeatmapLevels");

                    if (lastLevels != null)
                    {
                        // if using default sort on a playlist, just show it as a playlist instead of creating a new level pack
                        if (SongSortModule.IsDefaultSort)
                        {
                            LevelSelectionNavigationController.SetData(
                                new BeatmapLevelCollection(lastLevels),
                                LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                                LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"),
                                null);
                        }
                        else
                        {
                            LevelSelectionNavigationController.SetData(
                                _sortedLevelsLevelPack.SetupFromLevels(lastLevels),
                                true,
                                LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                                LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));
                        }
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

            _filteredLevelPack.SetupFromUnfilteredLevels(unfilteredLevels);
            LevelSelectionNavigationController.SetData(
                _filteredLevelPack,
                true,
                LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"));

            ButtonPanel.instance.SetFilterStatus(true);
        }

        private void ReportButtonPressed()
        {
            _uiAdditions.ShowBugReportModal();
        }

        private void ConfirmDeleteButtonPressed(CustomBeatmapLevel level)
        {
            // scrolling back to the previous position is done by SongListUIAdditions
            // just need to deal with setting up the current pack here
            var currentPack = LevelSelectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack", typeof(LevelSelectionNavigationController));

            // if the current list of levels does not belong to a level pack, just provide the same levels minus the deleted song
            if (currentPack == null)
            {
                IPreviewBeatmapLevel[] levels = _levelCollectionTableView.GetPrivateField<IPreviewBeatmapLevel[]>("_previewBeatmapLevels", typeof(LevelCollectionTableView));
                BeatmapLevelCollection replacementLevels = new BeatmapLevelCollection(levels.Where(x => x.levelID != level.levelID).ToArray());

                Loader.Instance.DeleteSong(level.customLevelPath);

                LevelSelectionNavigationController.SetData(
                    replacementLevels,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView", typeof(LevelSelectionNavigationController)),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView", typeof(LevelSelectionNavigationController)),
                    null);
            }
            // check if the current level pack is this mod's filtered/sorted level pack
            // if it is, just remove the song from the level pack and show it again
            else if (currentPack.packID == FilteredLevelsLevelPack.PackID || currentPack.packID.Contains(SortedLevelsLevelPack.PackIDSuffix))
            {
                // remove the song from the pack
                var replacementPack = new BeatmapLevelPack(
                    currentPack.packID,
                    currentPack.packName,
                    currentPack.shortPackName,
                    currentPack.coverImage,
                    new BeatmapLevelCollection(currentPack.beatmapLevelCollection.beatmapLevels.Where(x => x.levelID != level.levelID).ToArray()));

                try
                {
                    _isDeletingSongInModOwnedLevelPack = true;
                    Loader.Instance.DeleteSong(level.customLevelPath);
                }
                finally
                {
                    _isDeletingSongInModOwnedLevelPack = false;
                }

                LevelSelectionNavigationController.SetData(
                    replacementPack,
                    true,
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView", typeof(LevelSelectionNavigationController)),
                    LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView", typeof(LevelSelectionNavigationController)));
            }
            // if the current level pack is not from this mod, just delete
            // SongCore should automatically reload the pack
            else
            {
                Loader.Instance.DeleteSong(level.customLevelPath);
            }
        }

        private void LevelPackSelected(LevelFilteringNavigationController navController, IAnnotatedBeatmapLevelCollection levelPack, GameObject noDataInfoPrefab, BeatmapCharacteristicSO preferredCharacteristic)
        {
            // ignore the first select event that's fired immediately after the user select the free play mode
            // this is done so we can select the saved last pack later
            // when the saved pack is selected, it will then call this function again for sorting/storing
            if (_isSelectingInitialLevelPack)
            {
                _lastPack = levelPack;
                _isSelectingInitialLevelPack = false;
                return;
            }

            // in ConfirmDeleteButtonClicked, the call to SongCore.Loader.Instance.DeleteSong will reload the level packs
            // which causes the custom level pack to be re-selected. but, if filters are applied or level pack is sorted,
            // we want to reshow our own filtered/sorted level pack and not reset our UI, so we don't have to handle this event
            // this code is kinda smelly tbh, but can't do anything about it unless there are changes to SongCore
            if (_isDeletingSongInModOwnedLevelPack)
                return;

            if (levelPack.collectionName != FilteredLevelsLevelPack.CollectionName)
            {
                _lastPack = levelPack;

                // store level pack to PluginConfig
                var tabBarVC = LevelFilteringNavigationController.GetPrivateField<TabBarViewController>("_tabBarViewController");
                var tabBarItems = tabBarVC.GetPrivateField<TabBarViewController.TabBarItem[]>("_items");

                string lastLevelPackString = tabBarItems[tabBarVC.selectedCellNumber].title + PluginConfig.LastLevelPackIDSeparator;
                if (levelPack is IBeatmapLevelPack beatmapLevelPack)
                {
                    lastLevelPackString += beatmapLevelPack.packID;
                    Logger.log.Debug($"Storing '{beatmapLevelPack.packName}' (id = '{beatmapLevelPack.packID}') level pack as last pack");
                }
                else
                {
                    lastLevelPackString += levelPack.collectionName;
                    Logger.log.Debug($"Storing '{levelPack.collectionName}' level collection as last pack");
                }
                PluginConfig.LastLevelPackID = lastLevelPackString;

                // reapply sort mode
                if (!SongSortModule.IsDefaultSort)
                {
                    if (levelPack is IBeatmapLevelPack beatmapLevelPack2)
                        _sortedLevelsLevelPack.SetupFromLevelPack(beatmapLevelPack2);
                    else
                        _sortedLevelsLevelPack.SetupFromLevels(levelPack.beatmapLevelCollection.beatmapLevels);

                    // since the level selection navigation controller shows a level pack using the same event that calls this function
                    // and it technically isn't a guarantee that this function will run after it is set,
                    // delay setting our level pack
                    StartCoroutine(UIUtilities.DelayedAction(() =>
                        LevelSelectionNavigationController.SetData(
                            _sortedLevelsLevelPack,
                            true,
                            LevelSelectionNavigationController.GetPrivateField<bool>("_showPlayerStatsInDetailView"),
                            LevelSelectionNavigationController.GetPrivateField<bool>("_showPracticeButtonInDetailView"))));
                }
            }

            // SongBrowser can now apply filters to OST levels and switch between different level packs
            // so our old behaviour of cancelling the filters is no longer needed
            // that being said, without SongBrowser, we are still going to cancel filters upon switching level packs
            // because i'd rather the player have to go into the FilterViewController,
            // so that it can check if all the beatmap details have been loaded
            if (FilterList.AnyApplied)
                Logger.log.Debug("Another level pack has been selected, unapplying filters");
            UnapplyFilters();

            if (_uiAdditions != null)
                _uiAdditions.RefreshPageButtons();
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

                if (_uiAdditions != null)
                    _uiAdditions.RefreshPageButtons();
            }
        }

        private void SelectSongFromSearchResult(IPreviewBeatmapLevel level)
        {
            Logger.log.Debug($"Level selected from search: {level.songName} {level.songSubName} - {level.songAuthorName}");
            DismissSearchFlowCoordinator();

            var levelCollectionViewController = LevelSelectionNavigationController.GetPrivateField<LevelCollectionViewController>("_levelCollectionViewController", typeof(LevelSelectionNavigationController));
            levelCollectionViewController.SelectLevel(level);
        }

        private void FilterFlowCoordinatorSetFilteredSongs(IPreviewBeatmapLevel[] levels)
        {
            // filter application should be handled by FilterFlowCoordinator calling stuff in SongBrowserTweaks
            if (SongBrowserTweaks.Initialized)
                return;

            // the filter view controller is always provided a default-sorted array of levels,
            // so we apply sorting here
            _filteredLevelPack.SetupFromPrefilteredLevels(levels);
            _levelsToApply = _filteredLevelPack;

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
                if (_lastPack is IBeatmapLevelPack levelPack)
                    _levelsToApply = _sortedLevelsLevelPack.SetupFromLevelPack(levelPack);
                else if (_lastPack != null)
                    _levelsToApply = _sortedLevelsLevelPack.SetupFromLevels(_lastPack.beatmapLevelCollection.beatmapLevels);
                else        // this should never happen (_lastPack should always be defined by this point)
                    Logger.log.Warn("Unable to unapply filters (could not find previous level pack)");

                ButtonPanel.instance.SetFilterStatus(false);
            }
        }
    }
}
