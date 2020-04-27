using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BS_Utils.Utilities;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class CompactSearchKeyboard : MonoBehaviour
    {
        public event Action<char> TextKeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action FilterButtonPressed;

        public bool SymbolButtonInteractivity
        {
            get
            {
                return _symbolButton.button.interactable;
            }
            set
            {
                _symbolButton.button.interactable = value;
                //if (!value)
                //    ResetSymbolMode();
            }
        }

        private bool _symbolModeActive = false;
        private TextMeshProButton _symbolButton;

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        private TextMeshProButton _buttonPrefab;

        public void Awake()
        {
            // clear all existing buttons
            int childCount = this.transform.childCount;
            for (int i = 0; i < childCount; ++i)
                Destroy(this.transform.GetChild(i).gameObject);

            _buttonPrefab = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton");

            // work under the assumption that the box we can draw on is of size 80u x 60u
            TextMeshProButton button;
            string[] keyArray = new string[]
            {
                "q", "w", "e", "r", "t", "y", "u", "i", "o",  "p",
                "a", "s", "d", "f", "g", "h", "j", "k", "l",
                "z", "x", "c", "v", "b", "n", "m"
            };
            string[] symbolArray = new string[]
            {
                "[", "]", "{", "}", "\\", "|", "-", "_", "=", "+",
                "", "", "", "", "", ";", ":", "'", "\"",
                "", ",", ".", "<", ">", "/", "?"
            };
            for (int i = 0; i < keyArray.Length; ++i)
            {
                Vector2 anchoredPos;

                if (i < 10)
                    anchoredPos = new Vector2(i * 8f, 29f);
                else if (i < 19)
                    anchoredPos = new Vector2((i - 10) * 8f + 4f, 21f);
                else
                    anchoredPos = new Vector2((i - 19) * 8f + 8f, 13f);

                button = CreateKeyboardButton(anchoredPos, keyArray[i].ToUpper());

                button.text.text = keyArray[i];

                string key = keyArray[i];
                string symbol = symbolArray[i];

                _keyPairs[i].button = button;
                _keyPairs[i].key = key;
                _keyPairs[i].symKey = symbol;

                button.button.onClick.AddListener(delegate ()
                {
                    if (_symbolModeActive)
                    {
                        if (!string.IsNullOrEmpty(symbol))
                            TextKeyPressed?.Invoke(symbol[0]);
                    }
                    else
                    {
                        TextKeyPressed?.Invoke(key[0]);
                    }
                });
            }

            // Backspace
            button = CreateKeyboardButton(new Vector2(64f, 13f), new Vector2(15f, 7f), "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(delegate ()
            {
                DeleteButtonPressed?.Invoke();
            });

            // Space bar
            button = CreateKeyboardButton(new Vector2(15.5f, 5f), new Vector2(33f, 7f), "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(delegate ()
            {
                TextKeyPressed?.Invoke(' ');
            });

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, 5f), new Vector2(15f, 7f), "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.text.fontSize = 3f;
            _symbolButton.button.onClick.AddListener(delegate ()
            {
                SetSymbolMode(!_symbolModeActive);
            });

            // Clear
            button = CreateKeyboardButton(new Vector2(66f, 5f), new Vector2(13f, 7f), "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(delegate ()
            {
                ClearButtonPressed?.Invoke();
            });

            // To Filter key
            button = CreateKeyboardButton(new Vector2(49.25f, 5f), new Vector2(16f, 7f), "To Filter");
            button.text.text = "To\nFilter";
            button.text.fontSize = 2.6f;

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");

            var layout = filterIcon.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 3.5f;
            layout.preferredWidth = 3.5f;

            filterIcon.transform.SetParent(button.text.transform.parent, false);

            var handler = button.button.gameObject.AddComponent<EnterExitEventHandler>();
            handler.PointerEntered += () => filterIcon.color = Color.black;
            handler.PointerExited += () => filterIcon.color = Color.white;

            (button.text.transform.parent as RectTransform).sizeDelta += new Vector2(0f, 1.5f);

            button.button.onClick.AddListener(() => FilterButtonPressed?.Invoke());

            // Numbers
            string[] numSymbolArray = new string[]
            {
                "!", "@", "#", "$", "%", "^", "&", "*", "(", ")"
            };
            for (int i = 1; i <= 10; ++i)
            {
                button = CreateKeyboardButton(new Vector2((i - 1) * 8f, 37f), i.ToString());

                string key = i.ToString().Last().ToString();
                string symbol = numSymbolArray[i - 1];
                int index = (i % 10) + 26;

                _keyPairs[index].key = key;
                _keyPairs[index].symKey = symbol;
                _keyPairs[index].button = button;

                button.text.text = key;
                button.button.onClick.AddListener(delegate ()
                {
                    if (_symbolModeActive)
                        TextKeyPressed?.Invoke(symbol[0]);
                    else
                        TextKeyPressed?.Invoke(key[0]);
                });
            }
        }

        private TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, string name=null)
        {
            return CreateKeyboardButton(anchoredPosition, new Vector2(7f, 7f), name);
        }

        private TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Vector2 sizeDelta, string name=null)
        {
            TextMeshProButton button = Instantiate(_buttonPrefab, this.transform, false);

            // use bottom left edge as origin
            RectTransform rt = button.transform as RectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            Navigation nav = button.button.navigation;
            nav.mode = Navigation.Mode.None;
            button.button.navigation = nav;

            button.text.fontSize = 3.5f;
            button.button.onClick.RemoveAllListeners();

            if (!string.IsNullOrEmpty(name))
                button.gameObject.name = name;

            return button;
        }

        public void SetSymbolMode(bool useSymbols)
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
