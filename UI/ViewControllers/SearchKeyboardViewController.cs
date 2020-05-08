using System;
using UnityEngine;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.UI.Components;
using EnhancedSearchAndFilters.Utilities;
using SuggestionType = EnhancedSearchAndFilters.Search.SuggestedWord.SuggestionType;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SearchKeyboardViewController : ViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<string, SuggestionType> PredictionPressed;
        public event Action FilterButtonPressed;

        private SearchKeyboard _keyboard;
        private TextMeshProUGUI _textDisplayComponent;
        private PredictionBar _predictionBar;
        private string _searchText;

        private const string PlaceholderText = "Search...";
        private const string CursorText = "<color=#00CCCC>|</color>";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                this.name = "SearchKeyboardViewController";

                _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
                _predictionBar.Initialize(this.transform, 4f, 19f, -50f, 50f);
                _predictionBar.PredictionPressed += delegate (string query, SuggestionType type)
                {
                    _searchText = query;
                    _textDisplayComponent.text = _searchText.ToUpper() + CursorText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    PredictionPressed?.Invoke(query, type);
                };

                var keyboardGO = new GameObject("EnhancedSearchKeyboard", typeof(SearchKeyboard), typeof(RectTransform));

                var rt = keyboardGO.GetComponent<RectTransform>();
                rt.SetParent(this.rectTransform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 5f);
                rt.sizeDelta = new Vector2(-10f, 50f);

                _keyboard = keyboardGO.GetComponent<SearchKeyboard>();

                _keyboard.TextButtonPressed += delegate (char key)
                {
                    _searchText += key.ToString();
                    SetDisplayedText(_searchText);

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    TextKeyPressed?.Invoke(key);
                };
                _keyboard.DeleteButtonPressed += delegate
                {
                    if (_searchText.Length > 0)
                        _searchText = _searchText.Substring(0, _searchText.Length - 1);

                    SetDisplayedText(_searchText);
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
                _keyboard.FilterButtonPressed += () => FilterButtonPressed?.Invoke();

                _textDisplayComponent = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(0f, 28f), new Vector2(4f, 4f));
                _textDisplayComponent.fontSize = 7.5f;
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
            SetDisplayedText(text);

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

        private void SetDisplayedText(string text)
        {
            _textDisplayComponent.text = string.IsNullOrEmpty(text) ? PlaceholderText : (text.ToUpper().EscapeTextMeshProTags() + CursorText);
        }
    }
}
