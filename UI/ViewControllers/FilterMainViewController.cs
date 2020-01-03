using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using EnhancedSearchAndFilters.Filters;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class FilterMainViewController : BSMLResourceViewController
    {
        public override string ResourceName => "EnhancedSearchAndFilters.UI.Views.FilterMainView.bsml";

        public event Action ApplyButtonPressed;
        public event Action UnapplyButtonPressed;
        public event Action ClearButtonPressed;
        public event Action DefaultButtonPressed;

#pragma warning disable CS0649
        [UIObject("view-container")]
        private GameObject _viewContainer;
        [UIObject("loading-view-root")]
        private GameObject _loadingView;

        private GameObject _currentView;

        [UIComponent("default-button")]
        private Button _defaultButton;
        [UIComponent("clear-button")]
        private Button _clearButton;
        [UIComponent("apply-button")]
        private Button _applyButton;
        [UIComponent("unapply-button")]
        private Button _unapplyButton;

        [UIComponent("loading-text")]
        private TextMeshProUGUI _loadingDescriptionText;
        [UIComponent("progress-text")]
        private TextMeshProUGUI _loadingProgressText;
        [UIComponent("info-text")]
        private TextMeshProUGUI _infoText;
#pragma warning restore CS0649

        private GameObject _loadingSpinner;

        private int _coroutinesActive = 0;

        private const string FirstLoadText =
            "<color=#FF5555>Loading custom song details for the first time...</color>\n\n" +
            "This first load may take several minutes, depending on the number of custom songs you have\n" +
            "(it usually takes about 10 to 15 seconds for every 100 songs).\n\n" +
            "<color=#CCFFCC>You may back out of this screen and have the loading occur in the background</color>,\n" +
            "however, loading will pause when playing a level.";
        private const string LoadText = "Loading song details...";
        private const float InfoTextDisplayTime = 10f;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            _defaultButton.interactable = false;
            _clearButton.interactable = false;
            _applyButton.interactable = false;
            _unapplyButton.interactable = false;
            _unapplyButton.gameObject.SetActive(false);

            if (firstActivation)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.FilterLoadingView.bsml"), _viewContainer, this);

                var loadingSpinnerPrefab = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "LoadingIndicator").First();
                _loadingSpinner = Instantiate(loadingSpinnerPrefab, _loadingView.transform, false);
                _loadingSpinner.name = "EnhancedFiltersLoadingSpinner";

                var rt = (_loadingSpinner.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0.5f, 0.5f);

                _loadingSpinner.SetActive(true);
            }

            _loadingView.SetActive(false);
            _infoText.gameObject.SetActive(false);
        }

        public void ShowLoadingView()
        {
            if (PluginConfig.ShowFirstTimeLoadingText)
                _loadingDescriptionText.text = FirstLoadText;
            else
                _loadingDescriptionText.text = LoadText;

            if (_currentView != null && _currentView != _loadingView)
                _currentView.SetActive(false);

            _currentView = _loadingView;
            _loadingView.SetActive(true);
        }

        /// <summary>
        /// Set the buttons to be interactable.
        /// </summary>
        /// <param name="applyUnapplyInteractable">Set the apply/unapply button to be interactable.</param>
        /// <param name="clearInteractable">Set the clear button to be interactable.</param>
        /// <param name="defaultInteractable">Set the default button to be interactable</param>
        public void SetButtonInteractivity(bool applyUnapplyInteractable, bool clearInteractable, bool defaultInteractable)
        {
            _applyButton.interactable = applyUnapplyInteractable;
            _unapplyButton.interactable = applyUnapplyInteractable;
            _clearButton.interactable = clearInteractable;
            _defaultButton.interactable = defaultInteractable;
        }

        /// <summary>
        /// Show either the apply or unapply button.
        /// </summary>
        /// <param name="showApplyButton">Set to true to show the apply button and set to false to show the unapply button.</param>
        public void SetApplyUnapplyButton(bool showApplyButton)
        {
            _applyButton.gameObject.SetActive(showApplyButton);
            _unapplyButton.gameObject.SetActive(!showApplyButton);
        }

        public void UpdateLoadingProgressText(int loaded, int total)
        {
            _loadingProgressText.text = $"Loaded {loaded} out of {total} songs...";
        }

        public void ShowFilterContentView(IFilter filter)
        {
            if (_currentView != null)
                _currentView.SetActive(false);

            if (filter.GetView() == null)
                filter.Init(this._viewContainer);

            _currentView = filter.GetView();
            _currentView.SetActive(true);
        }

        public void ShowInfoText(string text)
        {
            _infoText.text = text;
            _infoText.gameObject.SetActive(true);

            StartCoroutine(HideInfoText());
        }

        private IEnumerator HideInfoText()
        {
            ++_coroutinesActive;
            yield return new WaitForSeconds(InfoTextDisplayTime);

            if ((--_coroutinesActive) == 0)
                _infoText.gameObject.SetActive(false);
        }

        [UIAction("default-button-clicked")]
        private void OnDefaultButtonClicked() => DefaultButtonPressed?.Invoke();

        [UIAction("clear-button-clicked")]
        private void OnClearButtonClicked() => ClearButtonPressed?.Invoke();

        [UIAction("apply-button-clicked")]
        private void OnApplyButtonClicked() => ApplyButtonPressed?.Invoke();

        [UIAction("unapply-button-clicked")]
        private void OnUnapplyButtonClicked() => UnapplyButtonPressed?.Invoke();
    }
}
