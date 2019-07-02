using System;
using System.Linq;
using UnityEngine;
using TMPro;
using VRUI;
using CustomUI.BeatSaber;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchCompactKeyboardViewController : VRUIViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;

        private CompactSearchKeyboard _keyboard;
        private TextMeshProUGUI _textDisplayComponent;
        private string _searchText;
        private const string _placeholderText = "Search...";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                this.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                this.rectTransform.anchorMax = this.rectTransform.anchorMin;
                this.rectTransform.pivot = this.rectTransform.anchorMin;
                this.rectTransform.anchoredPosition = Vector2.zero;
                this.rectTransform.sizeDelta = new Vector2(80f, 70f);

                var keyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard" && x.name != "EnhancedSearchKeyboard"), this.transform, false).gameObject;
                Destroy(keyboardGO.GetComponent<UIKeyboard>());
                _keyboard = keyboardGO.AddComponent<CompactSearchKeyboard>();
                keyboardGO.name = "EnhancedSearchKeyboard";

                var rt = _keyboard.transform as RectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(5f, 5f);
                rt.sizeDelta = Vector2.zero;

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

                _textDisplayComponent = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(5f, 26f), new Vector2(4f, 4f));
                _textDisplayComponent.fontSize = 6f;
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
