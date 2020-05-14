using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Parser;
using EnhancedSearchAndFilters.UI.Components;
using EnhancedSearchAndFilters.Utilities;
using Random = System.Random;
using Image = UnityEngine.UI.Image;
using BSUtilsUtilities = BS_Utils.Utilities.UIUtilities;
using UIUtilities = EnhancedSearchAndFilters.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.UI
{
    internal class SongListUIAdditions : MonoBehaviour, INotifiableHost
    {
        public event Action<CustomBeatmapLevel> ConfirmDeleteButtonPressed;
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _pageUpInteractable = false;
        [UIValue("page-up-interactable")]
        public bool PageUpButtonInteractable
        {
            get => _pageUpInteractable;
            set
            {
                if (_pageUpInteractable == value)
                    return;

                _pageUpInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _pageDownInteractable = true;
        [UIValue("page-down-interactable")]
        public bool PageDownButtonInteractable
        {
            get => _pageDownInteractable;
            set
            {
                if (_pageDownInteractable == value)
                    return;

                _pageDownInteractable = value;
                NotifyPropertyChanged();
            }
        }

        private string _deleteConfirmationText;
        [UIValue("delete-confirmation-text")]
        public string DeleteConfirmationText
        {
            get => _deleteConfirmationText;
            set
            {
                if (_deleteConfirmationText == value)
                    return;

                _deleteConfirmationText = value;
                NotifyPropertyChanged();
            }
        }
        public bool DeleteButtonInteractable
        {
            get => _deleteButton == null ? false : _deleteButton.interactable;
            set
            {
                if (_deleteButton != null)
                    _deleteButton.interactable = value;
            }
        }

#pragma warning disable CS0649
        [UIObject("issues-list")]
        private GameObject _issuesContainer;

        [UIComponent("delete-button")]
        private Button _deleteButton;

        [UIObject("page-up-button")]
        private GameObject _pageUpButton;
        [UIObject("page-down-button")]
        private GameObject _pageDownButton;
        [UIObject("random-button")]
        private GameObject _randomButton;
#pragma warning restore CS0649

        private BSMLParserParams _parserParams;

        private List<ReportedIssue> _issues = new List<ReportedIssue>();
        private Random _rng = new Random();

        private StandardLevelDetailView _standardLevelDetailView;
        private LevelCollectionViewController _levelCollectionViewController;
        private TableView _tableView;
        private TableViewScroller _scroller;

        private CustomBeatmapLevel _levelToDelete;

        private bool _initialized = false;

        private static readonly string LogsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        private const string GitHubPageURL = "https://github.com/chrislee0419/EnhancedSearchAndFilters";

        private void Start()
        {
            _parserParams = UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.SongListUIView.bsml", this.gameObject, this);

            _levelCollectionViewController = this.gameObject.GetComponentInChildren<LevelCollectionViewController>();
            _scroller = _levelCollectionViewController.GetComponentInChildren<TableViewScroller>();

            // add bindings to and modify RectTransform size of existing page buttons
            _tableView = _levelCollectionViewController.GetComponentInChildren<TableView>();
            var buttonBinder = _tableView.GetPrivateField<ButtonBinder>("_buttonBinder");
            Button pageUpButton = _tableView.GetPrivateField<Button>("_pageUpButton");
            if (pageUpButton != null)
            {
                buttonBinder.AddBinding(pageUpButton, () => StartCoroutine(UIUtilities.DelayedAction(RefreshPageButtons)));

                var rt = (pageUpButton.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
                rt.sizeDelta = new Vector2(42f, rt.sizeDelta.y);
            }
            Button pageDownButton = _tableView.GetPrivateField<Button>("_pageDownButton");
            if (pageDownButton != null)
            {
                buttonBinder.AddBinding(pageDownButton, () => StartCoroutine(UIUtilities.DelayedAction(RefreshPageButtons)));

                var rt = (pageDownButton.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
                rt.sizeDelta = new Vector2(42f, rt.sizeDelta.y);
            }

            // replace fast page button icons
            Texture2D tex = BSUtilsUtilities.LoadTextureFromResources("EnhancedSearchAndFilters.Assets.doublechevron.png");
            Sprite doubleChevronSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            Image pageUpImage = _pageUpButton.GetComponentsInChildren<Image>().First(x => x.name == "Arrow");
            pageUpImage.sprite = doubleChevronSprite;
            pageUpImage.rectTransform.sizeDelta = new Vector2(4f, 3f);

            Image pageDownImage = _pageDownButton.GetComponentsInChildren<Image>().First(x => x.name == "Arrow");
            pageDownImage.sprite = doubleChevronSprite;
            pageDownImage.rectTransform.sizeDelta = new Vector2(4f, 3f);

            // replace random button icon
            tex = BSUtilsUtilities.LoadTextureFromResources("EnhancedSearchAndFilters.Assets.shuffle.png");
            Sprite randomSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            Image randomImage = _randomButton.GetComponentsInChildren<Image>().First(x => x.name == "Arrow");
            randomImage.sprite = randomSprite;
            randomImage.rectTransform.sizeDelta = new Vector2(4f, 3f);

            // move buttons to the correct transform
            Transform parent = (_tableView.dataSource as LevelCollectionTableView).transform;
            _pageUpButton.transform.SetParent(parent, true);
            _pageDownButton.transform.SetParent(parent, true);
            _randomButton.transform.SetParent(parent, true);

            // since the StandardLevelDetailViewController isn't parented to the navigation controller at the start,
            // we have to delay the setup for the delete button until it is parented (after a level is selected)
            _levelCollectionViewController.didSelectLevelEvent += SetupDeleteButton;

            this.gameObject.GetComponent<LevelSelectionNavigationController>().didDeactivateEvent += OnNavigationControllerDeactivation;

            _initialized = true;
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;
            _parserParams.EmitEvent("hide-bug-report-modal,hide-delete-confirmation-modal");
        }

        private void OnNavigationControllerDeactivation(ViewController.DeactivationType deactivationType) => OnDisable();

        private void OnDestroy()
        {
            if (!_initialized)
                return;

            var navController = this.gameObject.GetComponent<LevelSelectionNavigationController>();
            if (navController != null)
                navController.didDeactivateEvent -= OnNavigationControllerDeactivation;

            _levelCollectionViewController.didSelectLevelEvent -= SetupDeleteButton;
            _levelCollectionViewController.didSelectLevelEvent -= OnLevelSelected;
        }

        private void SetupDeleteButton(LevelCollectionViewController viewController, IPreviewBeatmapLevel level)
        {
            StartCoroutine(UIUtilities.DelayedAction(delegate ()
            {
                // can't use GetComponentInChildren to get the StandardLevelDetailView,
                // since it may not be parented to the StandardLevelDetailViewController (doesn't get parented until it needs to be?)
                // at least we can take it directly through the private field in StandardLevelDetailViewController
                _standardLevelDetailView = this.GetComponentInChildren<StandardLevelDetailViewController>().GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView", typeof(StandardLevelDetailViewController));
                _deleteButton.transform.SetParent(_standardLevelDetailView.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "PlayButtons").transform, false);
                _deleteButton.gameObject.SetActive(true);

                viewController.didSelectLevelEvent += OnLevelSelected;

                OnLevelSelected(viewController, level);
            }));

            viewController.didSelectLevelEvent -= SetupDeleteButton;
        }

        public void ShowBugReportModal()
        {
            if (!_initialized)
                return;

            foreach (var issue in _issues)
                DestroyImmediate(issue.gameObject);
            _issues.Clear();

            _parserParams.EmitEvent("show-bug-report-modal");

            GitHubAPIHelper.instance.GetOpenIssues(delegate (bool success, List<string> issues)
            {
                if (!success)
                    issues = new List<string>(1);
                if (issues.Count == 0)
                    issues.Add("No reported issues");

                const float TextHeight = 6f;
                int maxIssues = (int)Math.Floor((_issuesContainer.transform as RectTransform).rect.height / TextHeight);
                for (int i = 0; i < maxIssues && i < issues.Count; ++i)
                {

                    var go = new GameObject("ReportedIssue");
                    go.transform.SetParent(_issuesContainer.transform, false);

                    var issue = go.AddComponent<ReportedIssue>();

                    var rt = issue.transform as RectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = new Vector2(0f, TextHeight);

                    if (i == maxIssues - 1)
                    {
                        int remainingIssues = issues.Count - maxIssues + 1;
                        issue.SetText($"<color=#FFCCCC>And {remainingIssues} other issue{(remainingIssues == 1 ? "" : "s")} have been reported</color>");
                    }
                    else
                    {
                        issue.SetText(issues[i].EscapeTextMeshProTags());
                    }

                    _issues.Add(issue);
                }
            });
        }

        public void RefreshPageButtons()
        {
            // the first time this is called in SongListUI:LevelPackSelected, the scroller can be null
            // but on subsequent calls, it isn't null even though there's not reassignment on the scroller?
            // i don't get how this works (GetComponentInChildren somehow binds a reference to your variable?)
            if (_scroller == null)
                return;

            PageDownButtonInteractable = _scroller.targetPosition < _scroller.scrollableSize - 0.01f;
            PageUpButtonInteractable = _scroller.targetPosition > 0.01f;
        }

        private void OnLevelSelected(LevelCollectionViewController _, IPreviewBeatmapLevel level) => DeleteButtonInteractable = level is CustomPreviewBeatmapLevel;

        #region BSML Actions
        [UIAction("open-logs-folder-button-clicked")]
        private void OnOpenLogsFolderButtonClicked() => Process.Start(LogsFolder);

        [UIAction("open-github-page-button-clicked")]
        private void OnOpenGithubPageButtonClicked() => Process.Start(GitHubPageURL);

        [UIAction("page-up-button-clicked")]
        private void OnPageUpButtonClicked()
        {
            float numOfVisibleCells = Mathf.Ceil(_tableView.scrollRectTransform.rect.height / _tableView.cellSize);
            float newTargetPosition = _scroller.targetPosition - Mathf.Max(1f, numOfVisibleCells - 1f) * _tableView.cellSize * PluginConfig.FastScrollSpeed;
            if (newTargetPosition < 0f)
                newTargetPosition = 0f;

            _scroller.SetPrivateField("_targetPosition", newTargetPosition, typeof(TableViewScroller));
            _scroller.enabled = true;

            RefreshPageButtons();
            _tableView.InvokeMethod("RefreshScrollButtons", true);
        }

        [UIAction("delete-button-clicked")]
        private void OnDeleteButtonClicked()
        {
            IBeatmapLevel level = _standardLevelDetailView.GetPrivateField<IBeatmapLevel>("_level", typeof(StandardLevelDetailView));

            if (level is CustomBeatmapLevel customLevel)
            {
                _levelToDelete = customLevel;
                DeleteConfirmationText = $"Are you sure you would like to delete '<color=#FFFFCC>{customLevel.songName.EscapeTextMeshProTags()}</color>' by {customLevel.levelAuthorName.EscapeTextMeshProTags()}?";

                _parserParams.EmitEvent("show-delete-confirmation-modal");
            }
            else
            {
                Logger.log.Debug("Unable to delete level (not a custom level)");
                _deleteButton.interactable = false;
            }
        }

        [UIAction("confirm-delete-button-clicked")]
        private void OnConfirmDeleteButtonClicked()
        {
            if (_levelToDelete != null)
            {
                Logger.log.Info($"Deleting beatmap '{_levelToDelete.songName}' by {_levelToDelete.levelAuthorName}");

                int indexOfTopCell = 0;
                if (_tableView.visibleCells.Count() > 0)
                    indexOfTopCell = _tableView.visibleCells.Min(x => x.idx);

                ConfirmDeleteButtonPressed?.Invoke(_levelToDelete);

                // scroll back to where we were
                _tableView.ScrollToCellWithIdx(indexOfTopCell, TableViewScroller.ScrollPositionType.Beginning, false);
            }

            _parserParams.EmitEvent("hide-delete-confirmation-modal");
        }

        [UIAction("page-down-button-clicked")]
        private void OnPageDownButtonClicked()
        {
            float maxPosition = _tableView.numberOfCells * _tableView.cellSize - _tableView.scrollRectTransform.rect.height;
            float numOfVisibleCells = Mathf.Ceil(_tableView.scrollRectTransform.rect.height / _tableView.cellSize);
            float newTargetPosition = _scroller.targetPosition + Mathf.Max(1f, numOfVisibleCells - 1f) * _tableView.cellSize * PluginConfig.FastScrollSpeed;
            if (newTargetPosition > maxPosition)
                newTargetPosition = maxPosition;

            _scroller.SetPrivateField("_targetPosition", newTargetPosition, typeof(TableViewScroller));
            _scroller.enabled = true;

            RefreshPageButtons();
            _tableView.InvokeMethod("RefreshScrollButtons", true);
        }

        [UIAction("random-button-clicked")]
        private void OnRandomButtonClicked()
        {
            if (_tableView.numberOfCells == 0)
                return;

            int index = _rng.Next(_tableView.numberOfCells);
            while (index == 0 && (_tableView.dataSource as LevelCollectionTableView).GetPrivateField<bool>("_showLevelPackHeader"))
                index = _rng.Next(_tableView.numberOfCells);

            _tableView.ScrollToCellWithIdx(index, TableViewScroller.ScrollPositionType.Beginning, false);
            RefreshPageButtons();
        }
        #endregion

        #region ReportedIssue class
        [RequireComponent(typeof(LayoutElement))]
        internal class ReportedIssue : MonoBehaviour
        {
            private ScrollingText _text;

            private void Awake()
            {
                // not sure why, but RequireComponent isn't working with the ScrollingText component
                _text = this.gameObject.AddComponent<ScrollingText>();
                _text.AnimationType = ScrollingText.ScrollAnimationType.ForwardAndReverse;
                _text.FontSize = 3.6f;
            }

            public void SetText(string text) => _text.Text = text;
        }
        #endregion

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception e)
            {
                Logger.log.Error($"Error Invoking PropertyChanged: {e.Message}");
                Logger.log.Error(e);
            }
        }
    }
}
