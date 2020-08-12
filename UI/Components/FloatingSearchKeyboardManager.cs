using TMPro;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using EnhancedSearchAndFilters.UI.ViewControllers;
using UnityEngine;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class FloatingSearchKeyboardManager : SearchKeyboardManagerBase
    {
        private FloatingScreen _floatingScreen;

        protected override void Awake()
        {
            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(120f, 62f), false, new Vector3(0f, 0.4f, 2f), Quaternion.Euler(55f, 0f, 0f));

            _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
            _predictionBar.Initialize(_floatingScreen.transform, 3.5f, 16f, -50f, 50f);

            var keyboardGO = new GameObject("EnhancedSearchKeyboard", typeof(FloatingSearchKeyboard), typeof(RectTransform));

            var rt = keyboardGO.GetComponent<RectTransform>();
            rt.SetParent(_floatingScreen.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 2f);
            rt.sizeDelta = new Vector2(-10f, 40f);

            _keyboard = keyboardGO.GetComponent<FloatingSearchKeyboard>();

            _textDisplayComponent = BeatSaberUI.CreateText(_floatingScreen.transform as RectTransform, "", new Vector2(0f, 24.5f), Vector2.zero);
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
    }
}
