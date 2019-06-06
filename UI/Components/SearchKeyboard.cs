using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal struct KeyPair
    {
        public TextMeshProButton button;
        public string key;
        public string symKey;
    }

    class SearchKeyboard : MonoBehaviour
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;

        public bool SymbolButtonInteractivity
        {
            get
            {
                return _symbolButton.button.interactable;
            }
            set
            {
                _symbolButton.button.interactable = value;
                if (!value)
                    ResetSymbolMode();
            }
        }

        private bool _symbolModeActive = false;

        private TextMeshProButton _symbolButton;

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        public void Awake()
        {
            // create buttons for letters and their respective symbols
            string[] keyArray = new string[]
            {
                "q", "w", "e", "r", "t", "y", "u", "i", "o",  "p",
                "a", "s", "d", "f", "g", "h", "j", "k", "l",
                "z", "x", "c", "v", "b", "n", "m",
                "<-", "Space", /*"OK", "Cancel",*/ "Symbols", "Clear"
            };
            string[] symbolArray = new string[]
            {
                "[", "]", "{", "}", "\\", "|", "-", "_", "=", "+",
                "", "", "", "", "", ";", ":", "'", "\"",
                "", ",", ".", "<", ">", "/", "?"
            };
            for (int i = 0; i < keyArray.Length; i++)
            {
                TextMeshProButton textMeshProButton;
                if (i < keyArray.Length - 2)
                {
                    textMeshProButton = CreateKeyboardButton(i);
                }
                else if (i == keyArray.Length - 2)
                {
                    // Symbol button adapted from Cancel button
                    textMeshProButton = CreateKeyboardButton(29);
                }
                else
                {
                    // Clear button adapted from Delete button
                    textMeshProButton = CreateKeyboardButton(26, true);
                }
                
                textMeshProButton.text.text = keyArray[i];

                if (i < keyArray.Length - 4)
                {
                    string key = keyArray[i];
                    string symbolKey = symbolArray[i];

                    _keyPairs[i].button = textMeshProButton;
                    _keyPairs[i].key = key;
                    _keyPairs[i].symKey = symbolArray[i];

                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        if (_symbolModeActive)
                        {
                            if (!string.IsNullOrEmpty(symbolKey))
                                TextKeyPressed?.Invoke(symbolKey[0]);
                        }
                        else
                        {
                            TextKeyPressed?.Invoke(key[0]);
                        }
                    });
                }
                else if (i == keyArray.Length - 4)
                {
                    // Delete/Backspace button
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        DeleteButtonPressed?.Invoke();
                    });
                }
                else if (i == keyArray.Length - 3)
                {
                    // Space key
                    (textMeshProButton.button.transform as RectTransform).sizeDelta = new Vector2(10f, 0f);
                    (textMeshProButton.button.transform as RectTransform).anchoredPosition += new Vector2(5f, 0f);
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        TextKeyPressed?.Invoke(' ');
                    });
                }
                //else if (i == keyArray.Length - 3)
                //{
                //    // Search button
                //    (textMeshProButton.transform as RectTransform).sizeDelta = new Vector2(4f, 2f);
                //    (textMeshProButton.transform as RectTransform).anchoredPosition += new Vector2(26f, -1f);
                //    textMeshProButton.text.fontSize = 6f;

                //    _searchButton = textMeshProButton.button;
                //    _searchButton.onClick.AddListener(delegate ()
                //    {
                //        SearchButtonPressed?.Invoke();
                //    });
                //    SearchButtonInteractivity = false;
                //}
                else if (i == keyArray.Length - 2)
                {
                    // Symbols button
                    (textMeshProButton.transform as RectTransform).sizeDelta = new Vector2(10f, 0f);
                    (textMeshProButton.transform as RectTransform).anchoredPosition += new Vector2(-2f, 0f);

                    _symbolButton = textMeshProButton;
                    _symbolButton.button.onClick.AddListener(delegate ()
                    {
                        SetSymbolMode(!_symbolModeActive);
                    });
                }
                else
                {
                    // Clear button
                    (textMeshProButton.transform as RectTransform).sizeDelta = new Vector2(6f, 0);
                    (textMeshProButton.transform as RectTransform).anchoredPosition += new Vector2(2f, -10f);
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        ClearButtonPressed?.Invoke();
                    });
                }
            }

            // destroy the existing "OK" button
            Destroy(this.transform.GetChild(28).gameObject);

            // create buttons for numbers and their respective symbols
            TextMeshProButton keyButtonPrefab = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton");
            string[] numSymbolArray = new string[]
            {
                "!", "@", "#", "$", "%", "^", "&", "*", "(", ")"
            };

            for (int i = 1; i <= 10; i++)
            {
                TextMeshProButton textButton = Instantiate(keyButtonPrefab);
                string key = i.ToString().Last().ToString();
                string symbolKey = numSymbolArray[i - 1];
                int index = (i % 10) + 26;

                _keyPairs[index].key = key;
                _keyPairs[index].symKey = symbolKey;
                _keyPairs[index].button = textButton;

                textButton.text.text = key;
                textButton.button.onClick.AddListener(delegate ()
                {
                    if (_symbolModeActive)
                        TextKeyPressed?.Invoke(symbolKey[0]);
                    else
                        TextKeyPressed?.Invoke(key[0]);
                });

                RectTransform buttonRect = textButton.GetComponent<RectTransform>();
                RectTransform component2 = this.transform.GetChild(i - 1).gameObject.GetComponent<RectTransform>();

                RectTransform buttonHolder = Instantiate(component2, component2.parent, false);
                Destroy(buttonHolder.GetComponentInChildren<Button>().gameObject);

                buttonHolder.anchoredPosition -= new Vector2(0f, -10.5f);

                buttonRect.SetParent(buttonHolder, false);

                buttonRect.localPosition = Vector2.zero;
                buttonRect.localScale = Vector3.one;
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.anchorMin = Vector2.zero;
                buttonRect.anchorMax = Vector3.one;
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
            }

            name = "EnhancedSearchKeyboard";
        }

        private TextMeshProButton CreateKeyboardButton(int childTransformIndex, bool duplicate = false)
        {
            RectTransform parent = this.transform.GetChild(childTransformIndex) as RectTransform;
            TextMeshProButton textMeshProButton = parent.GetComponentInChildren<TextMeshProButton>();
            if (duplicate)
                textMeshProButton = Instantiate(textMeshProButton, parent, false);

            RectTransform rectTransform = textMeshProButton.transform as RectTransform;
            rectTransform.localPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector3.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Navigation navigation = textMeshProButton.button.navigation;
            navigation.mode = Navigation.Mode.None;
            textMeshProButton.button.navigation = navigation;
            textMeshProButton.button.onClick.RemoveAllListeners();
            return textMeshProButton;
        }

        private void SetSymbolMode(bool useSymbols)
        {
            if (useSymbols)
            {
                foreach (KeyPair kp in _keyPairs)
                {
                    kp.button.text.text = kp.symKey;

                    if (string.IsNullOrEmpty(kp.symKey))
                        kp.button.button.interactable = false;
                }
                _symbolButton.text.text = "Alpha";
            }
            else
            {
                foreach (KeyPair kp in _keyPairs)
                {
                    kp.button.text.text = kp.key;
                    kp.button.button.interactable = true;
                }
                _symbolButton.text.text = "Symbols";
            }

            _symbolModeActive = useSymbols;
        }

        public void ResetSymbolMode()
        {
            SetSymbolMode(false);
        }
    }
}
