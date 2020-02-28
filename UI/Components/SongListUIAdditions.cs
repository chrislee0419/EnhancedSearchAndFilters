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
using Image = UnityEngine.UI.Image;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class SongListUIAdditions : MonoBehaviour, INotifiableHost
    {
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

#pragma warning disable CS0649
        [UIObject("issues-list")]
        private GameObject _issuesContainer;

        [UIObject("page-up-button")]
        private GameObject _pageUpButton;
        [UIObject("page-down-button")]
        private GameObject _pageDownButton;
#pragma warning restore CS0649

        private BSMLParserParams _parserParams;

        private List<ReportedIssue> _issues = new List<ReportedIssue>();

        private LevelCollectionViewController _levelCollectionViewController;
        private TableView _tableView;
        private TableViewScroller _scroller;

        private bool _initialized = false;

        private static readonly string LogsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        private const string GitHubPageURL = "https://github.com/chrislee0419/EnhancedSearchAndFilters";

        private void Start()
        {
            _parserParams = Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.SongListUIView.bsml", this.gameObject, this);

            _levelCollectionViewController = this.gameObject.GetComponentInChildren<LevelCollectionViewController>();
            _scroller = _levelCollectionViewController.GetComponentInChildren<TableViewScroller>();

            // add bindings to and modify RectTransform size of existing page buttons
            _tableView = _levelCollectionViewController.GetComponentInChildren<TableView>();
            var buttonBinder = _tableView.GetPrivateField<ButtonBinder>("_buttonBinder");
            Button pageUpButton = _tableView.GetPrivateField<Button>("_pageUpButton");
            if (pageUpButton != null)
            {
                buttonBinder.AddBinding(pageUpButton, () => StartCoroutine(DelayedRefreshPageButtons()));

                var rt = (pageUpButton.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
                rt.sizeDelta = new Vector2(42f, rt.sizeDelta.y);
            }
            Button pageDownButton = _tableView.GetPrivateField<Button>("_pageDownButton");
            if (pageDownButton != null)
            {
                buttonBinder.AddBinding(pageDownButton, () => StartCoroutine(DelayedRefreshPageButtons()));

                var rt = (pageDownButton.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
                rt.sizeDelta = new Vector2(42f, rt.sizeDelta.y);
            }

            // replace fast page button icons
            Texture2D tex = UIUtilities.LoadTextureFromResources("EnhancedSearchAndFilters.Assets.doublechevron.png");
            Sprite doubleChevronSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            Image pageUpImage = _pageUpButton.GetComponentsInChildren<Image>().First(x => x.name == "Arrow");
            pageUpImage.sprite = doubleChevronSprite;
            pageUpImage.rectTransform.sizeDelta = new Vector2(4f, 3f);

            Image pageDownImage = _pageDownButton.GetComponentsInChildren<Image>().First(x => x.name == "Arrow");
            pageDownImage.sprite = doubleChevronSprite;
            pageDownImage.rectTransform.sizeDelta = new Vector2(4f, 3f);

            this.gameObject.GetComponent<LevelSelectionNavigationController>().didDeactivateEvent += OnNavigationControllerDeactivation;

            _initialized = true;
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;
            _parserParams.EmitEvent("hide-bug-report-modal");
        }

        private void OnNavigationControllerDeactivation(ViewController.DeactivationType deactivationType) => OnDisable();

        private void OnDestroy()
        {
            if (!_initialized)
                return;

            var navController = this.gameObject.GetComponent<LevelSelectionNavigationController>();
            if (navController != null)
                navController.didDeactivateEvent -= OnNavigationControllerDeactivation;
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
                        issue.SetText(issues[i]);
                    }

                    _issues.Add(issue);
                }
            });
        }

        private void RefreshPageButtons()
        {
            PageDownButtonInteractable = _scroller.targetPosition < _scroller.scrollableSize - 0.01f;
            PageUpButtonInteractable = _scroller.targetPosition > 0.01f;
        }

        private IEnumerator DelayedRefreshPageButtons()
        {
            yield return new WaitForEndOfFrame();
            RefreshPageButtons();
        }

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
