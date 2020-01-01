using System;
using System.Reflection;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.UI.Components;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchResultsNavigationController : NavigationController
    {
        public event Action ForceShowButtonPressed;
        public event Action LastSearchButtonPressed;

#pragma warning disable CS0649
        [UIObject("header")]
        private GameObject _header;

        [UIComponent("results-text")]
        private TextMeshProUGUI _resultsText;
        [UIComponent("force-search-button")]
        private Button _forceButton;
        [UIComponent("last-search-button")]
        private Button _lastSearchButton;
        [UIComponent("last-search-text")]
        private TextMeshProUGUI _lastSearchText;
#pragma warning restore CS0649

        private GameObject _loadingSpinner;

        private SongPreviewPlayer _songPreviewPlayer;
        private string _songPreviewPlayerCrossfadingLevelID;
        private CancellationTokenSource _cancellationTokenSource;

        [UIValue("results-text-placeholder")]
        private const string _placeholderResultsText = "Use the keyboard on the right screen\nto search for a song.\n\n---->";

        private static readonly Vector2 ResultsTextDefaultAnchoredPosition = Vector2.zero;
        private static readonly Vector2 ResultsTextDefaultSizeDelta = new Vector2(120f, 40f);
        private const float ResultsTextDefaultFontSize = 6f;
        private static readonly Vector2 LoadingSpinnerDefaultAnchoredPosition = Vector2.zero;

        private static readonly Vector2 ResultsTextCompactAnchoredPosition = new Vector2(-35f, 0f);
        private static readonly Vector2 ResultsTextCompactSizeDelta = new Vector2(50f, 40f);
        private const float ResultsTextCompactFontSize = 5f;
        private static readonly Vector2 LoadingSpinnerCompactAnchoredPosition = new Vector2(-35f, 0f);

        [UIValue("redo-search-button-default-text")]
        private const string RedoSearchButtonDefaultText = "<color=#FFFFCC>Redo Last Search</color>";
        private const string RedoSearchButtonHighlightedText = "<color=#444400>Redo Last Search</color>";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.SearchResultsNavigationView.bsml"), this.gameObject, this);
                name = "SearchResultsNavigationController";

                _loadingSpinner = Utilities.CreateLoadingSpinner(this.rectTransform);

                _resultsText.enableWordWrapping = true;

                var handler = _lastSearchButton.gameObject.AddComponent<EnterExitEventHandler>();
                handler.PointerEntered += () => _lastSearchButton.SetButtonText(RedoSearchButtonHighlightedText);
                handler.PointerExited += () => _lastSearchButton.SetButtonText(RedoSearchButtonDefaultText);

                _lastSearchText.color = new Color(1f, 1f, 1f, 0.3f);

                _songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();
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

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            CrossfadeAudioToDefault();
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

            //// text is always getting reset to "Player Settings" when header becomes active, so we have to set it again
            //if (active)
            //{
            //    TextMeshProUGUI titleText = _header.GetComponentInChildren<TextMeshProUGUI>(true);
            //    titleText.text = _headerText;
            //}
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

        /// <summary>
        /// Play the preview audio of the provided preview beatmap level.
        /// </summary>
        /// <param name="level">The level to play the audio from.</param>
        public async void CrossfadeAudioToLevelAsync(IPreviewBeatmapLevel level)
        {
            if (_songPreviewPlayerCrossfadingLevelID != level.levelID)
            {
                try
                {
                    _songPreviewPlayerCrossfadingLevelID = level.levelID;

                    if (_cancellationTokenSource != null)
                        _cancellationTokenSource.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();
                    CancellationToken token = _cancellationTokenSource.Token;

                    AudioClip audio = await level.GetPreviewAudioClipAsync(token);
                    token.ThrowIfCancellationRequested();

                    _songPreviewPlayer.CrossfadeTo(audio, level.previewStartTime, level.previewDuration);
                }
                catch (OperationCanceledException)
                {
                    if (_songPreviewPlayerCrossfadingLevelID == level.levelID)
                        _songPreviewPlayerCrossfadingLevelID = null;
                }
            }
        }

        /// <summary>
        /// Stop playing level preview audio.
        /// </summary>
        public void CrossfadeAudioToDefault()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
            _songPreviewPlayer.CrossfadeToDefault();
            _songPreviewPlayerCrossfadingLevelID = null;
        }

        [UIAction("force-search-button-clicked")]
        private void OnForceSearchButtonClicked() => ForceShowButtonPressed?.Invoke();

        [UIAction("last-search-button-clicked")]
        private void OnLastSearchButtonClicked() => LastSearchButtonPressed?.Invoke();
    }
}
