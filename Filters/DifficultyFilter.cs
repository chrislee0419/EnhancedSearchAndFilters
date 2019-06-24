using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.UI;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class DifficultyFilter : IFilter
    {
        public string FilterName { get { return "Difficulty"; } }
        public FilterStatus Status {
            get
            {
                bool anyApplied = _appliedValues.Contains(true);

                for (int i = 0; i < 5; ++i)
                {
                    if (_stagingValues[i] != _appliedValues[i])
                        return anyApplied ? FilterStatus.AppliedAndChanged : FilterStatus.NotAppliedAndChanged;
                }

                return anyApplied ? FilterStatus.Applied : FilterStatus.NotAppliedAndDefault;
            }
        }
        public bool ApplyFilter
        {
            get
            {
                return _appliedValues.Contains(true);
            }
            set
            {
                if (value)
                {
                    for (int i = 0; i < 5; ++i)
                        _appliedValues[i] = _stagingValues[i];
                }
                else
                {
                    for (int i = 0; i < 5; ++i)
                        _appliedValues[i] = false;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[1];

        public event Action SettingChanged;

        private bool[] _stagingValues = new bool[5];
        private bool[] _appliedValues = new bool[5];
        private Toggle[] _toggles = new Toggle[5];

        private bool _isInitialized = false;

        private static readonly string[] DifficultyStrings = new string[] { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };

        public void Init()
        {
            if (_isInitialized)
                return;

            var container = new GameObject("DifficultiesFilterContainer");
            Controls[0] = new FilterControl(container, new Vector2(0f, 0.05f), new Vector2(1f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                delegate ()
                {
                    for (int i = 0; i < 5; ++i)
                        _toggles[i].isOn = _stagingValues[i];
                });

            var unused = container.AddComponent<Image>();
            unused.color = new Color(0f, 0f, 0f, 0f);

            var text = BeatSaberUI.CreateText(container.transform as RectTransform, "Keep Songs That Have These Difficulties", Vector2.zero, new Vector2(60f, 6f));
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var togglePrefab = Utilities.GetTogglePrefab();

            for (int i = 0; i < 4; ++i)
                CreateToggleControl(container.transform as RectTransform, DifficultyStrings[i], i, togglePrefab.toggle);
            CreateToggleControl(container.transform as RectTransform, "Expert+", 4, togglePrefab.toggle, false);

            Object.Destroy(togglePrefab.gameObject);

            _isInitialized = true;
        }

        private void CreateToggleControl(RectTransform parent, string label, int index, Toggle prefab, bool createDivider=true)
        {
            // difficulty toggle elements are 90 wide, 10 tall
            var text = BeatSaberUI.CreateText(parent, label, new Vector2(4f, -9.5f - (10f * index)), new Vector2(30f, 10f));
            //text.alignment = TextAlignmentOptions.Left;       // this doesn't work for whatever reason
            text.fontSize = 5f;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var toggle = Utilities.CreateToggleFromPrefab(prefab, parent);
            rt = toggle.transform as RectTransform;
            rt.anchorMin = Vector2.one;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(6f, 6f);
            rt.anchoredPosition = new Vector2(-6f, -13f - (10f * index));

            toggle.onValueChanged.AddListener(delegate (bool value)
            {
                _stagingValues[index] = value;

                SettingChanged?.Invoke();
            });
            _toggles[index] = toggle;

            if (createDivider)
            {
                var divider = Utilities.CreateHorizontalDivider(parent, 0f, false);
                divider.rectTransform.anchoredPosition = new Vector2(0f, -18f - (10f * index));
            }
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            for (int i = 0; i < 5; ++i)
            {
                _stagingValues[i] = false;
                _toggles[i].isOn = false;
            }

        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            for (int i = 0; i < 5; ++i)
            {
                _stagingValues[i] = _appliedValues[i];
                _toggles[i].isOn = _stagingValues[i];
            }
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !_appliedValues.Contains(true))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                bool remove = true;
                foreach (var difficultySet in detailsList[i].DifficultyBeatmapSets)
                {
                    var difficulties = difficultySet.DifficultyBeatmaps.Select(x => x.Difficulty.ToString()).ToArray();

                    for (int j = 0; j < 5; ++j)
                    {
                        if (!_appliedValues[j])
                            continue;
                        else if (difficulties.Contains(DifficultyStrings[j]))
                        {
                            var index = Array.IndexOf(difficulties, DifficultyStrings[j]);

                            // don't consider lightshow difficulties as valid difficulties
                            if (difficultySet.DifficultyBeatmaps[index].NotesCount > 0)
                            {
                                remove = false;
                                break;
                            }
                        }
                    }
                    if (!remove)
                        break;
                }

                if (remove)
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }
    }
}
