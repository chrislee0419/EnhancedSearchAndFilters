using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPredictionEngine = EnhancedSearchAndFilters.Search.WordPredictionEngine;

namespace EnhancedSearchAndFilters.UI.Components
{
    class PredictionBar : MonoBehaviour
    {
        public event Action<string> PredictionPressed;

        private bool _initialized = false;

        private Button _buttonPrefab;
        private Transform _parent;
        private float _fontSize;
        private float _yPos;
        private float _xStartPos;
        private float _xEndPos;

        private Stack<Button> _unusedButtons = new Stack<Button>();
        private List<Button> _predictionButtons = new List<Button>();

        private void Awake()
        {
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
            Button btn = null;
            float currentX = 0f;
            var predictions = WordPredictionEngine.instance.GetSuggestedWords(searchText);
            for (int i = 0; i < predictions.Count && currentX < _xEndPos - _xStartPos; ++i)
            {
                var word = predictions[i];

                if (_unusedButtons.Any())
                {
                    btn = _unusedButtons.Pop();
                    btn.gameObject.SetActive(true);
                }
                else
                {
                    btn = Instantiate(_buttonPrefab, _parent, false);
                    btn.name = "SearchPredictionBarButton";

                    var rectTransform = (btn.transform as RectTransform);
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0f, 0.5f);
                    btn.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content").padding = new RectOffset(0, 0, 0, 0);
                    btn.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Stroke").color = new Color(0.6f, 0.6f, 0.8f);
                }

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(delegate ()
                {
                    if (!char.IsLetterOrDigit(searchText[searchText.Length - 1]) && !(searchText[searchText.Length - 1] == '\''))
                    {
                        searchText += word;
                    }
                    else
                    {
                        var searchTextWords = WordPredictionEngine.RemoveSymbolsRegex.Replace(searchText, " ").Split(new char[] { ' ' });

                        if (searchTextWords.Length == 0)
                        {
                            // this should never be able to happen
                            // implies we got a suggested word from empty search query
                            searchText = word;
                        }
                        else
                        {
                            var lastSearchQueryWord = searchTextWords[searchTextWords.Length - 1];

                            if (word != lastSearchQueryWord && word.StartsWith(lastSearchQueryWord))
                            {
                                var space = searchTextWords.Length == 1 ? "" : " ";
                                searchText = searchText.Remove(searchText.Length - lastSearchQueryWord.Length) + space + word;
                            }
                            else
                            {
                                searchText += " " + word;
                            }
                        }
                    }

                    PredictionPressed?.Invoke(searchText);
                });

                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                text.fontSize = _fontSize;
                text.text = word.ToUpper();
                text.enableWordWrapping = false;

                var width = text.preferredWidth + 8f;
                var rt = btn.transform as RectTransform;
                rt.sizeDelta = new Vector2(width, 7f);
                rt.anchoredPosition = new Vector2(_xStartPos + currentX, _yPos);

                currentX += width + 1.5f;
                _predictionButtons.Add(btn);
            }

            // remove the last button created, since it goes past the end of the screen
            // we have to do this here, since we don't know the width of the strings to be displayed before button creation
            if (btn != null && currentX >= _xEndPos - _xStartPos)
            {
                _predictionButtons.Remove(btn);
                btn.gameObject.SetActive(false);
                _unusedButtons.Push(btn);
            }
        }

        public void ClearPredictionButtons()
        {
            if (!_initialized)
                return;

            foreach (var oldButton in _predictionButtons)
            {
                oldButton.gameObject.SetActive(false);
                _unusedButtons.Push(oldButton);
            }
            _predictionButtons.Clear();
        }
    }
}
