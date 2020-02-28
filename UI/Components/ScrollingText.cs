using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage;

namespace EnhancedSearchAndFilters.UI.Components
{
    [RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
    internal class ScrollingText : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }
        public TextMeshProUGUI TextComponent { get; private set; }
        public float FontSize
        {
            get => TextComponent.fontSize;
            set
            {
                TextComponent.fontSize = value;
                _isTextWidthDirty = true;

                // recalculate text width
                Text = _text;
            }
        }
        public TextAlignmentOptions _alignment;
        public TextAlignmentOptions Alignment
        {
            get => TextComponent.alignment;
            set
            {
                _alignment = value;

                // don't modify the alignment when the text is in scrolling animation
                if (_scrollAnimation != null)
                    TextComponent.alignment = value;
            }
        }
        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                RecalculateElements();
            }
        }

        private bool _isTextWidthDirty = true;
        private float _precalculatedTextWidth;
        private float TextWidth
        {
            get
            {
                if (_isTextWidthDirty)
                {
                    _precalculatedTextWidth = TextComponent.GetPreferredValues(_text).x;
                    _isTextWidthDirty = false;
                }
                return _precalculatedTextWidth;
            }
        }
        private float SizeRatio => RectTransform.rect.width != 0f ? TextWidth / RectTransform.rect.width : 0f;

        public ScrollMovementType MovementType { get; set; } = ScrollMovementType.ByDuration;
        public ScrollAnimationType AnimationType { get; set; } = ScrollAnimationType.Basic;
        private float _textWidthRatioThreshold = 1.2f;
        /// <summary>
        /// The minimum ratio of text width to container width before scrolling occurs. 
        /// Otherwise, if the text is wider than the container, the text will be scaled down to fit the container.
        /// </summary>
        public float TextWidthRatioThreshold
        {
            get => _textWidthRatioThreshold;
            set
            {
                if (_textWidthRatioThreshold == value || value < 1f)
                    return;

                _textWidthRatioThreshold = value;

                // determine whether to start animation with new threshold
                Text = _text;
            }
        }
        private WaitForSeconds _pauseDuration = new WaitForSeconds(2f);
        /// <summary>
        /// The number of seconds to wait before the animation starts.
        /// </summary>
        public float PauseDuration
        {
            set
            {
                if (value > 0f)
                    _pauseDuration = new WaitForSeconds(value);
            }
        }
        private float _scrollDuration = 2f;
        /// <summary>
        /// The number of seconds it takes to scroll the text.
        /// </summary>
        public float ScrollDuration
        {
            get => _scrollDuration;
            set
            {
                if (value > 0f)
                    _scrollDuration = value;
            }
        }
        private float _scrollSpeed = 20f;
        /// <summary>
        /// The speed at which the text scrolls in units per second.
        /// </summary>
        public float ScrollSpeed
        {
            get => _scrollSpeed;
            set
            {
                if (value > 0f)
                    _scrollSpeed = value;
            }
        }
        public bool AlwaysScroll { get; set; } = false;

        private IEnumerator _scrollAnimation;

        private const float ScalingMinimumSizeRatio = 0.98f;
        private const float FadeDurationSeconds = 0.6f;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            TextComponent = BeatSaberUI.CreateText(RectTransform, "", Vector2.zero);
            _alignment = TextComponent.alignment;

            var rt = TextComponent.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
        }

        private void OnEnable()
        {
            if (SizeRatio >= _textWidthRatioThreshold || AlwaysScroll)
                StartAnimation();
        }

        private void OnDisable()
        {
            if (_scrollAnimation != null)
            {
                StopCoroutine(_scrollAnimation);
                _scrollAnimation = null;
            }
        }

        private void OnRectTransformDimensionsChange() => RecalculateElements();

        private void RecalculateElements()
        {
            _isTextWidthDirty = true;

            if (_scrollAnimation != null)
            {
                StopCoroutine(_scrollAnimation);
                _scrollAnimation = null;
            }

            if (SizeRatio >= _textWidthRatioThreshold || AlwaysScroll)
            {
                TextComponent.alignment = TextAlignmentOptions.Center;
                TextComponent.text = _text;
                StartAnimation();
            }
            else if (SizeRatio > ScalingMinimumSizeRatio)
            {
                TextComponent.alignment = this.Alignment;
                TextComponent.text = $"<size={(98f / SizeRatio).ToString("N0")}%>" + _text + "</size>";
                TextComponent.rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                TextComponent.alignment = this.Alignment;
                TextComponent.text = _text;
                TextComponent.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void StartAnimation()
        {
            if (AnimationType == ScrollAnimationType.FadeInOut)
                _scrollAnimation = FadeScrollAnimationCoroutine();
            else if (AnimationType == ScrollAnimationType.ForwardAndReverse)
                _scrollAnimation = ForwardReverseScrollAnimationCoroutine();
            else if (AnimationType == ScrollAnimationType.Continuous)
                _scrollAnimation = ContinuousScrollAnimationCoroutine();
            else
                _scrollAnimation = BasicScrollAnimationCoroutine();

            StartCoroutine(_scrollAnimation);
        }

        #region Animation Coroutines
        private IEnumerator ScrollAnimationCoroutine(Vector2 startPos, Vector2 endPos)
        {
            RectTransform rt = TextComponent.rectTransform;

            if (MovementType == ScrollMovementType.ByDuration)
            {
                Vector2 nextPos = startPos;
                float seconds = 0f;

                if (startPos.x > endPos.x)
                {
                    // left to right
                    while (nextPos.x > endPos.x)
                    {
                        nextPos.x = Mathf.Lerp(startPos.x, endPos.x, seconds / ScrollDuration);
                        rt.anchoredPosition = nextPos;
                        seconds += Time.deltaTime;
                        yield return null;
                    }
                }
                else
                {
                    // right to left
                    while (nextPos.x < endPos.x)
                    {
                        nextPos.x = Mathf.Lerp(startPos.x, endPos.x, seconds / ScrollDuration);
                        rt.anchoredPosition = nextPos;
                        seconds += Time.deltaTime;
                        yield return null;
                    }
                }
            }
            else if (MovementType == ScrollMovementType.BySpeed)
            {
                Vector2 nextPos = startPos;
                if (startPos.x > endPos.x)
                {
                    // left to right
                    nextPos.x -= Time.deltaTime * ScrollSpeed;
                    while (nextPos.x > endPos.x)
                    {
                        rt.anchoredPosition = nextPos;
                        yield return null;
                        nextPos.x -= Time.deltaTime * ScrollSpeed;
                    }
                }
                else
                {
                    // right to left
                    nextPos.x += Time.deltaTime * ScrollSpeed;
                    while (nextPos.x < endPos.x)
                    {
                        rt.anchoredPosition = nextPos;
                        yield return null;
                        nextPos.x += Time.deltaTime * ScrollSpeed;
                    }
                }
            }
        }

        private IEnumerator BasicScrollAnimationCoroutine()
        {
            yield return null;

            RectTransform rt = this.TextComponent.rectTransform;
            float halfWidth = this.TextWidth / 2f;
            float halfMaxWidth = this.RectTransform.rect.width / 2f;
            Vector2 startPos = new Vector2(halfWidth - halfMaxWidth, 0f);
            Vector2 endPos = new Vector2(halfMaxWidth - halfWidth, 0f);

            while (true)
            {
                rt.anchoredPosition = startPos;
                yield return _pauseDuration;

                IEnumerator anim = ScrollAnimationCoroutine(startPos, endPos);
                while (anim.MoveNext())
                    yield return anim.Current;

                rt.anchoredPosition = endPos;
                yield return _pauseDuration;
            }
        }

        private IEnumerator FadeScrollAnimationCoroutine()
        {
            yield return null;

            RectTransform rt = this.TextComponent.rectTransform;
            float halfWidth = this.TextWidth / 2f;
            float halfMaxWidth = this.RectTransform.rect.width / 2f;
            Vector2 startPos = new Vector2(halfWidth - halfMaxWidth, 0f);
            Vector2 endPos = new Vector2(halfMaxWidth - halfWidth, 0f);

            while (true)
            {
                rt.anchoredPosition = startPos;

                // fade in
                float seconds = 0f;
                while (seconds < FadeDurationSeconds)
                {
                    yield return null;

                    Color currentColor = TextComponent.color;
                    currentColor.a = Mathf.Lerp(0f, 1f, seconds / FadeDurationSeconds);
                    TextComponent.color = currentColor;

                    seconds += Time.deltaTime;
                }
                yield return _pauseDuration;

                IEnumerator anim = ScrollAnimationCoroutine(startPos, endPos);
                while (anim.MoveNext())
                    yield return anim.Current;

                rt.anchoredPosition = endPos;
                yield return _pauseDuration;

                // fade out
                seconds = 0f;
                while (seconds < FadeDurationSeconds)
                {
                    Color currentColor = TextComponent.color;
                    currentColor.a = Mathf.Lerp(1f, 0f, seconds / FadeDurationSeconds);
                    TextComponent.color = currentColor;

                    seconds += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private IEnumerator ForwardReverseScrollAnimationCoroutine()
        {
            yield return null;

            RectTransform rt = this.TextComponent.rectTransform;
            float halfWidth = this.TextWidth / 2f;
            float halfMaxWidth = this.RectTransform.rect.width / 2f;
            Vector2 startPos = new Vector2(halfWidth - halfMaxWidth, 0f);
            Vector2 endPos = new Vector2(halfMaxWidth - halfWidth, 0f);

            while (true)
            {
                rt.anchoredPosition = startPos;
                yield return _pauseDuration;

                IEnumerator anim = ScrollAnimationCoroutine(startPos, endPos);
                while (anim.MoveNext())
                    yield return anim.Current;

                rt.anchoredPosition = endPos;
                yield return _pauseDuration;

                anim = ScrollAnimationCoroutine(endPos, startPos);
                while (anim.MoveNext())
                    yield return anim.Current;
            }
        }

        private IEnumerator ContinuousScrollAnimationCoroutine()
        {
            yield return null;

            RectTransform rt = this.TextComponent.rectTransform;
            float halfWidth = this.TextWidth / 2f;
            float halfMaxWidth = this.RectTransform.rect.width / 2f;
            Vector2 startPos = new Vector2(halfWidth + halfMaxWidth, 0f);
            Vector2 endPos = new Vector2(-halfWidth - halfMaxWidth, 0f);

            while (true)
            {
                rt.anchoredPosition = startPos;
                yield return null;

                IEnumerator anim = ScrollAnimationCoroutine(startPos, endPos);
                while (anim.MoveNext())
                    yield return anim.Current;

                rt.anchoredPosition = endPos;
                yield return null;
            }
        }
        #endregion

        public enum ScrollMovementType
        {
            ByDuration,
            BySpeed
        }

        public enum ScrollAnimationType
        {
            Basic,
            FadeInOut,
            ForwardAndReverse,
            Continuous
        }
    }
}
