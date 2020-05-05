using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.UI.Components.ButtonPanelModules
{
    [RequireComponent(typeof(RectTransform))]
    internal class QuickFilterModule : MonoBehaviour, INotifiableHost
    {
        public event Action<QuickFilter> ApplyQuickFilterPressed;
        public event PropertyChangedEventHandler PropertyChanged;

        public RectTransform RectTransform { get; private set; }

        private bool _previousButtonInteractable = false;
        [UIValue("previous-button-interactable")]
        public bool PreviousButtonInteractable
        {
            get => _previousButtonInteractable;
            set
            {
                if (_previousButtonInteractable == value)
                    return;

                _previousButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _nextButtonInteractable = false;
        [UIValue("next-button-interactable")]
        public bool NextButtonInteractable
        {
            get => _nextButtonInteractable;
            set
            {
                if (_nextButtonInteractable == value)
                    return;

                _nextButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool QuickFilterButtonInteractable
        {
            set => _quickFilterButton.interactable = value;
        }

#pragma warning disable CS0649
        [UIComponent("quick-filter-button")]
        private Button _quickFilterButton;
#pragma warning restore CS0649

        private EnterExitEventHandler _hoverEventHandler;

        private IEnumerator _applyAnimation;
        private IEnumerator _scrollAnimation;
        private float _maxTextWidth;

        private QuickFilter _currentQuickFilter;

        private TextMeshProUGUI _text;
        private Image _strokeImage;

        private bool _isInitialized = false;

        [UIValue("quick-filter-loading-text")]
        private const string LoadingText = "<color=#FF8888>Loading...</color>";
        private const string NoQuickFiltersAvailableText = "<color=#FF9999>None Available</color>";
        private static readonly Color AppliedButtonColour = new Color(0.2f, 1f, 0.2f);
        private const float ApplyAnimationDurationSeconds = 3f;

        private const float TextScrollAnimationScaleThreshold = 1.2f;
        private const float TextScrollAnimationSpeed = 20f;
        private const float TextFadeAnimationDurationSeconds = 0.6f;
        private static readonly WaitForSeconds TextScrollAnimationWait = new WaitForSeconds(2f);

        private void Awake()
        {
            RectTransform = this.transform as RectTransform;
        }

        private void Start()
        {
            UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.ButtonPanelModules.QuickFilterModuleView.bsml", this.gameObject, this);

            UIUtilities.ScaleButton(_quickFilterButton, 0.5f);

            _quickFilterButton.interactable = false;
            _text = _quickFilterButton.GetComponentInChildren<TextMeshProUGUI>();
            _strokeImage = _quickFilterButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");

            // set up scrolling text box
            var content = _quickFilterButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            var mask = new GameObject("Mask").AddComponent<RectMask2D>();
            mask.gameObject.AddComponent<LayoutElement>();

            mask.transform.SetParent(content.transform, false);
            _text.transform.SetParent(mask.transform, false);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_quickFilterButton.transform as RectTransform);
            _maxTextWidth = mask.rectTransform.rect.width;

            var rt = _text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 0f);

            // text no longer responds to hover after re-parenting the transform, so change the colour ourselves
            _hoverEventHandler = _quickFilterButton.gameObject.AddComponent<EnterExitEventHandler>();
            _hoverEventHandler.PointerEntered += () => _text.color = Color.black;
            _hoverEventHandler.PointerExited += () => _text.color = Color.white;

            _isInitialized = true;

            // since OnEnable is called before Start, we need to call it now, after every thing is initialized
            OnEnable();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                BeatmapDetailsLoader.instance.CachingStarted += ShowLoadingText;
                BeatmapDetailsLoader.instance.CachingFinished += RefreshUI;

                RefreshUI();
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            _applyAnimation = null;
            _scrollAnimation = null;

            BeatmapDetailsLoader.instance.CachingStarted -= ShowLoadingText;
            BeatmapDetailsLoader.instance.CachingFinished -= RefreshUI;
        }

        /// <summary>
        /// Refreshs the UI after the global list of quick filters has been changed.
        /// </summary>
        public void RefreshUI()
        {
            if (!_isInitialized)
            {
                return;
            }
            else if (BeatmapDetailsLoader.instance.IsCaching)
            {
                ShowLoadingText();
                BeatmapDetailsLoader.instance.CachingFinished -= RefreshUI;
                BeatmapDetailsLoader.instance.CachingFinished += RefreshUI;
                return;
            }

            var quickFiltersList = QuickFiltersManager.QuickFiltersList;
            if (quickFiltersList.Count == 0)
            {
                _currentQuickFilter = null;
                PreviousButtonInteractable = false;
                NextButtonInteractable = false;
            }
            else if (_currentQuickFilter == null || !quickFiltersList.Contains(_currentQuickFilter))
            {
                _currentQuickFilter = quickFiltersList.First();
                PreviousButtonInteractable = false;
                NextButtonInteractable = quickFiltersList.Count > 1;
            }
            else
            {
                int index = QuickFiltersManager.IndexOf(_currentQuickFilter);
                PreviousButtonInteractable = index > 0;
                NextButtonInteractable = index < quickFiltersList.Count - 1;
            }

            ResetButton();
        }

        #region BSML Actions
        [UIAction("previous-button-clicked")]
        private void OnPreviousButtonClicked()
        {
            if (_applyAnimation != null)
            {
                StopCoroutine(_applyAnimation);
                _applyAnimation = null;
            }

            int index = QuickFiltersManager.IndexOf(_currentQuickFilter);
            if (index < 0)
            {
                RefreshUI();
                return;
            }

            int prevIndex = index - 1;
            if (prevIndex >= 0)
                _currentQuickFilter = QuickFiltersManager.QuickFiltersList[prevIndex];
            else
                Logger.log.Warn("Previous button pressed when it wasn't supposed to be able to");

            PreviousButtonInteractable = prevIndex > 0;
            NextButtonInteractable = prevIndex < QuickFiltersManager.Count - 1;
            ResetButton();
        }

        [UIAction("next-button-clicked")]
        private void OnNextButtonClicked()
        {
            if (_applyAnimation != null)
            {
                StopCoroutine(_applyAnimation);
                _applyAnimation = null;
            }

            int index = QuickFiltersManager.IndexOf(_currentQuickFilter);
            if (index < 0)
            {
                RefreshUI();
                return;
            }

            int nextIndex = index + 1;
            if (nextIndex < QuickFiltersManager.Count)
                _currentQuickFilter = QuickFiltersManager.QuickFiltersList[nextIndex];
            else
                Logger.log.Warn("Previous button pressed when it wasn't supposed to be able to");

            PreviousButtonInteractable = nextIndex > 0;
            NextButtonInteractable = nextIndex < QuickFiltersManager.Count - 1;
            ResetButton();
        }

        [UIAction("quick-filter-button-clicked")]
        private void OnQuickFilterButtonClicked()
        {
            if (_applyAnimation != null)
                return;

            _applyAnimation = QuickFilterAppliedAnimationCoroutine();
            StartCoroutine(_applyAnimation);

            // let the installed delegate handle applying the quick filter
            if (_currentQuickFilter != null)
                ApplyQuickFilterPressed?.Invoke(_currentQuickFilter);
        }
        #endregion

        private IEnumerator QuickFilterAppliedAnimationCoroutine()
        {
            const float TextFadeOutRatio = 0.8f;
            const float TextFadeOutRatioRemainder = 0.2f;
            float seconds = 0;
            SetText("Applied!");
            _strokeImage.color = AppliedButtonColour;

            yield return null;
            while (seconds < ApplyAnimationDurationSeconds)
            {
                Color currentColour = new Color();
                float ratio = seconds / ApplyAnimationDurationSeconds;

                // stroke colour
                currentColour.r = Mathf.Lerp(AppliedButtonColour.r, Color.white.r, ratio);
                currentColour.g = Mathf.Lerp(AppliedButtonColour.g, Color.white.g, ratio);
                currentColour.b = Mathf.Lerp(AppliedButtonColour.b, Color.white.b, ratio);
                currentColour.a = 1f;
                _strokeImage.color = currentColour;

                // text colour
                currentColour = _text.color;
                currentColour.a = ratio > TextFadeOutRatio ? Mathf.Lerp(1f, 0f, (ratio - TextFadeOutRatio) / TextFadeOutRatioRemainder) : 1f;
                _text.color = currentColour;

                seconds += Time.deltaTime;
                yield return null;
            }

            _strokeImage.color = Color.white;
            _text.color = _hoverEventHandler.IsPointedAt ? Color.black : Color.white;

            _applyAnimation = null;
            ResetButton();
        }

        private void ShowLoadingText()
        {
            PreviousButtonInteractable = false;
            NextButtonInteractable = false;
            QuickFilterButtonInteractable = false;
            SetText(LoadingText);
        }

        private void ResetButton()
        {
            if (_applyAnimation != null)
                return;

            if (_currentQuickFilter == null)
            {
                QuickFilterButtonInteractable = false;
                SetText(NoQuickFiltersAvailableText);
            }
            else
            {
                QuickFilterButtonInteractable = true;
                SetText(_currentQuickFilter.Name);
            }

            _strokeImage.color = Color.white;
        }

        private void SetText(string text)
        {
            var textWidth = _text.GetPreferredValues(text).x;

            if (_scrollAnimation != null)
            {
                StopCoroutine(_scrollAnimation);
                _scrollAnimation = null;
            }

            _text.color = _hoverEventHandler.IsPointedAt ? Color.black : Color.white;

            // only use animation if the requested text is a lot bigger,
            // otherwise, just scale it
            float sizeRatio = textWidth / _maxTextWidth;
            if (sizeRatio < TextScrollAnimationScaleThreshold && sizeRatio > 1f)
            {
                _text.text = $"<size={(98f / sizeRatio).ToString("N0")}%>" + text + "</size>";
                _text.rectTransform.anchoredPosition = Vector2.zero;
            }
            else if (sizeRatio >= TextScrollAnimationScaleThreshold)
            {
                _text.text = text;
                _scrollAnimation = TextScrollAnimationCoroutine(textWidth);
                StartCoroutine(_scrollAnimation);
            }
            else
            {
                _text.text = text;
                _text.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private IEnumerator TextScrollAnimationCoroutine(float textWidth)
        {
            RectTransform rt = _text.rectTransform;
            float halfWidth = textWidth / 2f;
            float halfMaxWidth = _maxTextWidth / 2f;
            Vector2 startPos = new Vector2(halfWidth - halfMaxWidth + 1, 0f);
            Vector2 endPos = new Vector2(halfMaxWidth - halfWidth - 1, 0f);

            while (true)
            {
                rt.anchoredPosition = startPos;

                // fade in
                float seconds = 0f;
                while (seconds < TextFadeAnimationDurationSeconds)
                {
                    yield return null;

                    Color currentColor = _text.color;
                    currentColor.a = Mathf.Lerp(0f, 1f, seconds / TextFadeAnimationDurationSeconds);
                    _text.color = currentColor;

                    seconds += Time.deltaTime;
                }
                yield return TextScrollAnimationWait;

                // scroll
                Vector2 nextPos = startPos;
                nextPos.x -= Time.deltaTime * 20f;
                while (nextPos.x > endPos.x)
                {
                    rt.anchoredPosition = nextPos;
                    yield return null;
                    nextPos.x -= Time.deltaTime * TextScrollAnimationSpeed;
                }

                rt.anchoredPosition = endPos;
                yield return TextScrollAnimationWait;

                // fade out
                seconds = 0f;
                while (seconds < TextFadeAnimationDurationSeconds)
                {
                    Color currentColor = _text.color;
                    currentColor.a = Mathf.Lerp(1f, 0f, seconds / TextFadeAnimationDurationSeconds);
                    _text.color = currentColor;

                    seconds += Time.deltaTime;
                    yield return null;
                }
            }
        }

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
