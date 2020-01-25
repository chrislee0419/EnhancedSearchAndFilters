using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPredictionEngine = EnhancedSearchAndFilters.Search.WordPredictionEngine;
using SuggestionType = EnhancedSearchAndFilters.Search.SuggestedWord.SuggestionType;

namespace EnhancedSearchAndFilters.UI.Components
{
    public class PredictionBar : MonoBehaviour
    {
        public event Action<string, SuggestionType> PredictionPressed;

        private bool _initialized = false;

        private static Button _buttonPrefab;
        private Transform _parent;
        private float _fontSize;
        private float _yPos;
        private float _xStartPos;
        private float _xEndPos;

        private Stack<PredictionButton> _unusedButtons = new Stack<PredictionButton>();
        private List<PredictionButton> _predictionButtons = new List<PredictionButton>();

        private static readonly Color DefaultPredictionButtonColor = new Color(0.6f, 0.6f, 0.8f);
        private static readonly Color FuzzyMatchPredictionButtonColor = new Color(0.8f, 0.5f, 0.6f);

        private void Awake()
        {
            if (_buttonPrefab == null)
                _buttonPrefab = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "CancelButton");
        }

        public void Initialize(Transform parent, float fontSize, float yPosition, float xPositionStart, float xPositionEnd)
        {
            _parent = parent;
            _fontSize = fontSize;
            _yPos = yPosition;
            _xStartPos = xPositionStart;
            _xEndPos = xPositionEnd;

            _initialized = true;
        }

        public void ClearAndSetPredictionButtons(string searchText)
        {
            // NOTE: searchText should be lower-cased (keyboard sends lowercase characters)
            if (!_initialized)
                return;

            ClearPredictionButtons();

            if (string.IsNullOrEmpty(searchText))
                return;

            // create new or re-use old buttons
            PredictionButton btn = default;
            float currentX = 0f;
            var predictions = WordPredictionEngine.instance.GetSuggestedWords(searchText);
            for (int i = 0; i < predictions.Count && currentX < _xEndPos - _xStartPos; ++i)
            {
                var word = predictions[i].Word;
                var type = predictions[i].Type;

                if (_unusedButtons.Any())
                {
                    btn = _unusedButtons.Pop();
                    btn.SetActive(true);
                }
                else
                {
                    btn = new PredictionButton(_parent);
                }

                btn.Button.onClick.RemoveAllListeners();
                btn.Button.onClick.AddListener(delegate ()
                {
                    string[] searchTextWords = WordPredictionEngine.RemoveSymbolsRegex.Replace(searchText, " ").Split(WordPredictionEngine.SpaceCharArray);

                    if (searchTextWords.Length == 0)
                    {
                        // this should never be able to happen
                        // implies we got a suggested word from an empty search query
                        searchText = word;
                    }
                    else
                    {
                        char lastChar = searchText[searchText.Length - 1];
                        string lastSearchWord = searchTextWords[searchTextWords.Length - 1];

                        if (type == SuggestionType.Prefixed || type == SuggestionType.FuzzyMatch)
                        {
                            int index = searchText.LastIndexOf(lastSearchWord);
                            searchText = searchText.Remove(index) + word;
                        }
                        else if (type == SuggestionType.FollowUp)
                        {
                            string space = lastChar == ' ' ? "" : " ";
                            searchText = searchText + space + word;
                        }
                        else
                        {
                            searchText = word;
                        }
                    }

                    PredictionPressed?.Invoke(searchText, type);
                });

                btn.SetText(word.ToUpper(), _fontSize);
                btn.Type = type;

                var width = btn.PreferredTextWidth + 8f;
                var rt = btn.Button.transform as RectTransform;
                rt.sizeDelta = new Vector2(width, 7f);
                rt.anchoredPosition = new Vector2(_xStartPos + currentX, _yPos);

                currentX += width + 1.5f;
                _predictionButtons.Add(btn);
            }

            // remove the last button created, since it goes past the end of the screen
            // we have to do this here, since we don't know the width of the strings to be displayed before button creation
            if (PredictionButton.IsValid(btn) && currentX >= _xEndPos - _xStartPos)
            {
                _predictionButtons.Remove(btn);
                btn.SetActive(false);
                _unusedButtons.Push(btn);
            }
        }

        public void ClearPredictionButtons()
        {
            if (!_initialized)
                return;

            foreach (var oldButton in _predictionButtons)
            {
                oldButton.SetActive(false);
                _unusedButtons.Push(oldButton);
            }
            _predictionButtons.Clear();
        }

        private struct PredictionButton
        {
            public Button Button { get; private set; }
            public SuggestionType Type
            {
                get => _type;
                set
                {
                    _type = value;
                    _stroke.color = value == SuggestionType.FuzzyMatch ? FuzzyMatchPredictionButtonColor : DefaultPredictionButtonColor;
                }
            }
            public float PreferredTextWidth { get => _text.preferredWidth; }

            private TextMeshProUGUI _text;
            private SuggestionType _type;
            private Image _stroke;

            public PredictionButton(Transform parent)
            {
                Button = Instantiate(_buttonPrefab, parent, false);
                _text = Button.GetComponentInChildren<TextMeshProUGUI>();
                _type = default;
                _stroke = Button.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Stroke");

                Button.name = "SearchPredictionBarButton";

                var rectTransform = (Button.transform as RectTransform);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0f, 0.5f);
                Button.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content").padding = new RectOffset(0, 0, 0, 0);
                _stroke.color = DefaultPredictionButtonColor;
            }

            public void SetActive(bool active) => Button.gameObject.SetActive(active);

            public void SetText(string text, float fontSize = -1f)
            {
                if (fontSize > 0f)
                    _text.fontSize = fontSize;
                _text.text = text;
                _text.enableWordWrapping = false;
            }

            public static bool IsValid(PredictionButton btn) => btn.Button != null && btn._text != null && btn._stroke != null;
        }
    }
}
