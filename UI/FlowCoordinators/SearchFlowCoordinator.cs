using System;
using VRUI;
using EnhancedSearchAndFilters.UI.ViewControllers;
using EnhancedSearchAndFilters.Search;
using CustomUI.BeatSaber;
using CustomUI.Utilities;

namespace EnhancedSearchAndFilters.UI.FlowCoordinators
{
    class SearchFlowCoordinator : FlowCoordinator
    {
        public Action BackButtonPressed;
        public Action<IPreviewBeatmapLevel> SongSelected;

        private SearchResultsNavigationController _searchResultsNavigationController;
        private SearchResultsListViewController _searchResultsListViewController;
        private SongDetailsViewController _songDetailsViewController;

        private SearchOptionsViewController _searchOptionsViewController;
        private SearchKeyboardViewController _searchKeyboardViewController;
        private SearchCompactKeyboardViewController _searchCompactKeyboardViewController;

        private string _searchQuery = "";
        private string _lastSearchQuery = "";
        private IPreviewBeatmapLevel[] _levelsSearchSpace;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "Search For Songs";

                _searchResultsNavigationController = BeatSaberUI.CreateViewController<SearchResultsNavigationController>();
                _searchResultsListViewController = BeatSaberUI.CreateViewController<SearchResultsListViewController>();
                _songDetailsViewController = BeatSaberUI.CreateViewController<SongDetailsViewController>();
                _searchOptionsViewController = BeatSaberUI.CreateViewController<SearchOptionsViewController>();
                _searchKeyboardViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchCompactKeyboardViewController = BeatSaberUI.CreateViewController<SearchCompactKeyboardViewController>();

                _searchResultsNavigationController.BackButtonPressed += delegate ()
                {
                    _lastSearchQuery = _searchQuery;

                    SearchBehaviour.Instance.StopSearch();
                    _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                    PopAllViewControllersFromNavigationController(true);
                    BackButtonPressed?.Invoke();
                };
                _searchResultsNavigationController.ForceShowButtonPressed += delegate ()
                {
                    ShowSearchResult(SearchBehaviour.Instance.CachedResult, true);
                };
                _searchResultsNavigationController.LastSearchButtonPressed += delegate ()
                {
                    _searchResultsNavigationController.ShowLastSearchButton(false);

                    if (string.IsNullOrEmpty(_lastSearchQuery))
                        return;

                    PopAllViewControllersFromNavigationController();
                    _searchResultsNavigationController.ShowLoadingSpinner();
                    _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);

                    _searchQuery = _lastSearchQuery;

                    if (PluginConfig.CompactSearchMode)
                        _searchCompactKeyboardViewController.SetText(_searchQuery);
                    else
                        _searchKeyboardViewController.SetText(_searchQuery);
                    SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
                };
                _searchResultsListViewController.SongSelected += delegate (IPreviewBeatmapLevel level)
                {
                    if (_searchCompactKeyboardViewController.isInViewControllerHierarchy)
                        PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    if (!_songDetailsViewController.isInViewControllerHierarchy)
                        PushViewControllerToNavigationController(_searchResultsNavigationController, _songDetailsViewController, null, PluginConfig.CompactSearchMode);
                    _songDetailsViewController.SetContent(level);
                };
                _songDetailsViewController.SelectButtonPressed += delegate (IPreviewBeatmapLevel level)
                {
                    _lastSearchQuery = _searchQuery;

                    SearchBehaviour.Instance.StopSearch();
                    _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                    PopAllViewControllersFromNavigationController(true);
                    SongSelected?.Invoke(level);
                };
                _songDetailsViewController.CompactKeyboardButtonPressed += delegate ()
                {
                    if (!PluginConfig.CompactSearchMode)
                        return;

                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchCompactKeyboardViewController, null, true);

                    _searchCompactKeyboardViewController.SetText(_searchQuery);
                    _searchResultsListViewController.DeselectSong();
                };

                _searchOptionsViewController.SearchOptionsChanged += OptionsChanged;

                _searchKeyboardViewController.TextKeyPressed += KeyboardTextKeyPressed;
                _searchKeyboardViewController.DeleteButtonPressed += KeyboardDeleteButtonPressed;
                _searchKeyboardViewController.ClearButtonPressed += KeyboardClearButtonPressed;
                _searchKeyboardViewController.PredictionPressed += KeyboardPredictionPressed;

                _searchCompactKeyboardViewController.TextKeyPressed += KeyboardTextKeyPressed;
                _searchCompactKeyboardViewController.DeleteButtonPressed += KeyboardDeleteButtonPressed;
                _searchCompactKeyboardViewController.ClearButtonPressed += KeyboardClearButtonPressed;
                _searchCompactKeyboardViewController.PredictionPressed += KeyboardPredictionPressed;

                ProvideInitialViewControllers(_searchResultsNavigationController, _searchOptionsViewController, PluginConfig.CompactSearchMode ? null : _searchKeyboardViewController);
            }
            else
            {
                _searchQuery = "";
            }

        }

        /// <summary>
        /// Presents this flow coordinator and sets search space. 
        /// This must be used instead of invoking the private PresentFlowCoordinator to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will be immediately higher in the hierarchy that will present this flow coordinator.</param>
        /// <param name="levels">The list of levels that will be used as the search space.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IBeatmapLevelPack levelPack)
        {
            _levelsSearchSpace = levelPack.beatmapLevelCollection.beatmapLevels;
            Action onFinish = PushInitialViewControllersToNavigationController;
            parentFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { this, onFinish, false, false });

            WordPredictionEngine.Instance.SetActiveWordStorageFromLevelPack(levelPack);
        }

        public void PushInitialViewControllersToNavigationController()
        {
            // push view controllers to navigation controller after it has been activated, otherwise the coroutines will not be called
            PopAllViewControllersFromNavigationController(true);
            if (PluginConfig.CompactSearchMode)
            {
                PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, delegate ()
                {
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchCompactKeyboardViewController);
                }, true);

                SetRightScreenViewController(null);
            }
            else
            {
                SetRightScreenViewController(_searchKeyboardViewController);
                _searchResultsNavigationController.ShowPlaceholderText();
            }

            if (!string.IsNullOrEmpty(_lastSearchQuery))
                _searchResultsNavigationController.ShowLastSearchButton();
        }

        private void KeyboardTextKeyPressed(char key)
        {
            _searchQuery += key.ToString();

            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowLoadingSpinner();

            // clear list, just in case the user forced the results to show
            if (PluginConfig.CompactSearchMode)
                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);

            if (_searchQuery.Length == 1)
                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            else
                SearchBehaviour.Instance.StartSearchOnExistingList(_searchQuery, SearchCompleted);
        }

        private void KeyboardDeleteButtonPressed()
        {
            if (_searchQuery.Length > 0)
            {
                _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);

                PopAllViewControllersFromNavigationController();

                if (_searchQuery.Length > 0)
                {
                    _searchResultsNavigationController.ShowLoadingSpinner();
                    SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);

                    if (PluginConfig.CompactSearchMode)
                        _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                }
                else
                {
                    SearchBehaviour.Instance.StopSearch();
                    if (PluginConfig.CompactSearchMode)
                    {
                        _searchResultsNavigationController.HideUIElements();
                        _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                    }
                    else
                    {
                        _searchResultsNavigationController.ShowPlaceholderText();
                    }
                }
            }
        }

        private void KeyboardClearButtonPressed()
        {
            _searchQuery = "";

            SearchBehaviour.Instance.StopSearch();

            if (PluginConfig.CompactSearchMode)
            {
                if (_songDetailsViewController.isInViewControllerHierarchy)
                {
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchCompactKeyboardViewController);
                }

                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                _searchResultsNavigationController.HideUIElements();
            }
            else
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                _searchResultsNavigationController.ShowPlaceholderText();
            }
        }

        private void KeyboardPredictionPressed(string query)
        {
            _searchQuery = query;

            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowLoadingSpinner();

            // clear list, just in case the user forced the results to show
            if (PluginConfig.CompactSearchMode)
                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);

            if (_searchQuery.Length == 1)
                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            else
                SearchBehaviour.Instance.StartSearchOnExistingList(_searchQuery, SearchCompleted);
        }

        private void OptionsChanged()
        {
            _searchKeyboardViewController.SetSymbolButtonInteractivity(!PluginConfig.StripSymbols);
            _searchCompactKeyboardViewController.SetSymbolButtonInteractivity(!PluginConfig.StripSymbols);

            _searchResultsNavigationController.AdjustElements();
            _searchResultsListViewController.UpdateSize();

            if (PluginConfig.CompactSearchMode)
            {
                if (_searchKeyboardViewController.isInViewControllerHierarchy)
                    SetRightScreenViewController(null);
                if (_songDetailsViewController.isInViewControllerHierarchy)
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                if (!_searchResultsListViewController.isInViewControllerHierarchy)
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, null, true);
                if (!_searchCompactKeyboardViewController.isInViewControllerHierarchy)
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchCompactKeyboardViewController);
                _searchResultsNavigationController.HideUIElements();
                _searchCompactKeyboardViewController.SetText(_searchQuery);
            }
            else
            {
                if (!_searchKeyboardViewController.isInViewControllerHierarchy)
                    SetRightScreenViewController(_searchKeyboardViewController);
                if (_searchCompactKeyboardViewController.isInViewControllerHierarchy)
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                if (string.IsNullOrEmpty(_searchQuery))
                {
                    if (_searchResultsListViewController.isInViewControllerHierarchy)
                        PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    _searchResultsNavigationController.ShowPlaceholderText();
                }
                _searchKeyboardViewController.SetText(_searchQuery);
            }

            if (_searchQuery.Length > 0)
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowLoadingSpinner();
                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);

                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            }
        }

        private void SearchCompleted(IPreviewBeatmapLevel[] levels)
        {
            ShowSearchResult(levels);
        }

        private void ShowSearchResult(IPreviewBeatmapLevel[] levels, bool force = false)
        {
            if (!force && (levels.Length > PluginConfig.MaxSearchResults || levels.Length == 0))
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowResults(_searchQuery, levels, _levelsSearchSpace.Length);
            }
            else
            {
                _searchResultsNavigationController.HideUIElements();

                if (!_searchResultsListViewController.isInViewControllerHierarchy)
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, null, true);

                _searchResultsListViewController.UpdateSongs(levels);
            }
        }

        private void PopAllViewControllersFromNavigationController(bool force = false)
        {
            // we usually don't need to pop view controllers from the centre screen when in compact mode
            if (PluginConfig.CompactSearchMode && !force)
                return;

            int numOfViewControllers = _searchResultsListViewController.isInViewControllerHierarchy ? 1 : 0;
            numOfViewControllers += _songDetailsViewController.isInViewControllerHierarchy ? 1 : 0;

            // always pop view controllers immediately, otherwise UI elements can be blended together
            // or search can be completed before transition animation and can't interrupt (?)
            if (numOfViewControllers > 0)
                PopViewControllersFromNavigationController(_searchResultsNavigationController, numOfViewControllers, null, true);
        }
    }
}
