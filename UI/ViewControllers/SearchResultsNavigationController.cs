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

        private GameObject _header;
        private GameObject _loadingSpinner;
        private TextMeshProUGUI _resultsText;
        private Button _forceButton;

        private const string _headerText = "Search Results";
        private const string _placeholderResultsText = "Use the keyboard on the right screen\nto search for a song.\n\n---->";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                BeatSaberUI.CreateBackButton(this.rectTransform, () => BackButtonPressed?.Invoke());
                _loadingSpinner = BeatSaberUI.CreateLoadingSpinner(this.rectTransform);

                var headerRectTransform = Instantiate(Resources.FindObjectsOfTypeAll<RectTransform>()
                    .First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), this.rectTransform);
                _header = headerRectTransform.gameObject;

                _resultsText = BeatSaberUI.CreateText(this.rectTransform, _placeholderResultsText, Vector2.zero, new Vector2(120f, 60f));
                _resultsText.alignment = TextAlignmentOptions.Center;
                _resultsText.enableWordWrapping = true;
                _resultsText.fontSize = 6f;
                BeatSaberUI.CreateBackButton(this.rectTransform, () => BackButtonPressed?.Invoke());
                _loadingSpinner = BeatSaberUI.CreateLoadingSpinner(this.rectTransform);
                _forceButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", new Vector2(59f, -32f), new Vector2(36f, 10f), () => ForceShowButtonPressed?.Invoke(), "Force Show Results");
            }
            else
            {
                _resultsText.text = _placeholderResultsText;
                _resultsText.fontSize = 8f;
            }

            _loadingSpinner.SetActive(false);
            _resultsText.gameObject.SetActive(true);
            _forceButton.gameObject.SetActive(false);
            SetHeaderActive(true);
        }

        public void ShowLoadingSpinner()
        {
            _resultsText.gameObject.SetActive(false);
            _forceButton.gameObject.SetActive(false);

            SetHeaderActive(true);
            _loadingSpinner.SetActive(true);
        }

        public void ShowPlaceholderText()
        {
            _loadingSpinner.SetActive(false);
            _forceButton.gameObject.SetActive(false);

            _resultsText.text = _placeholderResultsText;
            _resultsText.gameObject.SetActive(true);
            SetHeaderActive(true);
        }

        public void ShowResults(string searchQuery, IPreviewBeatmapLevel[] searchResultsList, int searchSpaceSize)
        {
            _loadingSpinner.SetActive(false);

            string color = searchResultsList.Count() > 0 ? "#FFFF55" : "#FF2222";

            _resultsText.text = $"<color={color}>{searchResultsList.Count()} out of {searchSpaceSize}</color> beatmaps\n" +
                $"contain the text \"<color=#11FF11>{searchQuery}</color>\"";
            _resultsText.gameObject.SetActive(true);
            _forceButton.gameObject.SetActive(true);
            SetHeaderActive(true);
        }

        public void HideUIElements()
        {
            SetHeaderActive(false);
            _loadingSpinner.SetActive(false);
            _resultsText.gameObject.SetActive(false);
            _forceButton.gameObject.SetActive(false);
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
    }
}
