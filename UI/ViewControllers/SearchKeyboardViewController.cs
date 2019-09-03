using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRUI;
using CustomUI.BeatSaber;
using EnhancedSearchAndFilters.UI.Components;
using WordPredictionEngine = EnhancedSearchAndFilters.Search.WordPredictionEngine;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchKeyboardViewController : VRUIViewController
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<string> PredictionPressed;

        private Button _buttonPrefab;

        private SearchKeyboard _keyboard;
        private TextMeshProUGUI _textDisplayComponent;
        private List<Button> _predictionButtons = new List<Button>();
        private string _searchText;

        private const string PlaceholderText = "Search...";
        private const float PredictionBarY = 19f;
        private const float PredictionBarXStart = -50f;
        private const float PredictionBarXEnd = 50f;

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

                    SetPredictionButtons();

                    TextKeyPressed?.Invoke(key);
                };
                _keyboard.DeleteButtonPressed += delegate
                {
                    if (_searchText.Length > 0)
                        _searchText = _searchText.Substring(0, _searchText.Length - 1);

                    if (_searchText.Length > 0)
                    {
                        _textDisplayComponent.text = _searchText;
                    }
                    else
                    {
                        _textDisplayComponent.text = PlaceholderText;
                    }

                    SetPredictionButtons();

                    DeleteButtonPressed?.Invoke();
                };
                _keyboard.ClearButtonPressed += delegate
                {
                    _searchText = "";
                    _textDisplayComponent.text = PlaceholderText;

                    SetPredictionButtons();

                    ClearButtonPressed?.Invoke();
                };

                _textDisplayComponent = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(0f, 28f), new Vector2(4f, 4f));
                _textDisplayComponent.fontSize = 7.5f;
                _textDisplayComponent.alignment = TextAlignmentOptions.Center;
                _textDisplayComponent.enableWordWrapping = false;

                _buttonPrefab = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "CancelButton");
            }

            _searchText = "";
            _textDisplayComponent.text = PlaceholderText;
            _keyboard.SymbolButtonInteractivity = !PluginConfig.StripSymbols;
            _keyboard.ResetSymbolMode();

            foreach (var oldButton in _predictionButtons)
                Destroy(oldButton.gameObject);
            _predictionButtons.Clear();
        }

        public void SetText(string text)
        {
            _searchText = text.ToUpper();
            _textDisplayComponent.text = string.IsNullOrEmpty(text) ? PlaceholderText : text.ToUpper();

            SetPredictionButtons();
        }

        public void SetSymbolButtonInteractivity(bool isInteractive)
        {
            if (!this.isActivated || !_keyboard.isActiveAndEnabled)
                return;

            _keyboard.SymbolButtonInteractivity = isInteractive;
            if (!isInteractive)
                _keyboard.ResetSymbolMode();
        }

        private void SetPredictionButtons()
        {
            ClearPredictionButtons();

            if (string.IsNullOrEmpty(_searchText))
                return;

            // create new buttons
            Button btn = null;
            float currentX = 0f;
            var predictions = WordPredictionEngine.Instance.GetWordsWithPrefix(_searchText);
            for (int i = 0; i < predictions.Count && currentX < PredictionBarXEnd - PredictionBarXStart; ++i)
            {
                var word = predictions[i];

                btn = Instantiate(_buttonPrefab, this.transform, false);
                var rt = (btn.transform as RectTransform);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                btn.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content").padding = new RectOffset(0, 0, 0, 0);
                btn.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Stroke").color = new Color(0.6f, 0.6f, 0.8f);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(delegate ()
                {
                    _searchText = word.ToUpper();
                    _textDisplayComponent.SetText(_searchText);

                    SetPredictionButtons();

                    PredictionPressed?.Invoke(word);
                });

                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                text.fontSize = 4f;
                text.text = word.ToUpper();
                text.enableWordWrapping = false;

                var width = text.preferredWidth + 8f;
                rt.sizeDelta = new Vector2(width, 7f);
                rt.anchoredPosition = new Vector2(PredictionBarXStart + currentX, PredictionBarY);

                currentX += width + 1.5f;
                _predictionButtons.Add(btn);
            }

            // remove the last button created, since it goes past the end of the screen
            // we have to do this here, since we don't know the width of the strings to be displayed before button creation
            if (btn != null && currentX >= PredictionBarXEnd - PredictionBarXStart)
            {
                _predictionButtons.Remove(btn);
                Destroy(btn.gameObject);
            }
        }

        private void ClearPredictionButtons()
        {
            foreach (var oldButton in _predictionButtons)
                Destroy(oldButton.gameObject);
            _predictionButtons.Clear();
        }
    }
}
