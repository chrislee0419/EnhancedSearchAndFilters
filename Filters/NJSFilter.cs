using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.UI;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class NJSFilter : IFilter
    {
        public string FilterName { get { return "Note Jump Speed (NJS)"; } }
        public FilterStatus Status {
            get
            {
                if (ApplyFilter)
                {
                    if (_minEnabledStagingValue != _minEnabledAppliedValue ||
                        _maxEnabledStagingValue != _maxEnabledAppliedValue ||
                        _minStagingValue != _minAppliedValue ||
                        _maxStagingValue != _maxAppliedValue)
                        return FilterStatus.AppliedAndChanged;

                    for (int i = 0; i < 5; ++i)
                    {
                        if (_difficultiesStagingValue[i] != _difficultiesAppliedValue[i])
                            return FilterStatus.AppliedAndChanged;
                    }

                    return FilterStatus.Applied;
                }
                else if ((_minEnabledStagingValue || _maxEnabledStagingValue) && _difficultiesStagingValue.Contains(true))
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool ApplyFilter {
            get
            {
                return (_minEnabledAppliedValue || _maxEnabledAppliedValue) && _difficultiesAppliedValue.Contains(true);
            }
            set
            {
                if (value)
                {
                    _minEnabledAppliedValue = _minEnabledStagingValue;
                    _maxEnabledAppliedValue = _maxEnabledStagingValue;
                    _minAppliedValue = _minStagingValue;
                    _maxAppliedValue = _maxStagingValue;

                    for (int i = 0; i < 5; ++i)
                        _difficultiesAppliedValue[i] = _difficultiesStagingValue[i];
                }
                else
                {
                    _minEnabledAppliedValue = false;
                    _maxEnabledAppliedValue = false;

                    for (int i = 0; i < 5; ++i)
                        _difficultiesAppliedValue[i] = false;
                }
            }
        }

        public FilterControl[] Controls { get; private set; } = new FilterControl[3];

        public event Action SettingChanged;

        ListViewController _minViewController;
        ListViewController _maxViewController;
        private Toggle[] _difficultyToggles = new Toggle[5];

        private bool _isInitialized = false;

        private bool _minEnabledStagingValue = false;
        private bool _maxEnabledStagingValue = false;
        private int _minStagingValue = DefaultMinValue;
        private int _maxStagingValue = DefaultMaxValue;
        private bool[] _difficultiesStagingValue = new bool[5];
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private int _minAppliedValue = DefaultMinValue;
        private int _maxAppliedValue = DefaultMaxValue;
        private bool[] _difficultiesAppliedValue = new bool[5];

        private const int DefaultMinValue = 10;
        private const int DefaultMaxValue = 20;
        private const int MinValue = 1;
        private const int MaxValue = 50;

        private static readonly string[] DifficultyStrings = new string[] { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };

        public void Init()
        {
            if (_isInitialized)
                return;

            SubMenu submenu = new SubMenu((Transform)null);

            // difficulties
            var difficultiesContainer = new GameObject("DifficultiesContainer");
            Controls[0] = new FilterControl(difficultiesContainer, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 26f), Vector2.zero,
                delegate ()
                {
                    for (int i = 0; i < 5; ++i)
                        _difficultyToggles[i].isOn = _difficultiesStagingValue[i];
                });

            // the container needs some graphical component to have the Transform to RectTransform cast work
            var unused = difficultiesContainer.AddComponent<Image>();
            unused.color = new Color(0f, 0f, 0f, 0f);

            var divider = new GameObject("Divider").AddComponent<Image>();
            divider.color = new Color(1f, 1f, 1f, 0.4f);
            divider.material = UIUtilities.NoGlowMaterial;

            var rt = divider.rectTransform;
            rt.SetParent(difficultiesContainer.transform);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 0.2f);
            rt.anchoredPosition = Vector2.zero;

            CreateDifficultyToggles(difficultiesContainer.transform);

            // minimum value setting
            float[] values = Enumerable.Range(MinValue, MaxValue).Select(x => (float)x).ToArray();
            _minViewController = submenu.AddList("Minimum NJS", values, "Filter out songs that have a smaller NJS than this value");
            _minViewController.GetTextForValue += x => ((int)x).ToString();
            _minViewController.GetValue += () => _minStagingValue;
            _minViewController.SetValue += delegate (float value)
            {
                if (_maxEnabledStagingValue && value > _maxStagingValue)
                {
                    _minStagingValue = _maxStagingValue;
                    RefreshUI();
                    return;
                }

                _minStagingValue = (int)value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _minViewController.Init();
            _minViewController.applyImmediately = true;

            var minToggle = CreateEnableToggle(_minViewController);
            minToggle.name = "MinValueToggle";
            minToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _minEnabledStagingValue = value;

                if (value && _maxEnabledStagingValue && _minStagingValue > _maxStagingValue)
                    _minStagingValue = _maxStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            MoveViewControllerElements(_minViewController);

            divider = new GameObject("Divider").AddComponent<Image>();
            divider.color = new Color(1f, 1f, 1f, 0.15f);
            divider.material = UIUtilities.NoGlowMaterial;

            rt = divider.rectTransform;
            rt.SetParent(_minViewController.transform);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-4f, 0.1f);
            rt.anchoredPosition = Vector2.zero;

            Controls[1] = new FilterControl(_minViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -26f),
                delegate ()
                {
                    // disabling buttons needs to be done after the view controller is enabled to override the interactable assignments of ListSettingsController:OnEnable()
                    _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _minEnabledStagingValue && _minStagingValue > MinValue;
                    _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _minEnabledStagingValue && (!_maxEnabledStagingValue || _minStagingValue < _maxStagingValue) && _minStagingValue < MaxValue;

                    minToggle.isOn = _minEnabledStagingValue;
                });

            // maximum value setting
            _maxViewController = submenu.AddList("Maximum NJS", values, "Filter out songs that have a larger NJS than this value");
            _maxViewController.GetTextForValue += x => ((int)x).ToString();
            _maxViewController.GetValue += () => _maxStagingValue;
            _maxViewController.SetValue += delegate (float value)
            {
                if (_minEnabledStagingValue && value < _minStagingValue)
                {
                    _maxStagingValue = _minStagingValue;
                    RefreshUI();
                    return;
                }

                _maxStagingValue = (int)value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _maxViewController.Init();
            _maxViewController.applyImmediately = true;

            var maxToggle = CreateEnableToggle(_maxViewController);
            maxToggle.name = "MaxValueToggle";
            maxToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _maxEnabledStagingValue = value;

                if (value && _minEnabledStagingValue && _maxStagingValue < _minStagingValue)
                    _maxStagingValue = _minStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            MoveViewControllerElements(_maxViewController);

            Controls[2] = new FilterControl(_maxViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -38f),
                delegate ()
                {
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _maxEnabledStagingValue && (!_minEnabledStagingValue || _maxStagingValue > _minStagingValue) && _maxStagingValue < MinValue;
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _maxEnabledStagingValue && _maxStagingValue < MaxValue;

                    maxToggle.isOn = _maxEnabledStagingValue;
                });

            _isInitialized = true;
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = false;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = false;

            foreach (var toggle in _difficultyToggles)
                toggle.isOn = false;

            RefreshUI();
            // don't need to invoke SettingsChanged here, since that will be handed by FilterViewController
        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = _minEnabledAppliedValue;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = _maxEnabledAppliedValue;

            for (int i = 0; i < 5; ++i)
                _difficultyToggles[i].isOn = _difficultiesAppliedValue[i];

            RefreshUI();
            // don't need to invoke SettingsChanged here, since that will be handed by FilterViewController
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            // don't need to check _isApplied, that's done outside of this module
            if ((!_isInitialized) ||
                (!_minEnabledAppliedValue && !_maxEnabledAppliedValue) ||
                (!_difficultiesAppliedValue.Aggregate((x, y) => x || y)))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails details = detailsList[i];

                // don't filter out OST beatmaps
                if (details.IsOST)
                {
                    ++i;
                    continue;
                }

                SimplifiedDifficultyBeatmapSet[] difficultySets = details.DifficultyBeatmapSets;

                bool remove = false;
                for (int j = 0; j < 5; ++j)
                {
                    if (!_difficultiesAppliedValue[j])
                        continue;

                    remove = difficultySets.Count(delegate (SimplifiedDifficultyBeatmapSet difficultySet)
                    {
                        return difficultySet.DifficultyBeatmaps.Count(delegate (SimplifiedDifficultyBeatmap difficulty)
                        {
                            return difficulty.Difficulty.ToString() == DifficultyStrings[j] && (difficulty.NoteJumpMovementSpeed < _minAppliedValue || difficulty.NoteJumpMovementSpeed > _maxAppliedValue);
                        }) > 0;
                    }) > 0;

                    if (remove)
                        break;
                }

                if (remove)
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }

        /// <summary>
        /// Used to move the buttons and text outside of the "Value" transform for a ListViewController. 
        /// This is done because the "Value" transform has some forced horizontal layout that messes up child RectTransform positioning.
        /// </summary>
        /// <param name="controller"></param>
        private void MoveViewControllerElements(ListViewController controller)
        {
            var incButton = controller.transform.Find("Value/IncButton") as RectTransform;
            incButton.SetParent(controller.transform);
            incButton.anchorMin = new Vector2(1f, 0.5f);
            incButton.anchorMax = new Vector2(1f, 0.5f);
            incButton.pivot = new Vector2(1f, 0.5f);
            incButton.sizeDelta = new Vector2(8f, 8f);
            incButton.anchoredPosition = Vector2.zero;

            var text = controller.transform.Find("Value/ValueText") as RectTransform;
            text.SetParent(controller.transform);
            text.anchorMin = new Vector2(1f, 0.5f);
            text.anchorMax = new Vector2(1f, 0.5f);
            text.pivot = new Vector2(1f, 0.5f);
            text.sizeDelta = new Vector2(16f, 8f);
            text.anchoredPosition = new Vector2(-8f, 0f);

            var decButton = controller.transform.Find("Value/DecButton") as RectTransform;
            decButton.SetParent(controller.transform);
            decButton.anchorMin = new Vector2(1f, 0.5f);
            decButton.anchorMax = new Vector2(1f, 0.5f);
            decButton.pivot = new Vector2(1f, 0.5f);
            decButton.sizeDelta = new Vector2(8f, 8f);
            decButton.anchoredPosition = new Vector2(-24f, 0f);

            var toggle = controller.transform.Find("Value/MinValueToggle") as RectTransform ?? controller.transform.Find("Value/MaxValueToggle") as RectTransform;
            toggle.SetParent(controller.transform);
            toggle.anchorMin = new Vector2(1f, 0.5f);
            toggle.anchorMax = new Vector2(1f, 0.5f);
            toggle.pivot = new Vector2(1f, 0.5f);
            toggle.sizeDelta = new Vector2(8f, 8f);
            toggle.anchoredPosition = new Vector2(-34f, 0f);
        }

        private Toggle CreateEnableToggle(ListViewController controller)
        {
            var gameplayToggle = Utilities.GetTogglePrefab();

            var toggle = Utilities.CreateToggleFromPrefab(gameplayToggle.toggle, controller.transform.Find("Value"));

            Object.Destroy(gameplayToggle);

            return toggle;
        }

        private void CreateDifficultyToggles(Transform parent)
        {
            var gameplayToggle = Utilities.GetTogglePrefab();

            var text = BeatSaberUI.CreateText(parent as RectTransform, "Difficulties To Filter NJS", Vector2.zero, new Vector2(50f, 6f));
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.fontSize = 5.5f;
            BeatSaberUI.AddHintText(text.rectTransform, "Song is kept if at least one of the checked difficulties passes the filter. Songs without a checked difficulty and OST songs are not removed.");

            // easy
            var toggle = CreateToggleFromPrefab(gameplayToggle.toggle, parent, Vector2.zero, new Vector2(0.2f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[0] = value;

                if (_minEnabledStagingValue || _maxEnabledStagingValue)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[0] = toggle;

            text = BeatSaberUI.CreateText(parent as RectTransform, "Easy", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.1f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.1f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // normal
            toggle = CreateToggleFromPrefab(gameplayToggle.toggle, parent, new Vector2(0.2f, 0f), new Vector2(0.4f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[1] = value;

                if (_minEnabledStagingValue || _maxEnabledStagingValue)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[1] = toggle;

            text = BeatSaberUI.CreateText(parent as RectTransform, "Normal", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.3f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.3f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // hard
            toggle = CreateToggleFromPrefab(gameplayToggle.toggle, parent, new Vector2(0.4f, 0f), new Vector2(0.6f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[2] = value;

                if (_minEnabledStagingValue || _maxEnabledStagingValue)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[2] = toggle;

            text = BeatSaberUI.CreateText(parent as RectTransform, "Hard", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // expert
            toggle = CreateToggleFromPrefab(gameplayToggle.toggle, parent, new Vector2(0.6f, 0f), new Vector2(0.8f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[3] = value;

                if (_minEnabledStagingValue || _maxEnabledStagingValue)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[3] = toggle;

            text = BeatSaberUI.CreateText(parent as RectTransform, "Expert", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.7f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.7f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // expert+
            toggle = CreateToggleFromPrefab(gameplayToggle.toggle, parent, new Vector2(0.8f, 0f), new Vector2(1f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[4] = value;

                if (_minEnabledStagingValue || _maxEnabledStagingValue)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[4] = toggle;

            text = BeatSaberUI.CreateText(parent as RectTransform, "Expert+", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.9f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.9f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            Object.Destroy(gameplayToggle.gameObject);
        }

        private Toggle CreateToggleFromPrefab(Toggle prefab, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var toggle = Utilities.CreateToggleFromPrefab(prefab, parent);

            var rt = toggle.transform as RectTransform;
            rt.anchorMin = anchorMin + ((anchorMax - anchorMin) / 2f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(6f, 6f);
            rt.anchoredPosition = Vector2.zero;

            return toggle;
        }

        private void RefreshUI(bool init = true, bool refreshValue = false)
        {
            if (init)
            {
                if (!refreshValue)
                {
                    // disable applyImmediately temporarily, otherwise Init() triggers a call to SetValue, which also calls RefreshUI()
                    _minViewController.applyImmediately = false;
                    _maxViewController.applyImmediately = false;
                }

                _minViewController.Init();
                _maxViewController.Init();

                if (!refreshValue)
                {
                    _minViewController.applyImmediately = true;
                    _maxViewController.applyImmediately = true;
                }
            }

            // reset button state
            _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _minEnabledStagingValue && _minStagingValue > MinValue;
            _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _minEnabledStagingValue && (!_maxEnabledStagingValue || _minStagingValue < _maxStagingValue) && _minStagingValue < MaxValue;
            _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _maxEnabledStagingValue && (!_minEnabledStagingValue || _maxStagingValue > _minStagingValue) && _maxStagingValue > MinValue;
            _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _maxEnabledStagingValue && _maxStagingValue < MaxValue;
        }
    }
}
