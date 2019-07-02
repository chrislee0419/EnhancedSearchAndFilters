using System;
using System.Linq;
using UnityEngine;
using TMPro;
using VRUI;
using EnhancedSearchAndFilters.UI.Components;
using CustomUI.BeatSaber;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchKeyboardViewController : VRUIViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;

        private SearchKeyboard _keyboard;
        private TextMeshProUGUI _textDisplayComponent;
        private string _searchText;
        private const string _placeholderText = "Search...";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                var keyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard"), this.rectTransform, false).gameObject;
                Destroy(keyboardGO.GetComponent<UIKeyboard>());
                _keyboard = keyboardGO.AddComponent<SearchKeyboard>();
                keyboardGO.name = "EnhancedSearchKeyboard";

                _keyboard.TextKeyPressed += delegate (char key)
                {
                    _searchText += key.ToString().ToUpper();
                    _textDisplayComponent.text = _searchText;
                    TextKeyPressed?.Invoke(key);
                };
                _keyboard.DeleteButtonPressed += delegate
                {
                    if (_searchText.Length > 0)
                        _searchText = _searchText.Substring(0, _textDisplayComponent.text.Length - 1);

                    if (_searchText.Length > 0)
                    {
                        _textDisplayComponent.text = _searchText;
                    }
                    else
                    {
                        _textDisplayComponent.text = _placeholderText;
                    }

                    DeleteButtonPressed?.Invoke();
                };
                _keyboard.ClearButtonPressed += delegate
                {
                    _searchText = "";
                    _textDisplayComponent.text = _placeholderText;
                    ClearButtonPressed?.Invoke();
                };

                _textDisplayComponent = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(0f, 26f), new Vector2(4f, 4f));
                _textDisplayComponent.fontSize = 8f;
                _textDisplayComponent.alignment = TextAlignmentOptions.Center;
                _textDisplayComponent.enableWordWrapping = false;
            }

            _searchText = "";
            _textDisplayComponent.text = _placeholderText;
            _keyboard.SymbolButtonInteractivity = !PluginConfig.StripSymbols;
            _keyboard.ResetSymbolMode();
        }

        public void SetText(string text)
        {
            _searchText = text.ToUpper();
            _textDisplayComponent.text = string.IsNullOrEmpty(text) ? _placeholderText : text.ToUpper();
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
