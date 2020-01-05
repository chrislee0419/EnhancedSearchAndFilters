using System;
using System.Collections;
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

        public bool AreFiltersApplied { get; private set; } = false;


        private FilterMainViewController _filterMainViewController;
        private FilterSideViewController _filterSideViewController;

        private List<IFilter> _filterList = new List<IFilter>();
        private IFilter _currentFilter;

        private IPreviewBeatmapLevel[] _unfilteredLevels;
        private Dictionary<BeatmapDetails, IPreviewBeatmapLevel> _beatmapDetails;

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

                // add filters to filter list
                _filterList.Add(new DifficultyFilter());
                _filterList.Add(new DurationFilter());
                _filterList.Add(new NJSFilter());
                _filterList.Add(new PPFilter());
                _filterList.Add(new StarDifficultyFilter());
                _filterList.Add(new PlayerStatsFilter());
                _filterList.Add(new OtherFilter());

                _filterSideViewController.SetFilterList(_filterList);

                ProvideInitialViewControllers(_filterMainViewController, _filterSideViewController);
            }
            else
            {
                _filterMainViewController.ShowLoadingView();
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatmapDetailsLoader.instance.CancelLoading();

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

        private void RefreshUI()
        {
            bool anyAppliedNoChanges = _filterList.Any(x => x.Status == FilterStatus.Applied);
            bool anyChanged = _filterList.Any(x => x.HasChanges);
            bool allDefaults = _filterList.All(x => x.IsStagingDefaultValues);
            bool currentChanged = _currentFilter.HasChanges;
            bool currentDefault = _currentFilter.IsStagingDefaultValues;

            _filterMainViewController.SetButtonInteractivity(anyAppliedNoChanges || anyChanged, currentChanged, !currentDefault);
            _filterMainViewController.SetApplyUnapplyButton(!anyAppliedNoChanges);
            _filterSideViewController.SetButtonInteractivity(anyChanged, !allDefaults);
            _filterSideViewController.RefreshFilterList();
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
                levelsToLoad = allCustomLevels.ToArray();
            }
            else
            {
                levelsToLoad = _unfilteredLevels;
            }

            BeatmapDetailsLoader.instance.LoadBeatmaps(levelsToLoad,
                delegate (int loaded)
                {
                    // on update, show updated progress text
                    _filterMainViewController.UpdateLoadingProgressText(loaded, levelsToLoad.Length);
                },
                delegate (BeatmapDetails[] levels)
                {
                    // on finish
                    if (_currentFilter != null)
                    {
                        FilterSelected(_currentFilter);
                        _filterSideViewController.SelectCell(_filterList.IndexOf(_currentFilter));
                    }
                    else
                    {
                        FilterSelected(_filterList.First());
                        _filterSideViewController.SelectCell(0);
                    }
                    RefreshUI();

                    _beatmapDetails = new Dictionary<BeatmapDetails, IPreviewBeatmapLevel>(_unfilteredLevels.Length);
                    foreach (var level in _unfilteredLevels)
                    {
                        // if a custom level's level ID contains the name of its folder, strip it out
                        var details = levels.FirstOrDefault(x => x?.LevelID == BeatmapDetailsLoader.GetSimplifiedLevelID(level));
                        if (details == null)
                            continue;

                        _beatmapDetails[details] = level;
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

            List<BeatmapDetails> filteredLevels = new List<BeatmapDetails>(_beatmapDetails.Keys);

            bool hasApplied = false;
            foreach (var filter in _filterList)
            {
                filter.ApplyStagingValues();

                if (filter.Status == FilterStatus.Applied)
                {
                    // if SongBrowser is loaded, we will apply the filter after the call to SongBrowserUI:ProcessSongList()
                    if (!Tweaks.SongBrowserTweaks.Initialized)
                        filter.FilterSongList(ref filteredLevels);
                    hasApplied = true;
                }
            }

            if (!Tweaks.SongBrowserTweaks.Initialized)
                Logger.log.Debug($"Filter completed, {filteredLevels.Count} songs left");

            if (hasApplied)
            {
                RefreshUI();

                AreFiltersApplied = true;

                if (Tweaks.SongBrowserTweaks.ModLoaded && Tweaks.SongBrowserTweaks.Initialized)
                    _filterMainViewController.ShowInfoText("Filter applied");
                else
                    _filterMainViewController.ShowInfoText($"{filteredLevels.Count} out of {_beatmapDetails.Count} songs found");

                // SongBrowser will create its own BeatmapLevelPack when it gets our filtered levels via:
                // ProcessSongList() -> CustomFilterHandler() -> ApplyFiltersForSongBrowser() -> ApplyFilters()
                if (!Tweaks.SongBrowserTweaks.FiltersApplied())
                    FilterApplied?.Invoke(_beatmapDetails.Where(x => filteredLevels.Contains(x.Key)).Select(x => x.Value).ToArray());
            }
            else
            {
                // default values were applied (no filtering or undo filtering)
                RefreshUI();

                AreFiltersApplied = false;

                FiltersUnapplied?.Invoke();
            }
        }

        /// <summary>
        /// Filter application logic intended for use when filters are applied outside of this flow coordinator. 
        /// Mainly used by SongBrowser.
        /// </summary>
        /// <param name="levels">Array of levels to filter.</param>
        /// <returns>The filtered list of beatmaps.</returns>
        public List<IPreviewBeatmapLevel> ApplyFiltersFromExternalViewController(IPreviewBeatmapLevel[] levels)
        {
            Logger.log.Debug($"Providing SongBrowser with a list of filtered songs. Starting with {levels.Length} songs");

            BeatmapDetails[] detailsList = BeatmapDetailsLoader.instance.LoadBeatmapsInstant(levels);

            Dictionary<BeatmapDetails, IPreviewBeatmapLevel> pairs = new Dictionary<BeatmapDetails, IPreviewBeatmapLevel>(levels.Length);
            for (int i = 0; i < levels.Length; ++i)
            {
                if (detailsList[i] != null && !pairs.ContainsKey(detailsList[i]))
                    pairs.Add(detailsList[i], levels[i]);
            }

            var filteredLevels = pairs.Keys.ToList();
            foreach (var filter in _filterList)
            {
                if (filter.Status == FilterStatus.Applied || filter.Status == FilterStatus.AppliedAndChanged)
                    filter.FilterSongList(ref filteredLevels);
            }
            Logger.log.Debug($"Filter completed, {filteredLevels.Count} songs left");

            return pairs.Where(x => filteredLevels.Contains(x.Key)).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Unapply filters and refresh the UI.
        /// </summary>
        /// <param name="sendEvent">Invoke the FiltersUnapplied event.</param>
        public void UnapplyFilters(bool sendEvent = true)
        {
            foreach (var filter in _filterList)
                filter.ApplyDefaultValues();

            AreFiltersApplied = false;
            RefreshUI();
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

        private void FilterSelected(IFilter selectedFilter)
        {
            if (_currentFilter != null)
                _currentFilter.SettingChanged -= FilterSettingChanged;

            _filterMainViewController.ShowFilterContentView(selectedFilter);
            selectedFilter.SettingChanged += FilterSettingChanged;
            _currentFilter = selectedFilter;
        }

        private void ClearAllFilterChanges()
        {
            foreach (var filter in _filterList)
                filter.SetAppliedValuesToStaging();
            RefreshUI();
        }

        private void SetAllFiltersToDefault()
        {
            foreach (var filter in _filterList)
                filter.SetDefaultValuesToStaging();
            RefreshUI();
        }
    }
}
