using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;
using BSUIUtilities = BS_Utils.Utilities.UIUtilities;
using System.ComponentModel;

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

        private TabBase[] _tabs = new TabBase[3];
        private TabBase _currentTab = null;

#pragma warning disable CS0649
        [UIObject("container")]
        private GameObject _container;

        [UIValue("cell-data")]
        private readonly List<IconSegmentedControl.DataItem> _cellData = new List<IconSegmentedControl.DataItem>
        {
            new IconSegmentedControl.DataItem(BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.sort.png"), "Sort Mode"),
            new IconSegmentedControl.DataItem(BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png"), "Quick Filters"),
            new IconSegmentedControl.DataItem(BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.info.png"), "Mod Information")
        };
#pragma warning restore CS0649

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

            // this is needed to fix HoverHint position issues that occur because of the change in pivot done to the floating screen
            var wrapperCanvasGO = new GameObject("Wrapper", typeof(RectTransform), typeof(Canvas), typeof(VRGraphicRaycaster), typeof(SetMainCameraToCanvas));
            var rt = wrapperCanvasGO.transform as RectTransform;
            rt.SetParent(_floatingScreen.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var cameraSetter = wrapperCanvasGO.GetComponent<SetMainCameraToCanvas>();
            cameraSetter.SetField("_canvas", wrapperCanvasGO.GetComponent<Canvas>());
            cameraSetter.SetField("_mainCamera", Resources.FindObjectsOfTypeAll<MainCamera>().FirstOrDefault(camera => camera.camera?.stereoTargetEye != StereoTargetEyeMask.None) ?? Resources.FindObjectsOfTypeAll<MainCamera>().FirstOrDefault());

            _outlineImage = new GameObject("Outline").AddComponent<Image>();
            _outlineImage.color = OutlineColour;
            _outlineImage.material = UIUtilities.NoGlowMaterial;
            _outlineImage.type = Image.Type.Sliced;
            _outlineImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().LastOrDefault(x => x.name == "RoundRectSmallStroke");
            _outlineImage.preserveAspect = true;

            _outlineImage.rectTransform.SetParent(wrapperCanvasGO.transform, false);
            _outlineImage.rectTransform.anchorMin = Vector2.zero;
            _outlineImage.rectTransform.anchorMax = Vector2.one;
            _outlineImage.rectTransform.sizeDelta = Vector2.zero;

            var hlg = new GameObject("HorizontalLayoutGroup").AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3.5f;
            hlg.padding = new RectOffset(2, 2, 1, 1);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            rt = hlg.transform as RectTransform;
            rt.SetParent(wrapperCanvasGO.transform, false);
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

            // this needs to be the last child, otherwise the outline image will capture all the controller raycasts first
            UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.BottomScreen.BottomScreenView.bsml", wrapperCanvasGO, this);

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

            var sortModeTab = new SortModeTab(_container);
            sortModeTab.SortButtonPressed += () => SortButtonPressed?.Invoke();
            sortModeTab.Visible = true;

            _currentTab = sortModeTab;
            _tabs[0] = sortModeTab;
            Logger.log.Notice($"finished constructor, currentTab?={_currentTab == null}");

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

        public void UpdateSortButtons() => (_tabs[0] as SortModeTab).UpdateButtonsStatus();

        public void Dispose()
        {
            MonoBehaviour.Destroy(_floatingScreen);
        }

        [UIAction("cell-selected")]
        private void OnCellSelected(SegmentedControl control, int index)
        {
            _currentTab.Visible = false;

            if (_tabs[index] == null)
            {
                switch (index)
                {
                    case 1:
                        _tabs[index] = new QuickFiltersTab(_container);
                        break;

                    case 2:
                        _tabs[index] = new InfoTab(_container);
                        break;
                }
            }

            _tabs[index].Visible = true;
            _currentTab = _tabs[index];
        }

        private abstract class TabBase
        {
            public bool Visible
            {
                get => _gameObject.activeSelf;
                set => _gameObject.SetActive(value);
            }

            protected abstract string ResourceName { get; }

#pragma warning disable CS0649
            [UIObject("root")]
            protected GameObject _gameObject;
#pragma warning restore CS0649

            protected TabBase(GameObject parent)
            {
                UIUtilities.ParseBSML(ResourceName, parent, this);
            }
        }

        private class SortModeTab : TabBase
        {
            public event Action SortButtonPressed;

            protected override string ResourceName => "EnhancedSearchAndFilters.UI.Views.BottomScreen.SortModeTabView.bsml";

            private List<SortModeButton> _buttons;
            [UIValue("sort-buttons")]
            public List<object> Buttons
            {
                get
                {
                    if (_buttons == null)
                    {
                        _buttons = new List<SortModeButton>();
                        foreach (SortMode mode in Enum.GetValues(typeof(SortMode)))
                        {
                            var button = new SortModeButton(mode);
                            button.ButtonPressed += OnSortModeButtonPressed;

                            _buttons.Add(button);
                        }
                    }

                    return _buttons.Cast<object>().ToList();
                }
            }

            private static readonly Color DefaultColour = Color.white;
            private static readonly Color SelectedColour = new Color(0.7f, 1f, 0.6f);
            private static readonly Color SelectedReversedColour = new Color(0.7f, 0.6f, 1f);

            public SortModeTab(GameObject parent) : base(parent)
            {
                UpdateButtonsStatus();
            }

            public void UpdateButtonsStatus()
            {
                foreach (var button in _buttons)
                {
                    if (SongSortModule.CurrentSortMode == button.SortMode)
                        button.ButtonColour = SongSortModule.Reversed ? SelectedReversedColour : SelectedColour;
                    else
                        button.ButtonColour = DefaultColour;
                }
            }

            private void OnSortModeButtonPressed(SortMode sortMode)
            {
                if (sortMode != SongSortModule.CurrentSortMode)
                    _buttons.First(x => x.SortMode == SongSortModule.CurrentSortMode).ButtonColour = DefaultColour;
                SongSortModule.CurrentSortMode = sortMode;

                SortButtonPressed?.Invoke();
            }

            private class SortModeButton
            {
                public event Action<SortMode> ButtonPressed;

                public Color ButtonColour
                {
                    set
                    {
                        if (_buttonStroke == null)
                            _buttonStroke = _button.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");
                        if (_buttonStroke != null)
                            _buttonStroke.color = value;
                    }
                }

                public SortMode SortMode { get; private set; }

#pragma warning disable CS0649
                [UIValue("text")]
                private string _text;
                [UIComponent("button")]
                private Button _button;
#pragma warning restore CS0649

                private Image _buttonStroke;

                public SortModeButton(SortMode sortMode)
                {
                    SortMode = sortMode;

                    _text = Enum.GetName(typeof(SortMode), sortMode);

                    var descriptionAttributes = (DescriptionAttribute[])typeof(SortMode).GetField(_text).GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (descriptionAttributes.Length > 0)
                        _text = descriptionAttributes[0].Description;
                }

                [UIAction("clicked")]
                private void OnClick()
                {
                    ButtonPressed?.Invoke(SortMode);

                    if (SortMode == SongSortModule.CurrentSortMode)
                        ButtonColour = SongSortModule.Reversed ? SortModeTab.SelectedReversedColour : SortModeTab.SelectedColour;
                }
            }
        }

        private class QuickFiltersTab : TabBase
        {
            protected override string ResourceName => "EnhancedSearchAndFilters.UI.Views.BottomScreen.QuickFiltersTabView.bsml";

            public QuickFiltersTab(GameObject parent) : base(parent)
            {
            }
        }

        private class InfoTab : TabBase
        {
            protected override string ResourceName => "EnhancedSearchAndFilters.UI.Views.BottomScreen.InfoTabView.bsml";

            public InfoTab(GameObject parent) : base(parent)
            {
            }
        }
    }
}
