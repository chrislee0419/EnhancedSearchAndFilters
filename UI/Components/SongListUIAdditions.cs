using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class SongListUIAdditions : MonoBehaviour
    {
        private List<ReportedIssue> _issues = new List<ReportedIssue>();

#pragma warning disable CS0649
        [UIObject("issues-list")]
        private GameObject _issuesContainer;
#pragma warning restore CS0649

        private BSMLParserParams _parserParams;

        private bool _initialized = false;

        private static readonly string LogsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        private const string GitHubPageURL = "https://github.com/chrislee0419/EnhancedSearchAndFilters";

        private void Start()
        {
            _parserParams = Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.SongListUIView.bsml", this.gameObject, this);

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

        [UIAction("open-logs-folder-button-clicked")]
        private void OnOpenLogsFolderButtonClicked() => Process.Start(LogsFolder);

        [UIAction("open-github-page-button-clicked")]
        private void OnOpenGithubPageButtonClicked() => Process.Start(GitHubPageURL);

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
    }
}
