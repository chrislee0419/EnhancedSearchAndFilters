using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal interface ISearchKeyboard
    {
        event Action<char> TextButtonPressed;
        event Action DeleteButtonPressed;
        event Action ClearButtonPressed;
        event Action FilterButtonPressed;
    }

    internal abstract class SearchKeyboardBase : MonoBehaviour, ISearchKeyboard
    {
        public event Action<char> TextButtonPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action FilterButtonPressed;

        protected virtual Vector2 DefaultKeySize => new Vector2(9f, 9f);
        protected virtual float DefaultFontSize => 4.5f;

        protected TextMeshProButton _buttonPrefab;

        protected readonly static string[] KeyArray = new string[]
        {
            "q", "w", "e", "r", "t", "y", "u", "i", "o",  "p",
            "a", "s", "d", "f", "g", "h", "j", "k", "l",
            "z", "x", "c", "v", "b", "n", "m"
        };
        protected readonly static string[] ButtonArray = new string[]
        {
            "<-", "Space", "To\nFilter", "Symbols", "Clear"
        };
        protected readonly static string[] SymbolArray = new string[]
        {
            "[", "]", "{", "}", "\\", "|", "-", "_", "=", "+",
            "", "", "", "", "", ";", ":", "'", "\"",
            "", ",", ".", "<", ">", "/", "?"
        };
        protected readonly static string[] NumberSymbolArray = new string[]
        {
            "!", "@", "#", "$", "%", "^", "&", "*", "(", ")"
        };

        protected void InvokeTextButtonPressed(char c) => TextButtonPressed?.Invoke(c);
        protected void InvokeDeleteButtonPressed() => DeleteButtonPressed?.Invoke();
        protected void InvokeClearButtonPressed() => ClearButtonPressed?.Invoke();
        protected void InvokeFilterButtonPressed() => FilterButtonPressed?.Invoke();

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, string name = null)
        {
            return CreateKeyboardButton(anchoredPosition, DefaultKeySize, DefaultFontSize, name);
        }

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Vector2 sizeDelta, string name = null)
        {
            return CreateKeyboardButton(anchoredPosition, sizeDelta, DefaultFontSize, name);
        }

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize = -1f, string name = null)
        {
            if (_buttonPrefab == null)
                _buttonPrefab = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton");

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

            button.text.fontSize = fontSize > 0f ? fontSize : DefaultFontSize;
            button.button.onClick.RemoveAllListeners();

            if (!string.IsNullOrEmpty(name))
                button.gameObject.name = name;

            return button;
        }

        protected struct KeyPair
        {
            public TextMeshProButton button;
            public string key;
            public string symKey;
        }
    }

    internal class SearchKeyboard : SearchKeyboardBase
    {
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
        private TextMeshProButton _filterButton;

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        // keyboard should be of size 110w x 50h
        public void Awake()
        {
            // create buttons for letters and their respective symbols
            TextMeshProButton button;
            Vector2 anchoredPos;
            float xOffset = 5f;
            float keySize = DefaultKeySize.x + 1f;
            float halfKeySize = keySize / 2f;
            for (int i = 0; i < KeyArray.Length; ++i)
            {
                if (i < 10)
                    anchoredPos = new Vector2(i * keySize + xOffset, keySize * 3f);
                else if (i < 19)
                    anchoredPos = new Vector2((i - 10) * keySize + halfKeySize + xOffset, keySize * 2f);
                else
                    anchoredPos = new Vector2((i - 19) * keySize + keySize + xOffset, keySize);

                button = CreateKeyboardButton(anchoredPos, KeyArray[i].ToUpper());
                button.text.text = KeyArray[i];

                string key = KeyArray[i];
                string symbol = SymbolArray[i];
                _keyPairs[i].button = button;
                _keyPairs[i].key = key;
                _keyPairs[i].symKey = symbol;

                button.button.onClick.AddListener(delegate ()
                {
                    if (_symbolModeActive)
                    {
                        if (!string.IsNullOrEmpty(symbol))
                            InvokeTextButtonPressed(symbol[0]);
                    }
                    else
                    {
                        InvokeTextButtonPressed(key[0]);
                    }
                });
            }

            // create buttons for other stuff
            // backspace
            button = CreateKeyboardButton(new Vector2(85f, keySize), new Vector2(22f, 9f), 6f, "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // symbols
            _symbolButton = CreateKeyboardButton(Vector2.zero, new Vector2(20f, 9f), "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // spacebar
            button = CreateKeyboardButton(new Vector2(21f, 0f), new Vector2(44.5f, 9f), "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // to filter
            _filterButton = CreateKeyboardButton(new Vector2(66.5f, 0f), new Vector2(22f, 9f), "To Filter");
            _filterButton.text.text = "To\nFilter";
            _filterButton.text.fontSize = 3.5f;

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");

            var layout = filterIcon.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 4;
            layout.preferredHeight = 4;

            filterIcon.transform.SetParent(_filterButton.text.transform.parent, false);

            _filterButton.button.onClick.AddListener(delegate ()
            {
                filterIcon.color = Color.white;
                InvokeFilterButtonPressed();
            });

            if (PluginConfig.DisableFilters)
            {
                _filterButton.button.interactable = false;
                filterIcon.color = Color.gray;
            }
            else
            {
                var handler = _filterButton.button.gameObject.AddComponent<EnterExitEventHandler>();
                handler.PointerEntered += () => filterIcon.color = Color.black;
                handler.PointerExited += () => filterIcon.color = Color.white;

                BeatmapDetailsLoader.instance.CachingStarted += OnCachingStarted;

                if (BeatmapDetailsLoader.instance.SongsAreCached)
                {
                    _filterButton.button.interactable = true;
                }
                else
                {
                    _filterButton.button.interactable = false;
                    BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
                }
            }

            // clear
            button = CreateKeyboardButton(new Vector2(89.5f, 0f), new Vector2(20f, 9f), "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);

            // create buttons for numbers and their respective symbols
            for (int i = 1; i <= 10; i++)
            {
                button = CreateKeyboardButton(new Vector2((i - 1) * keySize + xOffset, keySize * 4), i.ToString());

                string key = i.ToString().Last().ToString();
                string symbol = NumberSymbolArray[i - 1];
                int index = (i % 10) + 26;

                _keyPairs[index].key = key;
                _keyPairs[index].symKey = symbol;
                _keyPairs[index].button = button;

                button.text.text = key;
                button.button.onClick.AddListener(delegate ()
                {
                    if (_symbolModeActive)
                        InvokeTextButtonPressed(symbol[0]);
                    else
                        InvokeTextButtonPressed(key[0]);
                });
            }
        }

        private void OnDestroy()
        {
            if (BeatmapDetailsLoader.IsSingletonAvailable)
                BeatmapDetailsLoader.instance.CachingStarted -= OnCachingStarted;
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

        private void OnCachingStarted()
        {
            if (_filterButton != null)
                _filterButton.button.interactable = false;
            BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
        }

        private void OnCachingFinished()
        {
            _filterButton.button.interactable = true;
            BeatmapDetailsLoader.instance.CachingFinished -= OnCachingFinished;
        }
    }
}
