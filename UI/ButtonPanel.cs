using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Screen = HMUI.Screen;
using EnhancedSearchAndFilters.UI.Components;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.UI
{
    class ButtonPanel : PersistentSingleton<ButtonPanel>
    {
        public event Action SearchButtonPressed;
        public event Action FilterButtonPressed;
        public event Action ClearFilterButtonPressed;
        public event Action SortButtonPressed;

        public bool Initialized { get; private set; } = false;

        private GameObject _container;
        private bool _areFiltersApplied = false;
        private bool _inRevealAnimation = false;
        private bool _inExpandAnimation = false;

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

        [UIComponent("default-sort-button")]
        private Button _defaultSortButton;
        [UIComponent("newest-sort-button")]
        private Button _newestSortButton;
        [UIComponent("play-count-sort-button")]
        private Button _playCountSortButton;
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

        private static readonly Color DefaultSortButtonColor = Color.white;
        private static readonly Color SelectedSortButtonColor = new Color(0.7f, 1f, 0.6f);
        private static readonly Color SelectedReversedSortButtonColor = new Color(0.7f, 0.6f, 1f);

        private const float DefaultYScale = 0.02f;
        private const float HiddenYScale = 0f;

        private const float DefaultXSize = 28f;
        private const float ExpandedXSize = 56f;

        public void Setup(bool hideSearchButton = false, bool hideFilterButtons = false, bool forceReinit = false)
        {
            if (Initialized)
            {
                if (!forceReinit)
                    return;

                if (_container != null)
                {
                    DestroyImmediate(_container);
                    _container = null;

                    _searchButton = null;
                    _filterButton = null;
                    _clearFilterButton = null;
                    _defaultSortButton = null;
                    _newestSortButton = null;
                    _playCountSortButton = null;
                }
            }

            if (hideSearchButton && hideFilterButtons)
                return;

            var topScreen = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "TopScreen");

            _container = Instantiate(topScreen, topScreen.transform.parent, true);
            _container.name = "EnhancedSearchAndFiltersButtonPanel";
            _container.AddComponent<RectMask2D>();

            // always render this screen in front of the title view controller's screen
            var canvas = _container.GetComponent<Canvas>();
            canvas.sortingOrder += 1;

            // expand screen to reveal sort mode buttons
            var enterExitEventHander = _container.AddComponent<EnterExitEventHandler>();
            enterExitEventHander.PointerEntered += delegate ()
            {
                if (_inRevealAnimation)
                    return;
                else if (_inExpandAnimation)
                    StopAllCoroutines();

                _inExpandAnimation = true;
                StartCoroutine(ExpandAnimationCoroutine(ExpandedXSize));
            };
            enterExitEventHander.PointerExited += delegate ()
            {
                if (_inRevealAnimation)
                    return;
                else if (_inExpandAnimation)
                    StopAllCoroutines();

                _inExpandAnimation = true;
                StartCoroutine(ExpandAnimationCoroutine(DefaultXSize));
            };

            Destroy(_container.GetComponentInChildren<SetMainCameraToCanvas>(true));
            Destroy(_container.transform.Find("TitleViewController").gameObject);
            Destroy(_container.GetComponentInChildren<Screen>(true));
            Destroy(_container.GetComponentInChildren<HorizontalLayoutGroup>(true));

            // position the screen
            var rt = _container.transform as RectTransform;
            rt.sizeDelta = new Vector2(DefaultXSize, 30f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(1.6f, 2.44f);
            rt.localRotation = Quaternion.Euler(345f, 0f, 0f);
            rt.localPosition += new Vector3(0f, 0f, -0.001f);

            _hideSearchButton = hideSearchButton;
            _hideFilterButtons = hideFilterButtons;
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.ButtonPanelView.bsml"), _container, this);

            // add ability to check pointer enter/exit events to filter/clear buttons to change colour
            if (_filterButton != null)
            {
                _filterButton.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _filterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonHighlightedAppliedText : FilterButtonHighlightedText);
                handler.PointerExited += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonAppliedText : FilterButtonText);
            }
            if (_clearFilterButton != null)
            {
                _clearFilterButton.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _clearFilterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText);
                handler.PointerExited += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonAppliedText : ClearFilterButtonText);
            }

            _defaultSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = SelectedSortButtonColor;

            Initialized = true;

            HidePanel(true);
        }

        public void DisablePanel()
        {
            if (!Initialized)
                return;

            if (_container != null)
            {
                DestroyImmediate(_container);
                _container = null;
            }

            Initialized = false;
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
            SetFilterStatus(false);
        }

        [UIAction("default-sort-button-clicked")]
        private void OnDefaultSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.Default;
            UpdateSortButtons();
            Logger.log.Debug("Default sort button pressed");

            SortButtonPressed?.Invoke();
        }

        [UIAction("newest-sort-button-clicked")]
        private void OnNewestSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.Newest;
            UpdateSortButtons();
            Logger.log.Debug("Newest sort button pressed");

            SortButtonPressed?.Invoke();
        }

        [UIAction("play-count-sort-button-clicked")]
        private void OnPlayCountSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.PlayCount;
            UpdateSortButtons();
            Logger.log.Debug("Play count sort button pressed");

            SortButtonPressed?.Invoke();
        }

        public void ShowPanel(bool immediately = false)
        {
            if (!Initialized || _container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = DefaultYScale;
                this._container.transform.localScale = localScale;

                // reset size delta as well
                var rt = (this._container.transform as RectTransform);
                var sizeDelta = rt.sizeDelta;
                sizeDelta.x = DefaultXSize;
                rt.sizeDelta = sizeDelta;

                _container.SetActive(true);
                return;
            }

            _container.SetActive(true);

            _inRevealAnimation = true;
            StopAllCoroutines();
            StartCoroutine(RevealAnimationCoroutine(DefaultYScale));
        }

        public void HidePanel(bool immediately = false)
        {
            if (!Initialized || !_container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = HiddenYScale;
                this._container.transform.localScale = localScale;

                // reset size delta as well
                var rt = (this._container.transform as RectTransform);
                var sizeDelta = rt.sizeDelta;
                sizeDelta.x = DefaultXSize;
                rt.sizeDelta = sizeDelta;

                _container.SetActive(false);

                return;
            }

            _inRevealAnimation = true;
            StopAllCoroutines();
            StartCoroutine(RevealAnimationCoroutine(HiddenYScale, true));
        }

        private IEnumerator RevealAnimationCoroutine(float destAnimationValue, bool disableOnFinish = false)
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

            // reset size delta as well
            var rt = (this._container.transform as RectTransform);
            var sizeDelta = rt.sizeDelta;
            sizeDelta.x = DefaultXSize;
            rt.sizeDelta = sizeDelta;

            _container.SetActive(!disableOnFinish);
            _inRevealAnimation = false;
        }

        private IEnumerator ExpandAnimationCoroutine(float destAnimationValue)
        {
            RectTransform rt = this._container.transform as RectTransform;
            Vector3 sizeDelta = rt.sizeDelta;

            while (Mathf.Abs(sizeDelta.x - destAnimationValue) > 0.0001f)
            {
                sizeDelta.x = Mathf.Lerp(sizeDelta.x, destAnimationValue, Time.deltaTime * 30);
                rt.sizeDelta = sizeDelta;

                yield return null;
            }

            sizeDelta.x = destAnimationValue;
            rt.sizeDelta = sizeDelta;

            _inExpandAnimation = false;
        }

        public void SetFilterStatus(bool filterApplied)
        {
            if (!Initialized)
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

        public void UpdateSortButtons()
        {
            if (!Initialized)
                return;

            switch (SongSortModule.CurrentSortMode)
            {
                case SortMode.Default:
                    _defaultSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    _newestSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    _playCountSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    break;

                case SortMode.Newest:
                    _defaultSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    _newestSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    _playCountSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    break;

                case SortMode.PlayCount:
                    _defaultSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    _newestSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = DefaultSortButtonColor;
                    _playCountSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    break;
            }
        }
    }
}
