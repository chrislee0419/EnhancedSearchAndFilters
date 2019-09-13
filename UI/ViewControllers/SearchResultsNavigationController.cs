using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRUI;
using CustomUI.BeatSaber;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchResultsNavigationController : VRUINavigationController
    {
        public Action BackButtonPressed;
        public Action ForceShowButtonPressed;
        public Action LastSearchButtonPressed;

        private GameObject _header;
        private GameObject _loadingSpinner;
        private TextMeshProUGUI _resultsText;
        private Button _forceButton;
        private Button _lastSearchButton;
        private TextMeshProUGUI _lastSearchText;

        private const string _headerText = "Search Results";
        private const string _placeholderResultsText = "Use the keyboard on the right screen\nto search for a song.\n\n---->";

        private static readonly Vector2 ResultsTextDefaultAnchoredPosition = Vector2.zero;
        private static readonly Vector2 ResultsTextDefaultSizeDelta = new Vector2(120f, 40f);
        private const float ResultsTextDefaultFontSize = 6f;
        private static readonly Vector2 LoadingSpinnerDefaultAnchoredPosition = Vector2.zero;

        private static readonly Vector2 ResultsTextCompactAnchoredPosition = new Vector2(-35f, 0f);
        private static readonly Vector2 ResultsTextCompactSizeDelta = new Vector2(50f, 40f);
        private const float ResultsTextCompactFontSize = 5f;
        private static readonly Vector2 LoadingSpinnerCompactAnchoredPosition = new Vector2(-35f, 0f);

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                BeatSaberUI.CreateBackButton(this.rectTransform, () => BackButtonPressed?.Invoke());
                _loadingSpinner = BeatSaberUI.CreateLoadingSpinner(this.rectTransform);

                var headerRectTransform = Instantiate(Resources.FindObjectsOfTypeAll<RectTransform>()
                    .First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), this.rectTransform);
                _header = headerRectTransform.gameObject;

                _resultsText = BeatSaberUI.CreateText(this.rectTransform, _placeholderResultsText, Vector2.zero, Vector2.zero);
                _resultsText.alignment = TextAlignmentOptions.Center;
                _resultsText.enableWordWrapping = true;
                _forceButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", new Vector2(59f, -32f), new Vector2(36f, 10f), () => ForceShowButtonPressed?.Invoke(), "Force Show Results");

                _lastSearchButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", new Vector2(59f, -32f), new Vector2(36f, 10f), () => LastSearchButtonPressed?.Invoke(), "<color=#FFFFCC>Redo Last Search</color>");
                (_lastSearchButton as HMUI.NoTransitionsButton).selectionStateDidChangeEvent += delegate (HMUI.NoTransitionsButton.SelectionState selectionState)
                {
                    var text = _lastSearchButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (selectionState == HMUI.NoTransitionsButton.SelectionState.Highlighted)
                        text.text = "<color=#444400>Redo Last Search</color>";
                    else
                        text.text = "<color=#FFFFCC>Redo Last Search</color>";
                };
                _lastSearchText = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(23f, -31.5f), new Vector2(30f, 8f));
                _lastSearchText.fontSize = 3.5f;
                _lastSearchText.color = new Color(1f, 1f, 1f, 0.3f);
                _lastSearchText.alignment = TextAlignmentOptions.TopRight;
            }
            else
            {
                _resultsText.text = _placeholderResultsText;
                _resultsText.fontSize = 6f;
            }

            AdjustElements();

            _loadingSpinner.SetActive(false);
            _resultsText.gameObject.SetActive(!PluginConfig.CompactSearchMode);
            _forceButton.gameObject.SetActive(false);
            _lastSearchButton.gameObject.SetActive(false);
            _lastSearchText.gameObject.SetActive(false);
            SetHeaderActive(!PluginConfig.CompactSearchMode);
        }

        public void ShowLoadingSpinner()
        {
            _resultsText.gameObject.SetActive(false);
            _forceButton.gameObject.SetActive(false);
            _lastSearchButton.gameObject.SetActive(false);
            _lastSearchText.gameObject.SetActive(false);

            SetHeaderActive(!PluginConfig.CompactSearchMode);
            _loadingSpinner.SetActive(true);
        }

        public void ShowPlaceholderText()
        {
            _loadingSpinner.SetActive(false);
            _forceButton.gameObject.SetActive(false);
            _lastSearchButton.gameObject.SetActive(false);
            _lastSearchText.gameObject.SetActive(false);

            _resultsText.text = _placeholderResultsText;
            _resultsText.gameObject.SetActive(true);
            SetHeaderActive(true);
        }

        public void ShowResults(string searchQuery, IPreviewBeatmapLevel[] searchResultsList, int searchSpaceSize)
        {
            _loadingSpinner.SetActive(false);

            string color = searchResultsList.Count() > 0 ? "#FFFF55" : "#FF2222";

            // NOTE: unsanitized user input (Pog)
            _resultsText.text = $"<color={color}>{searchResultsList.Count()} out of {searchSpaceSize}</color> beatmaps\n" +
                $"contain the text \"<color=#11FF11>{searchQuery}</color>\"";
            _resultsText.gameObject.SetActive(true);

            _forceButton.interactable = searchResultsList.Any() ? true : false;
            _forceButton.gameObject.SetActive(true);
            SetHeaderActive(!PluginConfig.CompactSearchMode);
        }

        public void HideUIElements()
        {
            SetHeaderActive(false);
            _loadingSpinner.SetActive(false);
            _resultsText.gameObject.SetActive(false);
            _forceButton.gameObject.SetActive(false);
            _lastSearchButton.gameObject.SetActive(false);
            _lastSearchText.gameObject.SetActive(false);
        }

        private void SetHeaderActive(bool active)
        {
            _header.SetActive(active);

            // text is always getting reset to "Player Settings" when header becomes active, so we have to set it again
            if (active)
            {
                TextMeshProUGUI titleText = _header.GetComponentInChildren<TextMeshProUGUI>(true);
                titleText.text = _headerText;
            }
        }

        /// <summary>
        /// Used to adjust elements after changing to or from compact mode.
        /// </summary>
        public void AdjustElements()
        {
            if (PluginConfig.CompactSearchMode)
            {
                _resultsText.rectTransform.anchoredPosition = ResultsTextCompactAnchoredPosition;
                _resultsText.rectTransform.sizeDelta = ResultsTextCompactSizeDelta;
                _resultsText.fontSize = ResultsTextCompactFontSize;

                (_loadingSpinner.transform as RectTransform).anchoredPosition = LoadingSpinnerCompactAnchoredPosition;
            }
            else
            {
                _resultsText.rectTransform.anchoredPosition = ResultsTextDefaultAnchoredPosition;
                _resultsText.rectTransform.sizeDelta = ResultsTextDefaultSizeDelta;
                _resultsText.fontSize = ResultsTextDefaultFontSize;

                (_loadingSpinner.transform as RectTransform).anchoredPosition = LoadingSpinnerDefaultAnchoredPosition;
            }
        }

        /// <summary>
        /// Should only be used when there was a non-empty search before.
        /// </summary>
        /// <param name="show">A boolean representing whether to show the last search button and text.</param>
        /// <param name="lastQuery">The last search query to display.</param>
        public void ShowLastSearchButton(bool show, string lastQuery = null)
        {
            _lastSearchButton.gameObject.SetActive(show);
            _lastSearchText.gameObject.SetActive(show);

            if (show && lastQuery != null)
                _lastSearchText.SetText($"<line-height=85%><u>Last Search</u>\n</line-height><color=#FFFFCC>\"{lastQuery}\"</color>");
        }
    }
}
