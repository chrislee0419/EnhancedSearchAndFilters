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

        private IEnumerator _applyAnimation;
        private IEnumerator _scrollAnimation;

        private QuickFilter _currentQuickFilter;

        private TextMeshProUGUI _text;
        private Image _strokeImage;
        private float _maxTextWidth;

        private const string NoQuickFiltersAvailableText = "<color=#FF9999>None Available</color>";
        private static readonly Color AppliedButtonColour = new Color(0.2f, 1f, 0.2f);
        private const float ApplyAnimationDurationSeconds = 3f;

        private const float TextScrollAnimationScaleThreshold = 1.2f;
        private const float TextScrollAnimationDurationSeconds = 2f;
        private const float TextFadeAnimationDurationSeconds = 0.5f;
        private static readonly WaitForSeconds TextScrollAnimationWait = new WaitForSeconds(TextScrollAnimationDurationSeconds);

        private void Awake()
        {
            RectTransform = this.transform as RectTransform;
        }

        private void Start()
        {
            Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.ButtonPanelModules.QuickFilterView.bsml", this.gameObject, this);

            Utilities.ScaleButton(_quickFilterButton, 0.5f);

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
            var handler = _quickFilterButton.gameObject.AddComponent<EnterExitEventHandler>();
            handler.PointerEntered += () => _text.color = Color.black;
            handler.PointerExited += () => _text.color = Color.white;

            RefreshUI();
            ResetButton();
        }

        /// <summary>
        /// Refreshs the UI after the global list of quick filters has been changed.
        /// </summary>
        public void RefreshUI()
        {
            var quickFiltersList = QuickFiltersManager.QuickFiltersList;
            if (quickFiltersList.Count == 0)
            {
                _currentQuickFilter = null;
                PreviousButtonInteractable = false;
                NextButtonInteractable = false;
                QuickFilterButtonInteractable = false;
                SetText(NoQuickFiltersAvailableText);
            }
            else if (_currentQuickFilter == null || !quickFiltersList.Contains(_currentQuickFilter))
            {
                _currentQuickFilter = quickFiltersList.First();
                PreviousButtonInteractable = false;
                NextButtonInteractable = quickFiltersList.Count > 1;
                QuickFilterButtonInteractable = true;
                SetText(_currentQuickFilter.Name);
            }
        }

        #region BSML Actions
        [UIAction("previous-button-clicked")]
        private void OnPreviousButtonClicked()
        {
            if (_applyAnimation != null)
            {
                StopAllCoroutines();
                ResetButton();
            }

            int index = QuickFiltersManager.IndexOf(_currentQuickFilter);
            if (index < 0)
            {
                RefreshUI();
                return;
            }

            int prevIndex = index - 1;
            if (prevIndex >= 0)
            {
                _currentQuickFilter = QuickFiltersManager.QuickFiltersList[prevIndex];
                SetText(_currentQuickFilter.Name);
            }

            PreviousButtonInteractable = prevIndex > 0;
            NextButtonInteractable = prevIndex < QuickFiltersManager.Count - 1;
        }

        [UIAction("next-button-clicked")]
        private void OnNextButtonClicked()
        {
            if (_applyAnimation != null)
            {
                StopCoroutine(_applyAnimation);
                _applyAnimation = null;
                ResetButton();
            }

            int index = QuickFiltersManager.IndexOf(_currentQuickFilter);
            if (index < 0)
            {
                RefreshUI();
                return;
            }

            int nextIndex = index + 1;
            if (nextIndex < QuickFiltersManager.Count)
            {
                _currentQuickFilter = QuickFiltersManager.QuickFiltersList[nextIndex];
                SetText(_currentQuickFilter.Name);
            }

            PreviousButtonInteractable = nextIndex > 0;
            NextButtonInteractable = nextIndex < QuickFiltersManager.Count - 1;
        }

        [UIAction("quick-filter-button-clicked")]
        private void OnQuickFilterButtonClicked()
        {
            if (_applyAnimation != null)
                return;
            StartCoroutine(QuickFilterAppliedAnimationCoroutine());

            // let the installed delegate handle applying the quick filter
            if (_currentQuickFilter != null)
                ApplyQuickFilterPressed?.Invoke(_currentQuickFilter);
        }
        #endregion

        private IEnumerator QuickFilterAppliedAnimationCoroutine()
        {
            float seconds = 0;
            _text.text = "Applied!";
            _strokeImage.color = AppliedButtonColour;

            yield return null;
            while (seconds < ApplyAnimationDurationSeconds)
            {
                Color currentColour = new Color();
                float ratio = seconds / ApplyAnimationDurationSeconds;

                currentColour.r = Mathf.Lerp(AppliedButtonColour.r, Color.white.r, ratio);
                currentColour.g = Mathf.Lerp(AppliedButtonColour.g, Color.white.g, ratio);
                currentColour.b = Mathf.Lerp(AppliedButtonColour.b, Color.white.b, ratio);
                currentColour.a = 1f;
                _strokeImage.color = currentColour;

                seconds += Time.deltaTime;
                yield return null;
            }

            _applyAnimation = null;
            ResetButton();
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

            // only use animation if the requested text is a lot bigger,
            // otherwise, just scale it
            float sizeRatio = textWidth / _maxTextWidth;
            if (sizeRatio < TextScrollAnimationScaleThreshold && sizeRatio > 1f)
            {
                _text.text = $"<size={(100f / sizeRatio).ToString("N0")}%>" + text + "</size>";
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
            Vector2 currentPos = new Vector2();

            while (true)
            {
                rt.anchoredPosition = startPos;

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

                seconds = 0;
                while (seconds < TextScrollAnimationDurationSeconds)
                {
                    currentPos.x = Mathf.Lerp(startPos.x, endPos.x, seconds / TextScrollAnimationDurationSeconds);
                    rt.anchoredPosition = currentPos;

                    seconds += Time.deltaTime;
                    yield return null;
                }

                rt.anchoredPosition = endPos;
                yield return TextScrollAnimationWait;

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
