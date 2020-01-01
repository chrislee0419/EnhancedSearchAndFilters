using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Screen = HMUI.Screen;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI
{
    class ButtonPanel : PersistentSingleton<ButtonPanel>
    {
        public event Action SearchButtonPressed;
        public event Action FilterButtonPressed;
        public event Action ClearFilterButtonPressed;

        private GameObject _container;
        private bool _initialized = false;
        private bool _areFiltersApplied = false;

#pragma warning disable CS0649
#pragma warning disable CS0414
#pragma warning disable CS0169
        [UIValue("hide-search")]
        private bool _hideSearchButton;

        [UIValue("hide-filters")]
        private bool _hideFilterButtons;

        [UIComponent("search-button")]
        private Button _searchButton;
        [UIComponent("filter-button")]
        private Button _filterButton;
        [UIComponent("clear-filter-button")]
        private Button _clearFilterButton;
#pragma warning restore CS0649
#pragma warning restore CS0414
#pragma warning restore CS0169

        [UIValue("filter-button-default-text")]
        private const string FilterButtonText = "<color=#FFFFCC>Filter</color>";
        [UIValue("clear-filter-button-default-text")]
        private const string ClearFilterButtonText = "<color=#FFFFCC>Clear Filters</color>";

        private const string FilterButtonHighlightedText = "<color=#444400>Filter</color>";
        private const string FilterButtonAppliedText = "<color=#DDFFDD>Filter (Applied)</color>";
        private const string FilterButtonHighlightedAppliedText = "<color=#004400>Filter (Applied)</color>";
        private const string ClearFilterButtonHighlightedText = "<color=#444400>Clear Filters</color>";
        private const string ClearFilterButtonAppliedText = "<color=#FFDDDD>Clear Filters</color>";
        private const string ClearFilterButtonHighlightedAppliedText = "<color=#440000>Clear Filters</color>";

        private const float DefaultYScale = 0.02f;
        private const float HiddenYScale = 0f;

        public void Setup(bool hideSearchButton = false, bool hideFilterButtons = false, bool forceReinit = false)
        {
            if (_initialized)
            {
                if (!forceReinit)
                    return;

                DestroyImmediate(_container);
                _container = null;
            }

            if (hideSearchButton && hideFilterButtons)
                return;

            var topScreen = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "TopScreen");

            _container = Instantiate(topScreen, topScreen.transform.parent, true);
            _container.name = "EnhancedSearchAndFiltersButtonPanel";

            Destroy(_container.GetComponentInChildren<SetMainCameraToCanvas>(true));
            Destroy(_container.transform.Find("TitleViewController"));
            Destroy(_container.GetComponentInChildren<Screen>(true));
            Destroy(_container.GetComponentInChildren<HorizontalLayoutGroup>(true));

            // position the screen
            var rt = _container.transform as RectTransform;
            rt.sizeDelta = new Vector2(28f, 30f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(1.6f, 2.44f);
            rt.localRotation = Quaternion.Euler(345f, 0f, 0f);

            _hideSearchButton = hideSearchButton;
            _hideFilterButtons = hideFilterButtons;
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.ButtonPanel.bsml"), _container, this);

            // replace the ugly looking button with the marginally better looking keyboard button (RoundRectBig)
            var replacementButtonImage = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton").GetComponentInChildren<Image>().sprite;
            var buttonBg = _searchButton?.GetComponentInChildren<Image>();
            if (buttonBg != null)
                buttonBg.sprite = replacementButtonImage;
            buttonBg = _filterButton?.GetComponentInChildren<Image>();
            if (buttonBg != null)
                buttonBg.sprite = replacementButtonImage;
            buttonBg = _clearFilterButton?.GetComponentInChildren<Image>();
            if (buttonBg != null)
                buttonBg.sprite = replacementButtonImage;

            // add ability to check pointer enter/exit events to filter buttons to change colour
            if (_filterButton != null)
            {
                _filterButton.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _filterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonHighlightedAppliedText : FilterButtonHighlightedText);
                handler.PointerExited += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonAppliedText : FilterButtonText);
            }
            if (_clearFilterButton != null)
            {
                _clearFilterButton?.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _clearFilterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText);
                handler.PointerExited += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonAppliedText : ClearFilterButtonText);
            }

            _initialized = true;

            HidePanel(true);
        }

        [UIAction("search-button-clicked")]
        private void OnSearchButtonClicked()
        {
            Logger.log.Debug("Search button presssed");
            SearchButtonPressed?.Invoke();
        }

        [UIAction("filter-button-clicked")]
        private void OnFilterButtonClicked()
        {
            Logger.log.Debug("Filter button pressed");
            FilterButtonPressed?.Invoke();
        }

        [UIAction("clear-filter-button-clicked")]
        private void OnClearFilterButtonClicked()
        {
            Logger.log.Debug("Clear Filter button pressed");
            ClearFilterButtonPressed?.Invoke();
        }

        public void ShowPanel(bool immediately = false)
        {
            if (!_initialized || _container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = DefaultYScale;
                this._container.transform.localScale = localScale;

                _container.SetActive(true);
                return;
            }

            _container.SetActive(true);

            StopAllCoroutines();
            StartCoroutine(AnimationCoroutine(DefaultYScale));
        }

        public void HidePanel(bool immediately = false)
        {
            if (!_initialized || !_container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = HiddenYScale;
                this._container.transform.localScale = localScale;

                _container.SetActive(false);

                return;
            }

            StopAllCoroutines();
            StartCoroutine(AnimationCoroutine(HiddenYScale, true));
        }

        private IEnumerator AnimationCoroutine(float destAnimationValue, bool disableOnFinish = false)
        {
            yield return null;
            yield return null;

            Vector3 localScale = this._container.transform.localScale;
            while (Mathf.Abs(localScale.y - destAnimationValue) > 0.0001f)
            {
                float num = (localScale.y > destAnimationValue) ? 30f : 16f;
                localScale.y = Mathf.Lerp(localScale.y, destAnimationValue, Time.deltaTime * num);
                this._container.transform.localScale = localScale;

                yield return null;
            }

            localScale.y = destAnimationValue;
            this._container.transform.localScale = localScale;

            _container.SetActive(!disableOnFinish);
        }

        public void SetFilterStatus(bool filterApplied)
        {
            if (!_initialized)
                return;

            _areFiltersApplied = filterApplied;

            if (_filterButton != null)
                _filterButton.SetButtonText(filterApplied ? FilterButtonAppliedText : FilterButtonText);

            if (_clearFilterButton != null)
            {
                if (_clearFilterButton.GetComponent<EnterExitEventHandler>().IsPointedAt)
                    _clearFilterButton.SetButtonText(filterApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText);
                else
                    _clearFilterButton.SetButtonText(filterApplied ? ClearFilterButtonAppliedText : ClearFilterButtonText);
            }
        }
    }
}
