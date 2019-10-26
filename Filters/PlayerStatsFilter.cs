using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using EnhancedSearchAndFilters.UI;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class PlayerStatsFilter : IFilter
    {
        public string FilterName { get { return "Player Stats"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (ApplyFilter)
                {
                    if (_hasCompletedStagingValue != _hasCompletedAppliedValue ||
                        _hasFullComboStagingValue != _hasFullComboAppliedValue)
                        return FilterStatus.AppliedAndChanged;

                    for (int i = 0; i < 5; ++i)
                    {
                        if (_difficultiesStagingValue[i] != _difficultiesAppliedValue[i])
                            return FilterStatus.AppliedAndChanged;
                    }

                    return FilterStatus.Applied;
                }
                else if (_hasCompletedStagingValue != SongCompletedFilterOption.Off ||
                    _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool ApplyFilter
        {
            get
            {
                return _hasCompletedAppliedValue != SongCompletedFilterOption.Off ||
                    _hasFullComboAppliedValue != SongFullComboFilterOption.Off;
            }
            set
            {
                if (value)
                {
                    _hasCompletedAppliedValue = _hasCompletedStagingValue;
                    _hasFullComboAppliedValue = _hasFullComboStagingValue;

                    for (int i = 0; i < 5; ++i)
                        _difficultiesAppliedValue[i] = _difficultiesStagingValue[i];
                }
                else
                {
                    _hasCompletedAppliedValue = SongCompletedFilterOption.Off;
                    _hasFullComboAppliedValue = SongFullComboFilterOption.Off;

                    for (int i = 0; i < 5; ++i)
                        _difficultiesAppliedValue[i] = false;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[4];

        public event Action SettingChanged;

        private ListViewController _hasCompletedViewController;
        private ListViewController _hasFullComboViewController;
        private Toggle[] _difficultyToggles = new Toggle[5];

        private SongCompletedFilterOption _hasCompletedStagingValue = SongCompletedFilterOption.Off;
        private SongFullComboFilterOption _hasFullComboStagingValue = SongFullComboFilterOption.Off;
        private bool[] _difficultiesStagingValue = new bool[5];
        private SongCompletedFilterOption _hasCompletedAppliedValue = SongCompletedFilterOption.Off;
        private SongFullComboFilterOption _hasFullComboAppliedValue = SongFullComboFilterOption.Off;
        private bool[] _difficultiesAppliedValue = new bool[5];

        private bool _isInitialized = false;

        public void Init()
        {
            if (_isInitialized)
                return;

            // title text
            var text = BeatSaberUI.CreateText(null, "Filter According To Your Play Stats", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // has completed filter
            var values = Enumerable.Range(0, 3).Select(x => (float)x).ToArray();
            _hasCompletedViewController = Utilities.CreateListViewController("Songs Completed At Least Once", values, "Filters out songs that you have completed at least once difficulty/have not completed");
            _hasCompletedViewController.GetTextForValue += delegate (float value)
            {
                if (value == (float)SongCompletedFilterOption.HasNeverCompleted)
                    return "<size=62%>Keep Never Completed</size>"; 
                else if (value == (float)SongCompletedFilterOption.HasCompleted)
                    return "<size=62%>Keep Completed</size>";
                else
                    return "OFF";
            };
            _hasCompletedViewController.GetValue += () => (float)_hasCompletedStagingValue;
            _hasCompletedViewController.SetValue += delegate (float value)
            {
                _hasCompletedStagingValue = (SongCompletedFilterOption)value;
                SettingChanged?.Invoke();
            };
            _hasCompletedViewController.Init();
            _hasCompletedViewController.applyImmediately = true;

            Utilities.MoveIncDecViewControllerElements(_hasCompletedViewController);
            Utilities.CreateHorizontalDivider(_hasCompletedViewController.transform);

            Controls[1] = new FilterControl(_hasCompletedViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f));

            // has full combo filter
            _hasFullComboViewController = Utilities.CreateListViewController("Songs With Full Combo", values, "Filters out songs that you have completed with a full combo/without a full combo (ignores lightmaps)");
            _hasFullComboViewController.GetTextForValue += delegate (float value)
            {
                if (value == (float)SongFullComboFilterOption.HasNoFullCombo)
                    return "<size=75%>Keep Songs Without FC</size>";
                else if (value == (float)SongFullComboFilterOption.HasFullCombo)
                    return "<size=75%>Keep Songs With FC</size>";
                else
                    return "OFF";
            };
            _hasFullComboViewController.GetValue += () => (float)_hasFullComboStagingValue;
            _hasFullComboViewController.SetValue += delegate (float value)
            {
                _hasFullComboStagingValue = (SongFullComboFilterOption)value;
                SettingChanged?.Invoke();
            };
            _hasFullComboViewController.Init();
            _hasFullComboViewController.applyImmediately = true;

            Utilities.MoveIncDecViewControllerElements(_hasFullComboViewController);

            Controls[2] = new FilterControl(_hasFullComboViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -20f));

            // difficulties
            var difficultiesContainer = new GameObject("PlayerStatsFilterDifficultiesContainer");
            Controls[3] = new FilterControl(difficultiesContainer, new Vector2(0f, 0.05f), new Vector2(1f, 0.05f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), Vector2.zero,
                delegate ()
                {
                    for (int i = 0; i < 5; i++)
                        _difficultyToggles[i].isOn = _difficultiesStagingValue[i];
                });

            // the container needs some graphical component to have the Transform to RectTransform cast work
            var unused = difficultiesContainer.AddComponent<Image>();
            unused.color = new Color(0f, 0f, 0f, 0f);

            var divider = Utilities.CreateHorizontalDivider(difficultiesContainer.transform, false);
            divider.color = new Color(1f, 1f, 1f, 0.4f);
            divider.rectTransform.sizeDelta = new Vector2(0f, 0.2f);

            // create the difficulty toggles
            var togglePrefab = Utilities.GetTogglePrefab();

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "(Optional) Difficulties To Filter", new Vector2(0f, -1f), new Vector2(50f, 6f));
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.fontSize = 4.5f;
            BeatSaberUI.AddHintText(text.rectTransform, "Check the stats for specific difficulties. Leaving all difficulties unchecked has the same behaviour as checking all difficulties.");

            // easy toggle
            var toggle = CreateToggleFromPrefab(togglePrefab.toggle, difficultiesContainer.transform, Vector2.zero, new Vector2(0.2f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[0] = value;

                if (_hasCompletedStagingValue != SongCompletedFilterOption.Off || _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[0] = toggle;

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "Easy", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.1f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.1f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // normal toggle
            toggle = CreateToggleFromPrefab(togglePrefab.toggle, difficultiesContainer.transform, new Vector2(0.2f, 0f), new Vector2(0.4f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[1] = value;

                if (_hasCompletedStagingValue != SongCompletedFilterOption.Off || _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[1] = toggle;

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "Normal", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.3f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.3f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // hard toggle
            toggle = CreateToggleFromPrefab(togglePrefab.toggle, difficultiesContainer.transform, new Vector2(0.4f, 0f), new Vector2(0.6f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[2] = value;

                if (_hasCompletedStagingValue != SongCompletedFilterOption.Off || _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[2] = toggle;

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "Hard", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // expert toggle
            toggle = CreateToggleFromPrefab(togglePrefab.toggle, difficultiesContainer.transform, new Vector2(0.6f, 0f), new Vector2(0.8f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[3] = value;

                if (_hasCompletedStagingValue != SongCompletedFilterOption.Off || _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[3] = toggle;

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "Expert", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.7f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.7f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            // expert+ toggle
            toggle = CreateToggleFromPrefab(togglePrefab.toggle, difficultiesContainer.transform, new Vector2(0.8f, 0f), new Vector2(1f, 0.5f));
            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _difficultiesStagingValue[4] = value;

                if (_hasCompletedStagingValue != SongCompletedFilterOption.Off || _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                    SettingChanged?.Invoke();
            });
            _difficultyToggles[4] = toggle;

            text = BeatSaberUI.CreateText(difficultiesContainer.transform as RectTransform, "Expert+", new Vector2(0f, -2f), new Vector2(8f, 4f));
            text.rectTransform.anchorMin = new Vector2(0.9f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.9f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Bottom;
            text.fontSize = 3.5f;

            UnityEngine.Object.Destroy(togglePrefab.gameObject);

            _isInitialized = true;
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
                    _hasCompletedViewController.applyImmediately = false;
                    _hasFullComboViewController.applyImmediately = false;
                }

                _hasCompletedViewController.Init();
                _hasFullComboViewController.Init();

                if (!refreshValue)
                {
                    _hasCompletedViewController.applyImmediately = true;
                    _hasFullComboViewController.applyImmediately = true;
                }
            }
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            _hasCompletedStagingValue = SongCompletedFilterOption.Off;
            _hasFullComboStagingValue = SongFullComboFilterOption.Off;

            for (int i = 0; i < 5; ++i)
            {
                _difficultiesStagingValue[i] = false;
                _difficultyToggles[i].isOn = false;
            }

            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            _hasCompletedStagingValue = _hasCompletedAppliedValue;
            _hasFullComboStagingValue = _hasFullComboAppliedValue;

            for (int i = 0; i < 5; ++i)
            {
                _difficultiesStagingValue[i] = _difficultiesAppliedValue[i];
                _difficultyToggles[i].isOn = _difficultiesAppliedValue[i];
            }

            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !ApplyFilter)
                return;

            List<BeatmapDifficulty> diffList = new List<BeatmapDifficulty>(5);
            if (_difficultiesAppliedValue[0])
                diffList.Add(BeatmapDifficulty.Easy);
            if (_difficultiesAppliedValue[1])
                diffList.Add(BeatmapDifficulty.Normal);
            if (_difficultiesAppliedValue[2])
                diffList.Add(BeatmapDifficulty.Hard);
            if (_difficultiesAppliedValue[3])
                diffList.Add(BeatmapDifficulty.Expert);
            if (_difficultiesAppliedValue[4])
                diffList.Add(BeatmapDifficulty.ExpertPlus);

            var levelsToRemove = detailsList.AsParallel().AsOrdered().Where(delegate (BeatmapDetails details)
            {
                bool remove = false;

                // NOTE: if any difficulties are selected, this filter also has the same behaviour as the difficulty filter
                if (diffList.Count > 0)
                {
                    bool hasDifficultiesToCheck = details.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diffList.Contains(diff.Difficulty)));
                    remove |= !hasDifficultiesToCheck;
                }
                if (_hasCompletedAppliedValue != SongCompletedFilterOption.Off && !remove)
                {
                    // if PlayerData object cannot be found, assume level has not been completed
                    bool hasBeenCompleted = PlayerDataHelper.Instance?.HasCompletedLevel(details.LevelID, null, diffList) ?? false;
                    remove |= hasBeenCompleted != (_hasCompletedAppliedValue == SongCompletedFilterOption.HasCompleted);
                }
                if (_hasFullComboAppliedValue != SongFullComboFilterOption.Off && !remove)
                {
                    // if PlayerData object cannot be found, assume level has not been full combo'd
                    bool hasFullCombo = PlayerDataHelper.Instance?.HasFullComboForLevel(details.LevelID, null, diffList) ?? false;
                    remove |= hasFullCombo != (_hasFullComboAppliedValue == SongFullComboFilterOption.HasFullCombo);
                }

                return remove;
            }).ToArray();

            foreach (var level in levelsToRemove)
                detailsList.Remove(level);
        }
    }

    internal enum SongCompletedFilterOption
    {
        Off = 0,
        HasNeverCompleted = 1,
        HasCompleted = 2
    }

    internal enum SongFullComboFilterOption
    {
        Off = 0,
        HasNoFullCombo = 1,
        HasFullCombo = 2
    }
}
