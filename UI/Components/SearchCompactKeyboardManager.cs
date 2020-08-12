using UnityEngine;
using TMPro;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SearchCompactKeyboardManager : ViewControllerSearchKeyboardManagerBase
    {
        protected override void Awake()
        {
            const float OffsetX = 5f;

            CreateViewController("SearchCompactKeyboardViewController");

            ViewController.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ViewController.rectTransform.anchorMax = ViewController.rectTransform.anchorMin;
            ViewController.rectTransform.pivot = ViewController.rectTransform.anchorMin;
            ViewController.rectTransform.anchoredPosition = Vector2.zero;
            ViewController.rectTransform.sizeDelta = new Vector2(80f, 70f);

            _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
            _predictionBar.Initialize(ViewController.transform, 3.5f, 19f, -35f, 45f);

            var keyboardGO = new GameObject("EnhancedSearchKeyboard", typeof(CompactSearchKeyboard), typeof(RectTransform));

            var rt = keyboardGO.GetComponent<RectTransform>();
            rt.SetParent(ViewController.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(OffsetX, 5f);
            rt.sizeDelta = Vector2.zero;

            _keyboard = keyboardGO.GetComponent<CompactSearchKeyboard>();

            _textDisplayComponent = BeatSaberUI.CreateText(ViewController.rectTransform, "", new Vector2(OffsetX, 28f), new Vector2(4f, 4f));
            _textDisplayComponent.fontSize = 6f;
            _textDisplayComponent.alignment = TextAlignmentOptions.Center;
            _textDisplayComponent.enableWordWrapping = false;

            base.Awake();
        }
    }
}
