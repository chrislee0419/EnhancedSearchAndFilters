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
    internal abstract class SearchKeyboardManagerBase : MonoBehaviour
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<string, SuggestionType> PredictionPressed;
        public event Action FilterButtonPressed;

        protected SearchKeyboard _keyboard;
        protected TextMeshProUGUI _textDisplayComponent;
        protected PredictionBar _predictionBar;
        protected string _searchText;

        public const string PlaceholderText = "Search...";
        public const string CursorText = "<color=#00CCCC>|</color>";

        protected virtual void Awake()
        {
            // NOTE: this should be called after initializing the components in the derived class's overridden Awake
            if (_predictionBar != null)
            {
                _predictionBar.PredictionPressed += delegate (string query, SuggestionType type)
                {
                    _searchText = query;
                    _textDisplayComponent.text = _searchText.ToUpper() + CursorText;

                    _predictionBar.ClearAndSetPredictionButtons(_searchText);

                    PredictionPressed?.Invoke(query, type);
                };
            }

            if (_keyboard != null)
            {
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
            }
        }

        public virtual void Activate()
        {
            _searchText = "";
            _textDisplayComponent.text = PlaceholderText;
            _keyboard.SymbolButtonInteractivity = !PluginConfig.StripSymbols;
            _keyboard.ResetSymbolMode();

            _predictionBar.ClearPredictionButtons();
        }

        public virtual void Deactivate()
        {

        }

        protected void InvokeTextKeyPressed(char c) => TextKeyPressed?.Invoke(c);
        protected void InvokeDeleteButtonPressed() => DeleteButtonPressed?.Invoke();
        protected void InvokeClearButtonPressed() => ClearButtonPressed?.Invoke();
        protected void InvokePredictionPressed(string s, SuggestionType type) => PredictionPressed?.Invoke(s, type);
        protected void InvokeFilterButtonPressed() => FilterButtonPressed?.Invoke();

        public void SetText(string text)
        {
            _searchText = text;
            SetDisplayedText(text);

            if (_predictionBar != null)
                _predictionBar.ClearAndSetPredictionButtons(_searchText);
        }

        public void SetSymbolButtonInteractivity(bool isInteractive)
        {
            if (!(_keyboard?.isActiveAndEnabled ?? false))
                return;

            _keyboard.SymbolButtonInteractivity = isInteractive;
            if (!isInteractive)
                _keyboard.ResetSymbolMode();
        }

        protected void SetDisplayedText(string text)
        {
            if (_textDisplayComponent != null)
                _textDisplayComponent.text = string.IsNullOrEmpty(text) ? PlaceholderText : (text.ToUpper().EscapeTextMeshProTags() + CursorText);
        }
    }

    internal abstract class ViewControllerSearchKeyboardManagerBase : SearchKeyboardManagerBase
    {
        public ViewController ViewController { get; private set; }

        protected virtual void OnDestroy()
        {
            if (ViewController != null)
                Destroy(ViewController.gameObject);
        }

        protected virtual void CreateViewController(string name = null)
        {
            var vc = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
            vc.name = name != null ? name : "SearchKeyboardViewController";
            vc.Parent = this;

            ViewController = vc;
        }

        protected class SearchKeyboardViewController : ViewController
        {
            public ViewControllerSearchKeyboardManagerBase Parent { get; set; }

            protected override void DidActivate(bool firstActivation, ActivationType activationType) => Parent.Activate();
        }
    }

    internal class RightScreenSearchKeyboardManager : ViewControllerSearchKeyboardManagerBase
    {
        protected override void Awake()
        {
            CreateViewController();

            _predictionBar = new GameObject("EnhancedSearchPredictionBar").AddComponent<PredictionBar>();
            _predictionBar.Initialize(ViewController.transform, 4f, 19f, -50f, 50f);

            var keyboardGO = new GameObject("EnhancedSearchKeyboard", typeof(SearchKeyboard), typeof(RectTransform));

            var rt = keyboardGO.GetComponent<RectTransform>();
            rt.SetParent(ViewController.rectTransform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 5f);
            rt.sizeDelta = new Vector2(-10f, 50f);

            _keyboard = keyboardGO.GetComponent<SearchKeyboard>();

            _textDisplayComponent = BeatSaberUI.CreateText(ViewController.rectTransform, "", new Vector2(0f, 28f), new Vector2(4f, 4f));
            _textDisplayComponent.fontSize = 7.5f;
            _textDisplayComponent.alignment = TextAlignmentOptions.Center;
            _textDisplayComponent.enableWordWrapping = false;

            base.Awake();
        }
    }
}
