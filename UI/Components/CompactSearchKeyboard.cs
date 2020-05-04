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
        protected virtual bool ShowFilterButton => true;

        protected override Vector2 DefaultKeySize => new Vector2(7f, 7f);
        protected override float DefaultFontSize => 3.5f;
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
        private TextMeshProButton _filterButton;

        /// <summary>
        /// Indices 0-25 => a to z, indices 26-35 => 0 to 9
        /// </summary>
        private KeyPair[] _keyPairs = new KeyPair[36];

        public void Awake()
        {
            // work under the assumption that the box we can draw on is of size 80u x 60u
            TextMeshProButton button;
            Vector2 anchoredPos;
            for (int i = 0; i < KeyArray.Length; ++i)
            {
                if (i < 10)
                    anchoredPos = new Vector2(i * 8f, 29f);
                else if (i < 19)
                    anchoredPos = new Vector2((i - 10) * 8f + 4f, 21f);
                else
                    anchoredPos = new Vector2((i - 19) * 8f + 8f, 13f);

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

            // Backspace
            button = CreateKeyboardButton(new Vector2(64f, 13f), new Vector2(15f, 7f), "Backspace");
            button.text.text = "<-";
            button.button.onClick.AddListener(InvokeDeleteButtonPressed);

            // Space bar
            button = CreateKeyboardButton(new Vector2(15.5f, 5f), new Vector2(33f, 7f), "Spacebar");
            button.text.text = "Space";
            button.button.onClick.AddListener(() => InvokeTextButtonPressed(' '));

            // Symbols
            _symbolButton = CreateKeyboardButton(new Vector2(0f, 5f), new Vector2(15f, 7f), "Symbols");
            _symbolButton.text.text = "Symbols";
            _symbolButton.text.fontSize = 3f;
            _symbolButton.button.onClick.AddListener(() => SetSymbolMode(!_symbolModeActive));

            // Clear
            button = CreateKeyboardButton(new Vector2(66f, 5f), new Vector2(13f, 7f), "Clear");
            button.text.text = "Clear";
            button.button.onClick.AddListener(InvokeClearButtonPressed);

            // To Filter key
            _filterButton = CreateKeyboardButton(new Vector2(49.25f, 5f), new Vector2(16f, 7f), "To Filter");
            _filterButton.text.text = "To\nFilter";
            _filterButton.text.fontSize = 2.6f;

            Image filterIcon = new GameObject("FilterIcon").AddComponent<Image>();
            filterIcon.sprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.filter.png");

            var layout = filterIcon.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 3.5f;
            layout.preferredWidth = 3.5f;

            filterIcon.transform.SetParent(_filterButton.text.transform.parent, false);

            var handler = _filterButton.button.gameObject.AddComponent<EnterExitEventHandler>();
            handler.PointerEntered += () => filterIcon.color = Color.black;
            handler.PointerExited += () => filterIcon.color = Color.white;

            (_filterButton.text.transform.parent as RectTransform).sizeDelta += new Vector2(0f, 1.5f);

            _filterButton.button.onClick.AddListener(InvokeFilterButtonPressed);

            if (PluginConfig.DisableFilters)
            {
                _filterButton.button.interactable = false;
            }
            else
            {
                BeatmapDetailsLoader.instance.CachingStarted += OnCachingStarted;

                if (!BeatmapDetailsLoader.instance.SongsAreCached)
                {
                    _filterButton.button.interactable = false;
                    BeatmapDetailsLoader.instance.CachingFinished += OnCachingFinished;
                }
            }

            // Numbers
            for (int i = 1; i <= 10; ++i)
            {
                button = CreateKeyboardButton(new Vector2((i - 1) * 8f, 37f), i.ToString());

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
            BeatmapDetailsLoader.instance.CachingFinished -= OnCachingFinished;
        }
    }

    internal class SearchFilterKeyboard : CompactSearchKeyboard
    {
        protected override bool ShowFilterButton => false;
    }
}
