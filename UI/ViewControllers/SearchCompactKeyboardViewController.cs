using System;
using System.Linq;
using UnityEngine;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.UI.Components;
using SuggestionType = EnhancedSearchAndFilters.Search.SuggestedWord.SuggestionType;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SearchCompactKeyboardViewController : ViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<string, SuggestionType> PredictionPressed;

        private CompactSearchKeyboard _keyboard;
        private TextMeshProUGUI _textDisplayComponent;
        private PredictionBar _predictionBar;
        private string _searchText;

        private const string PlaceholderText = "Search...";
        private const float OffsetX = 5f;
        private const string CursorText = "<color=#00CCCC>|</color>";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                this.name = "SearchCompactKeyboardViewController";

                this.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                this.rectTransform.anchorMax = this.rectTransform.anchorMin;
                this.rectTransform.pivot = this.rectTransform.anchorMin;
                this.rectTransform.anchoredPosition = Vector2.zero;
                this.rectTransform.sizeDelta = new Vector2(80f, 70f);

                _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
                _predictionBar.Initialize(this.transform, 3.5f, 19f, -35f, 45f);
                _predictionBar.PredictionPressed += delegate (string query, SuggestionType type)
                {
                    _searchText = query;
                    _textDisplayComponent.text = _searchText.ToUpper() + CursorText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    PredictionPressed?.Invoke(query, type);
                };

                var keyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard" && x.name != "EnhancedSearchKeyboard"), this.transform, false).gameObject;
                Destroy(keyboardGO.GetComponent<UIKeyboard>());
                _keyboard = keyboardGO.AddComponent<CompactSearchKeyboard>();
                keyboardGO.name = "EnhancedSearchKeyboard";

                var rt = _keyboard.transform as RectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(OffsetX, 5f);
                rt.sizeDelta = Vector2.zero;

                _keyboard.TextKeyPressed += delegate (char key)
                {
                    _searchText += key.ToString();
                    _textDisplayComponent.text = _searchText.ToUpper() + CursorText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    TextKeyPressed?.Invoke(key);
                };
                _keyboard.DeleteButtonPressed += delegate
                {
                    if (_searchText.Length > 0)
                        _searchText = _searchText.Substring(0, _searchText.Length - 1);

                    if (_searchText.Length > 0)
                    {
                        _textDisplayComponent.text = _searchText.ToUpper() + CursorText;
                    }
                    else
                    {
                        _textDisplayComponent.text = PlaceholderText;
                    }

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    DeleteButtonPressed?.Invoke();
                };
                _keyboard.ClearButtonPressed += delegate
                {
                    _searchText = "";
                    _textDisplayComponent.text = PlaceholderText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    ClearButtonPressed?.Invoke();
                };

                _textDisplayComponent = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(OffsetX, 28f), new Vector2(4f, 4f));
                _textDisplayComponent.fontSize = 6f;
                _textDisplayComponent.alignment = TextAlignmentOptions.Center;
                _textDisplayComponent.enableWordWrapping = false;
            }

            _searchText = "";
            _textDisplayComponent.text = PlaceholderText;
            _keyboard.SymbolButtonInteractivity = !PluginConfig.StripSymbols;
            _keyboard.ResetSymbolMode();
            _predictionBar.ClearPredictionButtons();
        }

        public void SetText(string text)
        {
            _searchText = text;
            _textDisplayComponent.text = string.IsNullOrEmpty(text) ? PlaceholderText : (text.ToUpper() + CursorText);

            _predictionBar.ClearAndSetPredictionButtons(_searchText);
        }

        public void SetSymbolButtonInteractivity(bool isInteractive)
        {
            if (!this.isActivated || !_keyboard.isActiveAndEnabled)
                return;

            _keyboard.SymbolButtonInteractivity = isInteractive;
            if (!isInteractive)
                _keyboard.ResetSymbolMode();
        }
    }
}
