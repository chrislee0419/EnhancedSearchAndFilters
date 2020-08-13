using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.Utilities;
using BSUIUtilities = BS_Utils.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class BottomScreen : IDisposable
    {
        public event Action SortButtonPressed;
        public event Action<QuickFilter> QuickFilterApplied;
        public event Action ReportIssueButtonPressed;

        private FloatingScreen _floatingScreen;
        private Image _outlineImage;
        private Image _iconImage;
        private TextMeshProUGUI _text;
        private EnterExitEventHandler _hoverEventHandler;

        private Coroutine _revealAnimation;
        private Coroutine _expandAnimation;
        private Coroutine _contractAnimation;

        private const float DefaultScale = 0.02f;
        private const float HiddenScale = 0f;

        private const float DefaultXSize = 30f;
        private const float DefaultYSize = 8f;
        private const float ExpandedXSize = 150f;
        private const float ExpandedYSize = 60f;

        private const float AnimationEpsilon = 0.0001f;

        private static readonly WaitForSeconds CollapseAnimationDelay = new WaitForSeconds(0.4f);

        private static readonly Color OutlineColour = new Color(0.6f, 0.6f, 0.6f);

        public BottomScreen()
        {
            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(DefaultXSize, DefaultYSize), false, new Vector3(1.5f, 0.05f, 1.5f), Quaternion.Euler(50f, 0f, 0f));
            (_floatingScreen.transform as RectTransform).pivot = new Vector2(1f, 0f);

            _outlineImage = new GameObject("Outline").AddComponent<Image>();
            _outlineImage.color = OutlineColour;
            _outlineImage.material = UIUtilities.NoGlowMaterial;
            _outlineImage.type = Image.Type.Sliced;
            _outlineImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().LastOrDefault(x => x.name == "RoundRectSmallStroke");
            _outlineImage.preserveAspect = true;

            _outlineImage.rectTransform.SetParent(_floatingScreen.transform, false);
            _outlineImage.rectTransform.anchorMin = Vector2.zero;
            _outlineImage.rectTransform.anchorMax = Vector2.one;
            _outlineImage.rectTransform.sizeDelta = Vector2.zero;

            var hlg = new GameObject("HorizontalLayoutGroup").AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3.5f;
            hlg.padding = new RectOffset(2, 2, 1, 1);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var rt = hlg.transform as RectTransform;
            rt.SetParent(_floatingScreen.transform, false);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(DefaultXSize, DefaultYSize);

            // icon
            _iconImage = new GameObject("Icon").AddComponent<Image>();
            _iconImage.material = UIUtilities.NoGlowMaterial;
            _iconImage.sprite = BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.bars.png");
            _iconImage.preserveAspect = true;

            var le = _iconImage.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 4.5f;
            le.preferredHeight = 4.5f;

            _iconImage.rectTransform.SetParent(rt, false);

            // text
            _text = BeatSaberUI.CreateText(rt, "OPTIONS", Vector2.zero, Vector2.zero);
            _text.fontSize = 4.4f;
            _text.alignment = TextAlignmentOptions.Center;

            _hoverEventHandler = _floatingScreen.gameObject.AddComponent<EnterExitEventHandler>();
            _hoverEventHandler.PointerEntered += delegate ()
            {
                if (_revealAnimation != null)
                {
                    return;
                }
                else if (_contractAnimation != null)
                {
                    UnityCoroutineHelper.Stop(_contractAnimation);
                    _contractAnimation = null;
                }

                _expandAnimation = UnityCoroutineHelper.Start(ExpandAnimationCoroutine());
            };
            _hoverEventHandler.PointerExited += delegate ()
            {
                if (_revealAnimation != null)
                    return;

                bool immediate = false;
                if (_expandAnimation != null)
                {
                    UnityCoroutineHelper.Stop(_expandAnimation);
                    _expandAnimation = null;
                    immediate = true;
                }
                else if (_contractAnimation != null)
                {
                    UnityCoroutineHelper.Stop(_contractAnimation);
                    _contractAnimation = null;
                }

                _contractAnimation = UnityCoroutineHelper.Start(ContractAnimationCoroutine(immediate));
            };

            HideScreen(true);
        }

        public void ShowScreen(bool immediately = false)
        {
            if (_floatingScreen.gameObject.activeSelf)
                return;

            if (immediately)
            {
                _floatingScreen.transform.localScale = new Vector3(DefaultScale, DefaultScale, _floatingScreen.transform.localScale.z);
                (_floatingScreen.transform as RectTransform).sizeDelta = new Vector2(DefaultXSize, DefaultYSize);

                _outlineImage.color = OutlineColour;
                _iconImage.color = Color.white;
                _text.color = Color.white;

                _floatingScreen.gameObject.SetActive(true);
            }
            else
            {
                _floatingScreen.gameObject.SetActive(true);
                _revealAnimation = UnityCoroutineHelper.Start(RevealAnimationCoroutine(DefaultScale));
            }
        }

        public void HideScreen(bool immediately = false)
        {
            if (!_floatingScreen.gameObject.activeSelf)
                return;

            if (immediately)
            {
                _floatingScreen.transform.localScale = new Vector3(HiddenScale, HiddenScale, _floatingScreen.transform.localScale.z);
                (_floatingScreen.transform as RectTransform).sizeDelta = new Vector2(DefaultXSize, DefaultYSize);

                _floatingScreen.gameObject.SetActive(false);
            }
            else
            {
                _revealAnimation = UnityCoroutineHelper.Start(RevealAnimationCoroutine(HiddenScale, true));
            }
        }

        private IEnumerator RevealAnimationCoroutine(float destAnimationValue, bool disableOnFinish = false)
        {
            yield return null;
            yield return null;

            // reset size delta as well
            (_floatingScreen.transform as RectTransform).sizeDelta = new Vector2(DefaultXSize, DefaultYSize);

            _outlineImage.color = OutlineColour;
            _iconImage.color = Color.white;
            _text.color = Color.white;

            Vector3 localScale = _floatingScreen.transform.localScale;
            while (Mathf.Abs(localScale.x - destAnimationValue) > 0.0001f)
            {
                float num = (localScale.x > destAnimationValue) ? 30f : 16f;
                localScale.x = Mathf.Lerp(localScale.y, destAnimationValue, Time.deltaTime * num);
                localScale.y = localScale.x;
                _floatingScreen.transform.localScale = localScale;

                yield return null;
            }

            localScale.x = destAnimationValue;
            localScale.y = destAnimationValue;
            _floatingScreen.transform.localScale = localScale;

            _floatingScreen.gameObject.SetActive(!disableOnFinish);
            _revealAnimation = null;
        }

        private IEnumerator ExpandAnimationCoroutine()
        {
            const float ColourTargetAlpha = 0f;

            RectTransform rt = _floatingScreen.transform as RectTransform;
            Vector3 sizeDelta = rt.sizeDelta;
            Color outlineColour = _outlineImage.color;
            Color iconColour = _iconImage.color;
            Color textColour = _text.color;

            while (Mathf.Abs(sizeDelta.x - ExpandedXSize) > AnimationEpsilon || Mathf.Abs(sizeDelta.y - ExpandedYSize) > AnimationEpsilon)
            {
                float t = Time.deltaTime * 45;

                sizeDelta.x = Mathf.Lerp(sizeDelta.x, ExpandedXSize, t);
                sizeDelta.y = Mathf.Lerp(sizeDelta.y, ExpandedYSize, t);
                rt.sizeDelta = sizeDelta;

                outlineColour.a = Mathf.Lerp(outlineColour.a, ColourTargetAlpha, t);
                _outlineImage.color = outlineColour;
                iconColour.a = outlineColour.a;
                _iconImage.color = iconColour;
                textColour.a = outlineColour.a;
                _text.color = textColour;

                yield return null;
            }

            sizeDelta.x = ExpandedXSize;
            sizeDelta.y = ExpandedYSize;
            rt.sizeDelta = sizeDelta;

            outlineColour.a = ColourTargetAlpha;
            _outlineImage.color = outlineColour;
            iconColour.a = ColourTargetAlpha;
            _iconImage.color = iconColour;
            textColour.a = ColourTargetAlpha;
            _text.color = textColour;

            _expandAnimation = null;
        }

        private IEnumerator ContractAnimationCoroutine(bool immediate = false)
        {
            const float ColourTargetAlpha = 1f;

            RectTransform rt = _floatingScreen.transform as RectTransform;
            Vector3 sizeDelta = rt.sizeDelta;
            Color outlineColour = _outlineImage.color;
            Color iconColour = _iconImage.color;
            Color textColour = _text.color;

            if (!immediate)
            {
                yield return CollapseAnimationDelay;
                if (_hoverEventHandler.IsPointedAt)
                    yield break;
            }

            while (Mathf.Abs(sizeDelta.x - DefaultXSize) > AnimationEpsilon || Mathf.Abs(sizeDelta.y - DefaultYSize) > AnimationEpsilon)
            {
                float t = Time.deltaTime * 30;

                sizeDelta.x = Mathf.Lerp(sizeDelta.x, DefaultXSize, t);
                sizeDelta.y = Mathf.Lerp(sizeDelta.y, DefaultYSize, t);
                rt.sizeDelta = sizeDelta;

                outlineColour.a = Mathf.Lerp(outlineColour.a, ColourTargetAlpha, t);
                _outlineImage.color = outlineColour;
                iconColour.a = outlineColour.a;
                _iconImage.color = iconColour;
                textColour.a = outlineColour.a;
                _text.color = textColour;

                yield return null;
            }

            sizeDelta.x = DefaultXSize;
            sizeDelta.y = DefaultYSize;
            rt.sizeDelta = sizeDelta;

            outlineColour.a = ColourTargetAlpha;
            _outlineImage.color = outlineColour;
            iconColour.a = ColourTargetAlpha;
            _iconImage.color = iconColour;
            textColour.a = ColourTargetAlpha;
            _text.color = textColour;

            _contractAnimation = null;
        }

        public void UpdateSortButtons()
        {
            // TODO
        }

        public void Dispose()
        {
            MonoBehaviour.Destroy(_floatingScreen);
        }

        private abstract class TabBase
        {
            public bool Visible
            {
                get => _gameObject.activeSelf;
                set => _gameObject.SetActive(value);
            }

            protected abstract string ResourceName { get; }

            [UIObject("root")]
            protected GameObject _gameObject;

            protected TabBase(GameObject parent)
            {
                UIUtilities.ParseBSML(ResourceName, parent, this);
            }
        }

        private class SortModeTab : TabBase
        {
            protected override string ResourceName => "";

            public SortModeTab(GameObject parent) : base(parent)
            {
            }
        }

        private class QuickFiltersTab : TabBase
        {
            protected override string ResourceName => "";

            public QuickFiltersTab(GameObject parent) : base(parent)
            {
            }
        }

        private class InfoTab : TabBase
        {
            protected override string ResourceName => "";

            public InfoTab(GameObject parent) : base(parent)
            {
            }
        }
    }
}
