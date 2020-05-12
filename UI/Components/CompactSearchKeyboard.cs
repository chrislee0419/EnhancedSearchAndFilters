using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class CompactSearchKeyboard : SearchKeyboardBase
    {
        protected override Vector2 DefaultKeySize => new Vector2(7f, 7f);
        protected override float DefaultFontSize => 3.5f;
        protected virtual float YOffset => 5f;
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

        protected bool _symbolModeActive = false;

        protected TextMeshProButton _symbolButton;
        protected TextMeshProButton _filterButton;

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        public void Awake()
        {
            // work under the assumption that the box we can draw on is of size 80u x 60u
            TextMeshProButton button;
            Vector2 anchoredPos;
            float keySize = DefaultKeySize.x + 1f;
            for (int i = 0; i < KeyArray.Length; ++i)
            {
                if (i < 10)
                    anchoredPos = new Vector2(i * keySize, keySize * 3 + YOffset);
                else if (i < 19)
                    anchoredPos = new Vector2((i - 10) * keySize + keySize / 2f, keySize * 2 + YOffset);
                else
                    anchoredPos = new Vector2((i - 19) * keySize + keySize, keySize + YOffset);

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

            CreateBottomButtons();

            // Numbers
            for (int i = 1; i <= 10; ++i)
            {
                button = CreateKeyboardButton(new Vector2((i - 1) * keySize, keySize * 4 + YOffset), i.ToString());

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
            // Backspace
            TextMeshProButton button = CreateKeyboardButton(new Vector2(64f, YOffset + DefaultKeySize.y + 1f), new Vector2(15f, DefaultKeySize.y), "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, YOffset), new Vector2(15f, DefaultKeySize.y), "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.text.fontSize = 3f;
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // Space bar
            button = CreateKeyboardButton(new Vector2(16f, YOffset), new Vector2(32f, DefaultKeySize.y), "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // To Filter key
            _filterButton = CreateKeyboardButton(new Vector2(49f, YOffset), new Vector2(16f, DefaultKeySize.y), "To Filter");
            _filterButton.text.text = "To\nFilter";
            _filterButton.text.fontSize = 2.6f;

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");
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
            button = CreateKeyboardButton(new Vector2(66f, 5f), new Vector2(13f, 7f), "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);
        }

        private void OnDestroy()
        {
            if (BeatmapDetailsLoader.IsSingletonAvailable)
                BeatmapDetailsLoader.instance.CachingStarted -= OnCachingStarted;
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

        private void OnCachingStarted()
        {
            if (_filterButton != null)
                _filterButton.button.interactable = false;
            BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
        }

        private void OnCachingFinished()
        {
            _filterButton.button.interactable = true;
            Image icon = _filterButton.button.transform.parent.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "FilterIcon");
            if (icon != null)
                icon.color = Color.white;

            BeatmapDetailsLoader.instance.CachingFinished -= OnCachingFinished;
        }
    }

    internal class SearchFilterKeyboard : CompactSearchKeyboard
    {
        protected override Vector2 DefaultKeySize => new Vector2(7.5f, 7.5f);
        protected override float YOffset => 0f;

        protected override void CreateBottomButtons()
        {
            // Backspace
            TextMeshProButton button = CreateKeyboardButton(new Vector2(68f, YOffset + DefaultKeySize.y + 1f), new Vector2(16f, DefaultKeySize.y), "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, YOffset), new Vector2(16f, DefaultKeySize.y), "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.text.fontSize = 3f;
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // Space bar
            button = CreateKeyboardButton(new Vector2(17f, YOffset), new Vector2(51f, DefaultKeySize.y), "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // Clear
            button = CreateKeyboardButton(new Vector2(69f, YOffset), new Vector2(16f, DefaultKeySize.y), "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);
        }
    }
}
