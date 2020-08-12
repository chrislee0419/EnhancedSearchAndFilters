using System;
using UnityEngine;
using HMUI;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.UI.ViewControllers;
using EnhancedSearchAndFilters.Search;
using SuggestionType = EnhancedSearchAndFilters.Search.SuggestedWord.SuggestionType;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.FlowCoordinators
{
    public class SearchFlowCoordinator : FlowCoordinator
    {
        public event Action BackButtonPressed;
        public event Action<IPreviewBeatmapLevel> SongSelected;
        public event Action<string> SearchFilterButtonPressed;

        private SearchResultsNavigationController _searchResultsNavigationController;
        private SearchResultsListViewController _searchResultsListViewController;
        private SongDetailsViewController _songDetailsViewController;

        private SearchOptionsViewController _searchOptionsViewController;

        private SearchKeyboardManagerBase _keyboardManager;
        private ViewController _keyboardViewController;

        private LaserPointerInputManager _laserPointerInputManager;

        private string _searchQuery = "";
        private string _lastSearchQuery = "";
        private IPreviewBeatmapLevel[] _levelsSearchSpace;

        // prevents the UI messing up when the user presses the back button very soon after activating the search screen
        // it's a bit of a bandaid, but i'll fix it once i find the root cause
        private bool _readyForDeactivation = false;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "Search For Songs";
                showBackButton = true;

                if (PluginConfig.TwoHandedTyping)
                    CreateLaserPointerManager();

                _searchResultsNavigationController = BeatSaberUI.CreateViewController<SearchResultsNavigationController>();
                _searchResultsListViewController = BeatSaberUI.CreateViewController<SearchResultsListViewController>();
                _songDetailsViewController = BeatSaberUI.CreateViewController<SongDetailsViewController>();
                _searchOptionsViewController = BeatSaberUI.CreateViewController<SearchOptionsViewController>();

                CreateKeyboardManager();

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
                    _keyboardManager.SetText(_searchQuery);

                    SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, levels => ShowSearchResult(levels, true));
                };
                _searchResultsNavigationController.ResetKeyboardPositionButtonPressed += delegate ()
                {
                    if (PluginConfig.SearchKeyboard == SearchKeyboardType.Floating && _keyboardManager is FloatingSearchKeyboardManager keyboardManager)
                        keyboardManager.ResetPosition();
                };

                _searchResultsListViewController.SongSelected += delegate (IPreviewBeatmapLevel level)
                {
                    if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact && _keyboardViewController.isInViewControllerHierarchy)
                        PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    if (!_songDetailsViewController.isInViewControllerHierarchy)
                        PushViewControllerToNavigationController(_searchResultsNavigationController, _songDetailsViewController, null, PluginConfig.SearchKeyboard == SearchKeyboardType.Compact);
                    _songDetailsViewController.SetContent(level);
                    _searchResultsNavigationController.CrossfadeAudioToLevelAsync(level);
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
                    if (PluginConfig.SearchKeyboard != SearchKeyboardType.Compact)
                        return;

                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _keyboardViewController, null, true);

                    _keyboardManager.SetText(_searchQuery);
                    _searchResultsListViewController.DeselectSong();
                };

                _searchOptionsViewController.SearchOptionsApplied += OptionsChanged;

                ProvideInitialViewControllers(_searchResultsNavigationController, _searchOptionsViewController, PluginConfig.SearchKeyboard == SearchKeyboardType.RightScreen ? _keyboardViewController : null);
            }
            else
            {
                _searchQuery = "";
                _readyForDeactivation = false;

                if (_laserPointerInputManager != null)
                    _laserPointerInputManager.gameObject.SetActive(true);

                _keyboardManager.Activate();
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            if (_laserPointerInputManager != null)
                _laserPointerInputManager.gameObject.SetActive(false);

            _keyboardManager.Deactivate();
        }

        /// <summary>
        /// Presents this flow coordinator and sets search space. 
        /// This must be used instead of invoking the private PresentFlowCoordinator to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will be immediately higher in the hierarchy that will present this flow coordinator.</param>
        /// <param name="levels">The list of levels that will be used as the search space.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IAnnotatedBeatmapLevelCollection levelPack)
        {
            _levelsSearchSpace = levelPack.beatmapLevelCollection.beatmapLevels;
            Action onFinish = PushInitialViewControllersToNavigationController;
            parentFlowCoordinator.PresentFlowCoordinator(this, onFinish);

            WordPredictionEngine.instance.SetActiveWordStorageFromLevelPack(levelPack);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (!_readyForDeactivation)
                return;

            _lastSearchQuery = _searchQuery;

            SearchBehaviour.Instance.StopSearch();
            _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
            PopAllViewControllersFromNavigationController(true);

            BackButtonPressed?.Invoke();
        }

        private void CreateKeyboardManager()
        {
            if (_keyboardViewController != null)
            {
                if (_keyboardViewController.isInViewControllerHierarchy)
                {
                    if (_keyboardManager is SearchCompactKeyboardManager)
                        PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    else if (_keyboardManager is RightScreenSearchKeyboardManager)
                        SetRightScreenViewController(null, true);
                }

                Destroy(_keyboardViewController.gameObject);
                _keyboardViewController = null;
            }
            if (_keyboardManager != null)
            {
                _keyboardManager.Deactivate();

                Destroy(_keyboardManager);
                _keyboardManager = null;
            }

            switch (PluginConfig.SearchKeyboard)
            {
                case SearchKeyboardType.Floating:
                    var floatingKeyboardManager = this.gameObject.AddComponent<FloatingSearchKeyboardManager>();
                    _keyboardManager = floatingKeyboardManager;
                    break;

                case SearchKeyboardType.RightScreen:
                    var rightScreenKeyboardManager = this.gameObject.AddComponent<RightScreenSearchKeyboardManager>();
                    _keyboardManager = rightScreenKeyboardManager;
                    _keyboardViewController = rightScreenKeyboardManager.ViewController;
                    break;

                case SearchKeyboardType.Compact:
                    var compactKeyboardManager = this.gameObject.AddComponent<SearchCompactKeyboardManager>();
                    _keyboardManager = compactKeyboardManager;
                    _keyboardViewController = compactKeyboardManager.ViewController;
                    break;
            }

            _keyboardManager.TextKeyPressed += KeyboardTextKeyPressed;
            _keyboardManager.DeleteButtonPressed += KeyboardDeleteButtonPressed;
            _keyboardManager.ClearButtonPressed += KeyboardClearButtonPressed;
            _keyboardManager.PredictionPressed += KeyboardPredictionPressed;
            _keyboardManager.FilterButtonPressed += KeyboardFilterButtonPressed;
        }

        private void PushInitialViewControllersToNavigationController()
        {
            // push view controllers to navigation controller after it has been activated, otherwise the coroutines will not be called
            PopAllViewControllersFromNavigationController(true);
            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
            {
                PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, delegate ()
                {
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _keyboardViewController, delegate ()
                    {
                        _readyForDeactivation = true;
                    });
                }, true);

                SetRightScreenViewController(null);
            }
            else
            {
                SetRightScreenViewController(PluginConfig.SearchKeyboard == SearchKeyboardType.RightScreen ? _keyboardViewController : null);
                _searchResultsNavigationController.ShowPlaceholderText();
                _readyForDeactivation = true;
            }

            if (!string.IsNullOrEmpty(_lastSearchQuery))
                _searchResultsNavigationController.ShowLastSearchButton(true, _lastSearchQuery);
        }

        private void KeyboardTextKeyPressed(char key)
        {
            _searchQuery += key.ToString();

            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowLoadingSpinner();
            _searchResultsNavigationController.CrossfadeAudioToDefault();

            // clear list, just in case the user forced the results to show
            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
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
                _searchResultsNavigationController.CrossfadeAudioToDefault();

                if (_searchQuery.Length > 0)
                {
                    _searchResultsNavigationController.ShowLoadingSpinner();
                    SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);

                    if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
                        _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
                }
                else
                {
                    SearchBehaviour.Instance.StopSearch();
                    if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
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
            if (!string.IsNullOrEmpty(_searchQuery))
                _lastSearchQuery = _searchQuery;

            _searchQuery = "";

            SearchBehaviour.Instance.StopSearch();
            _searchResultsNavigationController.CrossfadeAudioToDefault();

            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
            {
                if (_songDetailsViewController.isInViewControllerHierarchy)
                {
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _keyboardViewController);
                }

                _searchResultsNavigationController.HideUIElements();
            }
            else
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowPlaceholderText();
            }

            _searchResultsListViewController.UpdateSongs(null);

            // allow the user to redo the cleared search
            if (!string.IsNullOrEmpty(_lastSearchQuery))
                _searchResultsNavigationController.ShowLastSearchButton(true, _lastSearchQuery);
        }

        private void KeyboardPredictionPressed(string query, SuggestionType type)
        {
            _searchQuery = query;

            PopAllViewControllersFromNavigationController();
            _searchResultsNavigationController.ShowLoadingSpinner();
            _searchResultsNavigationController.CrossfadeAudioToDefault();

            // clear list, just in case the user forced the results to show
            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
                _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);

            // we need to start a new search when a fuzzy match prediction is pressed,
            // otherwise the search would have already filtered out the selected word
            if (type == SuggestionType.FuzzyMatch)
                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            else
                SearchBehaviour.Instance.StartSearchOnExistingList(_searchQuery, SearchCompleted);
        }

        private void KeyboardFilterButtonPressed()
        {
            if (string.IsNullOrEmpty(_searchQuery))
                return;

            _lastSearchQuery = _searchQuery;

            SearchBehaviour.Instance.StopSearch();
            _searchResultsListViewController.UpdateSongs(new IPreviewBeatmapLevel[0]);
            PopAllViewControllersFromNavigationController(true);

            SearchFilterButtonPressed?.Invoke(_searchQuery);
        }

        private void OptionsChanged()
        {
            CreateKeyboardManager();

            _keyboardManager.SetSymbolButtonInteractivity(!PluginConfig.StripSymbols);

            _searchResultsNavigationController.AdjustElements();
            _searchResultsNavigationController.CrossfadeAudioToDefault();
            _searchResultsListViewController.UpdateSize();

            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
            {
                if (_songDetailsViewController.isInViewControllerHierarchy)
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                if (!_searchResultsListViewController.isInViewControllerHierarchy)
                    PushViewControllerToNavigationController(_searchResultsNavigationController, _searchResultsListViewController, null, true);

                if (_searchQuery.Length == 0)
                    _searchResultsListViewController.UpdateSongs(null);

                PushViewControllerToNavigationController(_searchResultsNavigationController, _keyboardViewController);
                _searchResultsNavigationController.HideUIElements();
            }
            else if (string.IsNullOrEmpty(_searchQuery))
            {
                if (_searchResultsListViewController.isInViewControllerHierarchy)
                    PopViewControllerFromNavigationController(_searchResultsNavigationController, null, true);
                _searchResultsNavigationController.ShowPlaceholderText();
            }

            if (PluginConfig.SearchKeyboard == SearchKeyboardType.RightScreen)
                SetRightScreenViewController(_keyboardViewController);
            ProvideInitialViewControllers(_searchResultsNavigationController, _searchOptionsViewController, PluginConfig.SearchKeyboard == SearchKeyboardType.RightScreen ? _keyboardViewController : null);

            _keyboardManager.SetText(_searchQuery);

            if (PluginConfig.TwoHandedTyping && _laserPointerInputManager == null)
                CreateLaserPointerManager();
            else if (!PluginConfig.TwoHandedTyping && _laserPointerInputManager != null)
                Destroy(_laserPointerInputManager.gameObject);

            if (_searchQuery.Length > 0)
            {
                PopAllViewControllersFromNavigationController();
                _searchResultsNavigationController.ShowLoadingSpinner();
                _searchResultsListViewController.UpdateSongs(null);

                SearchBehaviour.Instance.StartNewSearch(_levelsSearchSpace, _searchQuery, SearchCompleted);
            }
        }

        private void SearchCompleted(IPreviewBeatmapLevel[] levels) => ShowSearchResult(levels);

        private void ShowSearchResult(IPreviewBeatmapLevel[] levels, bool force = false)
        {
            force |= PluginConfig.MaxSearchResults == PluginConfig.MaxSearchResultsUnlimitedValue;
            if ((!force && levels.Length > PluginConfig.MaxSearchResults) || levels.Length == 0)
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
            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact && !force)
                return;

            int numOfViewControllers = _searchResultsListViewController.isInViewControllerHierarchy ? 1 : 0;
            numOfViewControllers += _songDetailsViewController.isInViewControllerHierarchy ? 1 : 0;

            // always pop view controllers immediately, otherwise UI elements can be blended together
            // or search can be completed before transition animation and can't interrupt (?)
            if (numOfViewControllers > 0)
                PopViewControllersFromNavigationController(_searchResultsNavigationController, numOfViewControllers, null, true);
        }

        private void CreateLaserPointerManager()
        {
            _laserPointerInputManager = new GameObject("ESAFOffHandLaserPointerInputManager").AddComponent<LaserPointerInputManager>();
            _laserPointerInputManager.transform.SetParent(this.transform);
        }
    }
}
