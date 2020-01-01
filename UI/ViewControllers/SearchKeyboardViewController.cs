using System;
using System.Linq;
using UnityEngine;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchKeyboardViewController : ViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<string> PredictionPressed;

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
                _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
                _predictionBar.Initialize(this.transform, 4f, 19f, -50f, 50f);
                _predictionBar.PredictionPressed += delegate (string query)
                {
                    _searchText = query;
                    _textDisplayComponent.text = _searchText.ToUpper() + CursorText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    PredictionPressed?.Invoke(query);
                };

                var keyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard"), this.rectTransform, false).gameObject;
                Destroy(keyboardGO.GetComponent<UIKeyboard>());
                _keyboard = keyboardGO.AddComponent<SearchKeyboard>();
                keyboardGO.name = "EnhancedSearchKeyboard";

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
