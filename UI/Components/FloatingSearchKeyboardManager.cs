using UnityEngine;
using TMPro;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using EnhancedSearchAndFilters.UI.ViewControllers;
using EnhancedSearchAndFilters.Utilities;
using BSUIUtilities = BS_Utils.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class FloatingSearchKeyboardManager : SearchKeyboardManagerBase
    {
        private FloatingScreen _floatingScreen;

#pragma warning disable CS0649
        [UIComponent("lock-image")]
        private ClickableImage lockImage;
#pragma warning restore CS0649

        private static Sprite lockSprite;
        private static Sprite unlockSprite;

        private static readonly Color LockDefaultColour = new Color(0.47f, 0.47f, 0.47f);
        private static readonly Color UnlockDefaultColour = new Color(0.67f, 0.67f, 0.67f);

        [UIValue("lock-image-path")]
        private const string LockImageResourcePath = "EnhancedSearchAndFilters.Assets.lock.png";
        private const string UnlockImageResourcePath = "EnhancedSearchAndFilters.Assets.unlock.png";

        protected override void Awake()
        {
            if (lockSprite == null)
                lockSprite = BSUIUtilities.LoadSpriteFromResources(LockImageResourcePath);
            if (unlockSprite == null)
                unlockSprite = BSUIUtilities.LoadSpriteFromResources(UnlockImageResourcePath);

            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(120f, 64f), false, new Vector3(0f, 0.4f, 2f), Quaternion.Euler(55f, 0f, 0f));
            _floatingScreen.HandleSide = FloatingScreen.Side.Top;

            UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.FloatingKeyboardView.bsml", _floatingScreen.gameObject, this);

            _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
            _predictionBar.Initialize(_floatingScreen.transform, 3.5f, 15f, -55f, 55f);

            var keyboardGO = new GameObject("EnhancedSearchKeyboard", typeof(FloatingSearchKeyboard), typeof(RectTransform));

            var rt = keyboardGO.GetComponent<RectTransform>();
            rt.SetParent(_floatingScreen.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 2f);
            rt.sizeDelta = new Vector2(-10f, 40f);

            _keyboard = keyboardGO.GetComponent<FloatingSearchKeyboard>();

            _textDisplayComponent = BeatSaberUI.CreateText(_floatingScreen.transform as RectTransform, "", new Vector2(0f, 23.5f), Vector2.zero);
            _textDisplayComponent.fontSize = 7f;
            _textDisplayComponent.alignment = TextAlignmentOptions.Center;
            _textDisplayComponent.enableWordWrapping = false;

            base.Awake();
        }

        public override void Activate()
        {
            base.Activate();
            if (_floatingScreen != null)
                _floatingScreen.gameObject.SetActive(true);
        }

        public override void Deactivate()
        {
            _floatingScreen.gameObject.SetActive(false);
        }

        protected void OnDestroy()
        {
            Destroy(_floatingScreen.gameObject);
        }

        [UIAction("lock-clicked")]
        private void OnLockClicked()
        {
            if (_floatingScreen.ShowHandle)
            {
                lockImage.sprite = lockSprite;
                lockImage.DefaultColor = LockDefaultColour;

                _floatingScreen.ShowHandle = false;
            }
            else
            {
                lockImage.sprite = unlockSprite;
                lockImage.DefaultColor = UnlockDefaultColour;

                _floatingScreen.ShowHandle = true;
            }
        }
    }
}
