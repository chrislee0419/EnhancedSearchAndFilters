﻿using System;
using System.Collections.Generic;
using System.Linq;
using HMUI;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.UI.ViewControllers;
using EnhancedSearchAndFilters.Filters;

namespace EnhancedSearchAndFilters.UI.FlowCoordinators
{
    public class FilterFlowCoordinator : FlowCoordinator
    {
        public event Action BackButtonPressed;
        public event Action<IPreviewBeatmapLevel[]> FilterApplied;
        public event Action FiltersUnapplied;

        private FilterMainViewController _filterMainViewController;
        private FilterSideViewController _filterSideViewController;

        private IFilter _currentFilter;
        private IPreviewBeatmapLevel[] _unfilteredLevels;
        private Dictionary<IPreviewBeatmapLevel, BeatmapDetails> _beatmapDetails;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "Filter Songs";
                showBackButton = true;

                _filterMainViewController = BeatSaberUI.CreateViewController<FilterMainViewController>();
                _filterSideViewController = BeatSaberUI.CreateViewController<FilterSideViewController>();

                _filterMainViewController.ApplyButtonPressed += ApplyFilters;
                _filterMainViewController.UnapplyButtonPressed += () => UnapplyFilters();
                _filterMainViewController.ClearButtonPressed += ClearCurrentFilterChanges;
                _filterMainViewController.DefaultButtonPressed += SetCurrentFilterToDefault;
                _filterSideViewController.FilterSelected += FilterSelected;
                _filterSideViewController.ClearButtonPressed += ClearAllFilterChanges;
                _filterSideViewController.DefaultButtonPressed += SetAllFiltersToDefault;
                _filterSideViewController.QuickFilterApplied += ApplyQuickFilter;

                FilterList.FilterListChanged -= FilterListChanged;
                FilterList.FilterListChanged += FilterListChanged;

                ProvideInitialViewControllers(_filterMainViewController, _filterSideViewController);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatmapDetailsLoader.instance.StopLoading();

            // filter will be re-selected during the LoadBeatmaps on finish handler, which will re-install this delegate
            if (_currentFilter != null)
                _currentFilter.SettingChanged -= FilterSettingChanged;

            BackButtonPressed?.Invoke();
        }

        /// <summary>
        /// Presents this flow coordinator and sets the list of songs to filter. 
        /// This must be used instead of invoking the private PresentFlowCoordinator to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will present this flow coordinator.</param>
        /// <param name="levels">The list of levels that will be filtered.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IPreviewBeatmapLevel[] levels)
        {
            Action loadBeatmaps = LoadBeatmaps;

            _unfilteredLevels = levels;
            parentFlowCoordinator.InvokeMethod("PresentFlowCoordinator", new object[] { this, loadBeatmaps, false, false });
        }

        internal void RefreshUI()
        {
            bool anyAppliedNoChanges = FilterList.ActiveFilters.Any(x => x.Status == FilterStatus.Applied);
            bool anyChanged = FilterList.AnyChanged;
            bool allDefaults = FilterList.ActiveFilters.All(x => x.IsStagingDefaultValues);
            bool currentChanged = _currentFilter.HasChanges;
            bool currentDefault = _currentFilter.IsStagingDefaultValues;

            _filterMainViewController.SetButtonInteractivity(anyAppliedNoChanges || anyChanged, currentChanged, !currentDefault);
            _filterMainViewController.SetApplyUnapplyButton(!anyAppliedNoChanges || anyChanged);

            _filterSideViewController.ClearButtonInteractable = anyChanged;
            _filterSideViewController.DefaultButtonInteractable = !allDefaults;
            _filterSideViewController.RefreshFilterListCellContent();
        }

        // beatmaps have to be loaded after everything is created, since only then will the loading view exist
        private void LoadBeatmaps()
        {
            IPreviewBeatmapLevel[] levelsToLoad;

            if (PluginConfig.ShowFirstTimeLoadingText)
            {
                // first time loading should load all custom levels for cache
                var allCustomLevels = SongCore.Loader.CustomLevels.Values.Select(x => x as IPreviewBeatmapLevel).ToList();
                allCustomLevels.AddRange(_unfilteredLevels);
                levelsToLoad = allCustomLevels.Distinct().ToArray();
            }
            else
            {
                levelsToLoad = _unfilteredLevels;
            }

            _filterMainViewController.ShowLoadingView();

            BeatmapDetailsLoader.instance.StartLoading(levelsToLoad,
                delegate (int loaded)
                {
                    // on update, show updated progress text
                    _filterMainViewController.UpdateLoadingProgressText(loaded, levelsToLoad.Length);
                },
                delegate (BeatmapDetails[] levels)
                {
                    // on finish
                    _filterSideViewController.FilterListActive = true;
                    _filterSideViewController.QuickFilterSectionActive = true;

                    // NOTE: this quick filter check is done here, since the text size calculation cannot be done during DidActivate
                    // it also can't happen before FilterListActive is set to true, otherwise the first time this text is shown,
                    // it will be a tenth of the size of what it should be
                    _filterSideViewController.CheckSelectedQuickFilter();

                    if (_currentFilter != null)
                    {
                        FilterSelected(_currentFilter);
                        _filterSideViewController.SelectFilterCell(FilterList.ActiveFilters.IndexOf(_currentFilter));
                    }
                    else
                    {
                        FilterSelected(FilterList.ActiveFilters.First());
                        _filterSideViewController.SelectFilterCell(0);
                    }
                    RefreshUI();

                    _beatmapDetails = new Dictionary<IPreviewBeatmapLevel, BeatmapDetails>(_unfilteredLevels.Length);
                    foreach (var level in _unfilteredLevels)
                    {
                        // if a custom level's level ID contains the name of its folder, strip it out
                        var details = levels.FirstOrDefault(x => x?.LevelID == BeatmapDetailsLoader.GetSimplifiedLevelID(level));
                        if (details == null)
                            continue;

                        _beatmapDetails[level] = details;
                    }

                    PluginConfig.ShowFirstTimeLoadingText = false;
                });
        }

        private void FilterSettingChanged()
        {
            RefreshUI();
        }

        private void ApplyFilters()
        {
            if (BeatmapDetailsLoader.instance.IsLoading)
            {
                Logger.log.Warn("Tried to apply filters while loading beatmap details (this should never happen?)");
                return;
            }

            if (!Tweaks.SongBrowserTweaks.Initialized)
                Logger.log.Debug($"Applying filter, starting with {_beatmapDetails.Count} songs");

            List<BeatmapDetails> filteredLevels = null;

            bool hasApplied;
            if (!Tweaks.SongBrowserTweaks.Initialized)
            {
                filteredLevels = new List<BeatmapDetails>(_beatmapDetails.Values);
                hasApplied = FilterList.ApplyFilter(ref filteredLevels);
                Logger.log.Debug($"Filter completed, {filteredLevels.Count} songs left");
            }
            else
            {
                foreach (var filter in FilterList.ActiveFilters)
                    filter.ApplyStagingValues();
                hasApplied = FilterList.AnyApplied;
            }

            RefreshUI();

            if (hasApplied)
            {
                if (Tweaks.SongBrowserTweaks.ModLoaded && Tweaks.SongBrowserTweaks.Initialized)
                {
                    _filterMainViewController.ShowInfoText("Filter applied");
                }
                else
                {
                    _filterMainViewController.ShowInfoText($"{filteredLevels.Count} out of {_beatmapDetails.Count} songs found");

                    // SongBrowser will create its own BeatmapLevelPack when it gets our filtered levels via:
                    // ProcessSongList() -> CustomFilterHandler() -> ApplyFiltersForSongBrowser() -> ApplyFilters()
                    // filters are applied once this flow coordinator is dismissed
                    FilterApplied?.Invoke(_beatmapDetails.Where(x => filteredLevels.Contains(x.Value)).Select(x => x.Key).ToArray());
                }
            }
            else
            {
                // default values were applied (no filtering or undo filtering)
                FiltersUnapplied?.Invoke();
            }
        }

        /// <summary>
        /// Unapply filters and refresh the UI.
        /// </summary>
        /// <param name="sendEvent">Invoke the FiltersUnapplied event.</param>
        public void UnapplyFilters(bool sendEvent = true)
        {
            foreach (var filter in FilterList.ActiveFilters)
                filter.ApplyDefaultValues();

            RefreshUI();

            if (sendEvent)
                FiltersUnapplied?.Invoke();
        }

        private void ClearCurrentFilterChanges()
        {
            _currentFilter.SetAppliedValuesToStaging();
            RefreshUI();
        }

        private void SetCurrentFilterToDefault()
        {
            _currentFilter.SetDefaultValuesToStaging();
            RefreshUI();
        }

        private void ApplyQuickFilter(QuickFilter quickFilter)
        {
            FilterList.ApplyQuickFilter(quickFilter);
            ApplyFilters();
        }

        private void FilterSelected(IFilter selectedFilter)
        {
            if (_currentFilter != null)
                _currentFilter.SettingChanged -= FilterSettingChanged;

            _filterMainViewController.ShowFilterContentView(selectedFilter);
            selectedFilter.SettingChanged += FilterSettingChanged;
            _currentFilter = selectedFilter;

            RefreshUI();
        }

        private void ClearAllFilterChanges()
        {
            foreach (var filter in FilterList.ActiveFilters)
                filter.SetAppliedValuesToStaging();
            RefreshUI();
        }

        private void SetAllFiltersToDefault()
        {
            foreach (var filter in FilterList.ActiveFilters)
                filter.SetDefaultValuesToStaging();
            RefreshUI();
        }

        private void FilterListChanged()
        {
            _filterSideViewController.SetFilterList();

            // the filter list isn't intended to be changed when in the filter screen
            // so if the current filter has been removed, just set it to null and
            // let LoadBeatmaps deal with it
            if (_currentFilter != null && !FilterList.ActiveFilters.Contains(_currentFilter))
                _currentFilter = null;

            UnapplyFilters();
        }
    }
}
