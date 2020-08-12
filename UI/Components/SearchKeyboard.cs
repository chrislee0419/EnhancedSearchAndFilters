using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;
using BSUIUtilities = BS_Utils.Utilities.UIUtilities;
using BeatSaberMarkupLanguage;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal interface ISearchKeyboard
    {
        event Action<char> TextButtonPressed;
        event Action DeleteButtonPressed;
        event Action ClearButtonPressed;
        event Action FilterButtonPressed;
    }

    internal class SearchKeyboard : MonoBehaviour, ISearchKeyboard
    {
        public event Action<char> TextButtonPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action FilterButtonPressed;

        protected TextMeshProButton _symbolButton;
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

        protected virtual Vector2 DefaultKeySize => new Vector2(9f, 9f);
        protected virtual float DefaultFontSize => 4.5f;
        protected virtual float DefaultButtonScale => 0.85f;
        protected virtual float XOffset => 5f;
        protected virtual float YOffset => 0f;

        protected bool _symbolModeActive = false;
        protected TextMeshProButton _filterButton;

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

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        public virtual void Awake()
        {
            // create buttons for letters and their respective symbols
            TextMeshProButton button;
            Vector2 anchoredPos;
            float xKeySizeOffset = DefaultKeySize.x + 1f;
            float yKeySizeOffset = DefaultKeySize.y + 1f;
            float halfXKeySize = xKeySizeOffset / 2f;
            for (int i = 0; i < KeyArray.Length; ++i)
            {
                if (i < 10)
                    anchoredPos = new Vector2(i * xKeySizeOffset + XOffset, yKeySizeOffset * 3f + YOffset);
                else if (i < 19)
                    anchoredPos = new Vector2((i - 10) * xKeySizeOffset + halfXKeySize + XOffset, yKeySizeOffset * 2f + YOffset);
                else
                    anchoredPos = new Vector2((i - 19) * xKeySizeOffset + xKeySizeOffset + XOffset, yKeySizeOffset + YOffset);

                button = CreateKeyboardButton(anchoredPos, this.transform, KeyArray[i].ToUpper());
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

            CreateBottomButtons();

            // create buttons for numbers and their respective symbols
            for (int i = 1; i <= 10; i++)
            {
                button = CreateKeyboardButton(new Vector2((i - 1) * xKeySizeOffset + XOffset, yKeySizeOffset * 4 + YOffset), this.transform, i.ToString());

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

        protected virtual void CreateBottomButtons()
        {
            float yKeySizeOffset = DefaultKeySize.y + 1f;

            // create buttons for other stuff
            // backspace
            TextMeshProButton button = CreateKeyboardButton(new Vector2(85f, yKeySizeOffset + YOffset), new Vector2(22f, DefaultKeySize.y), 6f, this.transform, "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, YOffset), new Vector2(20f, DefaultKeySize.y), this.transform, "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // spacebar
            button = CreateKeyboardButton(new Vector2(21f, YOffset), new Vector2(44.5f, DefaultKeySize.y), this.transform, "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // to filter
            _filterButton = CreateKeyboardButton(new Vector2(66.5f, YOffset), new Vector2(22f, DefaultKeySize.y), 3.6f, this.transform, "To Filter");
            _filterButton.text.text = "To\nFilter";

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");
            filterIcon.preserveAspect = true;

            var layout = filterIcon.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 4f;
            layout.preferredHeight = 4f;

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
                handler.PointerEntered += () => filterIcon.color = _filterButton.button.interactable ? Color.black : Color.gray;
                handler.PointerExited += () => filterIcon.color = _filterButton.button.interactable ? Color.white : Color.gray;

                BeatmapDetailsLoader.instance.CachingStarted += OnCachingStarted;

                if (BeatmapDetailsLoader.instance.SongsAreCached)
                {
                    _filterButton.button.interactable = true;
                }
                else
                {
                    _filterButton.button.interactable = false;
                    filterIcon.color = Color.gray;
                    BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
                }
            }

            // clear
            button = CreateKeyboardButton(new Vector2(89.5f, 0f), new Vector2(20f, DefaultKeySize.y), this.transform, "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);
        }

        protected void InvokeTextButtonPressed(char c) => TextButtonPressed?.Invoke(c);
        protected void InvokeDeleteButtonPressed() => DeleteButtonPressed?.Invoke();
        protected void InvokeClearButtonPressed() => ClearButtonPressed?.Invoke();
        protected void InvokeFilterButtonPressed() => FilterButtonPressed?.Invoke();

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Transform parent = null, string name = null)
        {
            return CreateKeyboardButton(anchoredPosition, DefaultKeySize, DefaultFontSize, parent, name);
        }

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Vector2 sizeDelta, Transform parent = null, string name = null)
        {
            return CreateKeyboardButton(anchoredPosition, sizeDelta, DefaultFontSize, parent, name);
        }

        protected TextMeshProButton CreateKeyboardButton(Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize = -1f, Transform parent = null, string name = null)
        {
            if (_buttonPrefab == null)
                _buttonPrefab = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton");

            TextMeshProButton button = Instantiate(_buttonPrefab, parent != null ? parent : this.transform, false);

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

            UIUtilities.ScaleButton(button.button, DefaultButtonScale);

            return button;
        }

        protected virtual void OnDestroy()
        {
            if (BeatmapDetailsLoader.IsSingletonAvailable)
                BeatmapDetailsLoader.instance.CachingStarted -= OnCachingStarted;
        }

        public virtual void SetSymbolMode(bool useSymbols)
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

        public virtual void ResetSymbolMode() => SetSymbolMode(false);

        protected virtual void OnCachingStarted()
        {
            if (_filterButton != null)
                _filterButton.button.interactable = false;
            BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
        }

        protected virtual void OnCachingFinished()
        {
            _filterButton.button.interactable = true;
            Image icon = _filterButton.button.transform.parent.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "FilterIcon");
            if (icon != null)
                icon.color = Color.white;

            BeatmapDetailsLoader.instance.CachingFinished -= OnCachingFinished;
        }

        protected struct KeyPair
        {
            public TextMeshProButton button;
            public string key;
            public string symKey;
        }
    }

    internal class CompactSearchKeyboard : SearchKeyboard
    {
        protected override Vector2 DefaultKeySize => new Vector2(7f, 7f);
        protected override float DefaultFontSize => 3.8f;
        protected override float DefaultButtonScale => 0.65f;
        protected override float XOffset => 0f;
        protected override float YOffset => 5f;

        protected override void CreateBottomButtons()
        {
            // Backspace
            TextMeshProButton button = CreateKeyboardButton(new Vector2(64f, YOffset + DefaultKeySize.y + 1f), new Vector2(15f, DefaultKeySize.y), 4f, this.transform, "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, YOffset), new Vector2(15f, DefaultKeySize.y), 3.4f, this.transform, "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // Space bar
            button = CreateKeyboardButton(new Vector2(16f, YOffset), new Vector2(32f, DefaultKeySize.y), this.transform, "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // To Filter key
            _filterButton = CreateKeyboardButton(new Vector2(49f, YOffset), new Vector2(16f, DefaultKeySize.y), 3.2f, this.transform, "To Filter");
            _filterButton.text.text = "To\nFilter";

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = BSUIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");
            filterIcon.preserveAspect = true;

            var layout = filterIcon.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 3.5f;
            layout.preferredWidth = 3.5f;

            filterIcon.transform.SetParent(_filterButton.text.transform.parent, false);

            (_filterButton.text.transform.parent as RectTransform).sizeDelta += new Vector2(0f, 1.5f);

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
                handler.PointerEntered += () => filterIcon.color = _filterButton.button.interactable ? Color.black : Color.gray;
                handler.PointerExited += () => filterIcon.color = _filterButton.button.interactable ? Color.white : Color.gray;

                BeatmapDetailsLoader.instance.CachingStarted += OnCachingStarted;

                if (BeatmapDetailsLoader.instance.SongsAreCached)
                {
                    _filterButton.button.interactable = true;
                }
                else
                {
                    _filterButton.button.interactable = false;
                    filterIcon.color = Color.gray;
                    BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
                }
            }

            // Clear
            button = CreateKeyboardButton(new Vector2(66f, YOffset), new Vector2(13f, DefaultKeySize.y), this.transform, "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);
        }
    }

    internal class SearchFilterKeyboard : CompactSearchKeyboard
    {
        protected override Vector2 DefaultKeySize => new Vector2(7.5f, 7.5f);
        protected override float YOffset => 0f;

        protected override void CreateBottomButtons()
        {
            // Backspace
            TextMeshProButton button = CreateKeyboardButton(new Vector2(68f, YOffset + DefaultKeySize.y + 1f), new Vector2(16f, DefaultKeySize.y), this.transform, "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, YOffset), new Vector2(16f, DefaultKeySize.y), this.transform, "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.text.fontSize = 3f;
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // Space bar
            button = CreateKeyboardButton(new Vector2(17f, YOffset), new Vector2(51f, DefaultKeySize.y), this.transform, "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // Clear
            button = CreateKeyboardButton(new Vector2(69f, YOffset), new Vector2(16f, DefaultKeySize.y), this.transform, "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);
        }
    }

    internal class FloatingSearchKeyboard : SearchKeyboard
    {
        protected override Vector2 DefaultKeySize => new Vector2(9f, 7f);
        protected override float DefaultFontSize => 3.8f;
        protected override float DefaultButtonScale => 0.7f;
        protected override float XOffset => 5f;
        protected override float YOffset => 0f;

        protected override void CreateBottomButtons()
        {
            base.CreateBottomButtons();

            // roughly scaled from 3.6 * (1.0 / 0.7) * (3.8 / 4.5)
            _filterButton.text.fontSize = 4.3f;
        }
    }
}
