using System;
using VRUI;
using EnhancedSearchAndFilters.UI.ViewControllers;
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

        private string _searchQuery = "";
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

                _searchResultsNavigationController.BackButtonPressed += delegate ()
                {
                    SearchBehaviour.Instance.StopSearch();
                    PopAllViewControllersFromNavigationController();
                    BackButtonPressed?.Invoke();
                };
                _searchResultsListViewController.SongSelected += delegate (IPreviewBeatmapLevel level)
                {
                    if (!_songDetailsViewController.isInViewControllerHierarchy)
                        PushViewControllerToNavigationController(_searchResultsNavigationController, _songDetailsViewController);
                    _songDetailsViewController.SetContent(level);
                };
                _songDetailsViewController.SelectButtonPressed += delegate (IPreviewBeatmapLevel level)
                {
                    SearchBehaviour.Instance.StopSearch();
                    PopAllViewControllersFromNavigationController();
                    SongSelected?.Invoke(level);
                };

                _searchOptionsViewController.SearchOptionsChanged += OptionsChanged;

                _searchKeyboardViewController.TextKeyPressed += KeyboardTextKeyPressed;
                _searchKeyboardViewController.DeleteButtonPressed += KeyboardDeleteButtonPressed;
                _searchKeyboardViewController.ClearButtonPressed += KeyboardClearButtonPressed;

                ProvideInitialViewControllers(_searchResultsNavigationController, _searchOptionsViewController, _searchKeyboardViewController);
            }
            else
            {
                _searchQuery = "";
                _searchResultsNavigationController.ShowPlaceholderText();
            }
        }

        /// <summary>
        /// Presents this flow coordinator and sets search space. 
        /// This must be used instead of invoking the private PresentFlowCoordinator to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will be immediately higher in the hierarchy that will present this flow coordinator.</param>
        /// <param name="levels">The list of levels that will be used as the search space.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IPreviewBeatmapLevel[] levels)
        {
            _levelsSearchSpace = levels;
            parentFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { this, null, false, false });
        }

        private void KeyboardTextKeyPressed(char key)
        {
            _searchQuery += key.ToString();

            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowLoadingSpinner();

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
                }
                else
                {
                    SearchBehaviour.Instance.StopSearch();
                    _searchResultsNavigationController.ShowPlaceholderText();
                }
            }
        }

        private void KeyboardClearButtonPressed()
        {
            _searchQuery = "";

            SearchBehaviour.Instance.StopSearch();
            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowPlaceholderText();
        }

        private void OptionsChanged()
        {
            _searchKeyboardViewController.SetSymbolButtonInteractivity(!PluginConfig.StripSymbols);

            if (_searchQuery.Length > 0)
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowLoadingSpinner();

                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            }
        }

        private void SearchCompleted(IPreviewBeatmapLevel[] levels)
        {
            if (levels.Length > PluginConfig.MaxSearchResults || levels.Length == 0)
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowResults(_searchQuery, levels, _levelsSearchSpace.Length);
            }
            else if (levels.Length > 0)
            {
                _searchResultsNavigationController.HideUIElements();

                if (!_searchResultsListViewController.isInViewControllerHierarchy)
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, null, true);

                _searchResultsListViewController.UpdateSongs(levels);
            }
        }

        private void PopAllViewControllersFromNavigationController()
        {
            int numOfViewControllers = _searchResultsListViewController.isInViewControllerHierarchy ? 1 : 0;
            numOfViewControllers += _songDetailsViewController.isInViewControllerHierarchy ? 1 : 0;

            // always pop view controllers immediately, otherwise UI elements can be blended together
            // or search can be completed before transition animation and can't interrupt (?)
            if (numOfViewControllers > 0)
                PopViewControllersFromNavigationController(_searchResultsNavigationController, numOfViewControllers, null, true);
        }
    }
}
