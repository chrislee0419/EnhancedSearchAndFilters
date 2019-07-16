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
using EnhancedSearchAndFilters.SongData;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class PPFilter : IFilter
    {
        public string FilterName { get { return "PP"; } }
        public bool IsAvailable { get { return Tweaks.SongDataCoreTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (ApplyFilter)
                {
                    if (_rankedAppliedValue != _rankedStagingValue ||
                        _minEnabledAppliedValue != _minEnabledStagingValue ||
                        _maxEnabledAppliedValue != _maxEnabledStagingValue ||
                        _minAppliedValue != _minStagingValue ||
                        _maxAppliedValue != _maxStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_rankedStagingValue != RankFilterOption.Off)
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
                return _rankedAppliedValue != RankFilterOption.Off;
            }
            set
            {
                if (value)
                {
                    _rankedAppliedValue = _rankedStagingValue;
                    _minEnabledAppliedValue = _minEnabledStagingValue;
                    _maxEnabledAppliedValue = _maxEnabledStagingValue;
                    _minAppliedValue = _minStagingValue;
                    _maxAppliedValue = _maxStagingValue;
                }
                else
                {
                    _rankedAppliedValue = RankFilterOption.Off;
                    _minEnabledAppliedValue = false;
                    _maxEnabledAppliedValue = false;
                    _minAppliedValue = DefaultMaxValue;
                    _maxAppliedValue = _maxStagingValue;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[3];

        public event Action SettingChanged;

        private ListViewController _rankedViewController;
        private ListViewController _minViewController;
        private ListViewController _maxViewController;

        private GameObject _container;

        private bool _isInitialized = false;

        private RankFilterOption _rankedStagingValue = RankFilterOption.Off;
        private bool _minEnabledStagingValue = false;
        private bool _maxEnabledStagingValue = false;
        private float _minStagingValue = DefaultMinValue;
        private float _maxStagingValue = DefaultMaxValue;
        private RankFilterOption _rankedAppliedValue = RankFilterOption.Off;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private const float DefaultMinValue = 200f;
        private const float DefaultMaxValue = 300f;
        private const float MinValue = 0f;
        private const float MaxValue = 500f;
        private const float IncrementValue = 25f;

        public void Init()
        {
            if (_isInitialized)
                return;

            if (!Tweaks.SongDataCoreTweaks.ModLoaded)
            {
                Controls = new FilterControl[1];

                var noModMessage = BeatSaberUI.CreateText(null, "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the SongDataCore mod to be\n installed.</size></color>", Vector2.zero);
                noModMessage.alignment = TextAlignmentOptions.Center;
                noModMessage.fontSize = 5.5f;

                Controls[0] = new FilterControl(noModMessage.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 50f), new Vector2(0f, 10f));

                _isInitialized = true;
                return;
            }

            // title text
            var text = BeatSaberUI.CreateText(null, "Keep Songs That Award PP", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // ranked view controller
            float[] rankedValues = Enumerable.Range(0, Enum.GetValues(typeof(RankFilterOption)).Length).Select(x => (float)x).ToArray();
            _rankedViewController = Utilities.CreateListViewController("Is Ranked", rankedValues, "Filters out songs depending on whether they are ranked");
            _rankedViewController.GetTextForValue += delegate (float value)
            {
                RankFilterOption option = (RankFilterOption)value;
                if (option == RankFilterOption.Ranked)
                    return "Ranked";
                else if (option == RankFilterOption.NotRanked)
                    return "<size=80%>Not Ranked</size>";
                else
                    return "Off";
            };
            _rankedViewController.GetValue += () => (float)_rankedStagingValue;
            _rankedViewController.SetValue += delegate (float value)
            {
                _rankedStagingValue = (RankFilterOption)value;

                if (_rankedStagingValue == RankFilterOption.Ranked)
                {
                    _container.SetActive(true);
                    _minEnabledStagingValue = _minEnabledAppliedValue;
                    _maxEnabledStagingValue = _maxEnabledAppliedValue;
                }
                else
                {
                    _container.SetActive(false);
                    _minEnabledStagingValue = false;
                    _maxEnabledStagingValue = false;
                }

                SettingChanged?.Invoke();
            };
            _rankedViewController.Init();
            _rankedViewController.applyImmediately = true;

            Utilities.MoveListViewControllerElements(_rankedViewController);

            Controls[1] = new FilterControl(_rankedViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f));

            // min-max view controller container
            _container = new GameObject("PPFilterContainer");
            var image = _container.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.material = UIUtilities.NoGlowMaterial;

            Controls[2] = new FilterControl(_container, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(0f, -20f),
                delegate ()
                {
                    _container.SetActive(_rankedStagingValue != RankFilterOption.Off);
                });

            var togglePrefab = Utilities.GetTogglePrefab();

            // min view controller
            float[] values = Enumerable.Range((int)MinValue, (int)((MaxValue - MinValue) / IncrementValue) + 1).Select(x => x * IncrementValue).ToArray();
            _minViewController = Utilities.CreateListViewController("Minimum PP", values, "Filters out ranked songs that award less PP that this value");
            _minViewController.GetTextForValue += x => x.ToString();
            _minViewController.GetValue += () => _minStagingValue;
            _minViewController.SetValue += delegate (float value)
            {
                if (_maxEnabledStagingValue && value > _maxStagingValue)
                {
                    _minStagingValue = _maxStagingValue;
                    RefreshUI();
                    return;
                }

                _minStagingValue = value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _minViewController.Init();
            _minViewController.applyImmediately = true;

            rt = _minViewController.transform as RectTransform;
            rt.parent = _container.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 12f);

            var minToggle = Utilities.CreateToggleFromPrefab(togglePrefab.toggle, _minViewController.transform.Find("Value"));
            minToggle.name = "MinValueToggle";
            minToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _minEnabledStagingValue = value;

                if (value && _maxEnabledStagingValue && _minStagingValue > _maxStagingValue)
                    _minStagingValue = _maxStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            Utilities.MoveListViewControllerElements(_minViewController);
            var divider = Utilities.CreateHorizontalDivider(_minViewController.transform, false);
            divider.color = new Color(1f, 1f, 1f, 0.4f);
            divider.rectTransform.sizeDelta = new Vector2(0f, 0.2f);
            Utilities.CreateHorizontalDivider(_minViewController.transform);

            // max view controller
            _maxViewController = Utilities.CreateListViewController("Minimum PP", values, "Filters out ranked songs that award less PP that this value");
            _maxViewController.GetTextForValue += x => x.ToString();
            _maxViewController.GetValue += () => _maxStagingValue;
            _maxViewController.SetValue += delegate (float value)
            {
                if (_minEnabledStagingValue && value < _minStagingValue)
                {
                    _maxStagingValue = _minStagingValue;
                    RefreshUI();
                    return;
                }

                _maxStagingValue = value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _maxViewController.Init();
            _maxViewController.applyImmediately = true;

            rt = _maxViewController.transform as RectTransform;
            rt.parent = _container.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(0f, 12f);

            var maxToggle = Utilities.CreateToggleFromPrefab(togglePrefab.toggle, _maxViewController.transform.Find("Value"));
            maxToggle.name = "MaxValueToggle";
            maxToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _maxEnabledStagingValue = value;

                if (value && _minEnabledStagingValue && _maxStagingValue < _minStagingValue)
                    _maxStagingValue = _minStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            Utilities.MoveListViewControllerElements(_maxViewController);

            Object.Destroy(togglePrefab);

            _isInitialized = true;
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

        public void SetDefaultValues()
        {
            if (!_isInitialized || !Tweaks.SongDataCoreTweaks.ModLoaded)
                return;

            _rankedStagingValue = RankFilterOption.Off;
            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = false;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = false;
            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized || !Tweaks.SongDataCoreTweaks.ModLoaded)
                return;

            _rankedStagingValue = _rankedAppliedValue;
            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = _minEnabledAppliedValue;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = _maxEnabledAppliedValue;
            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !Tweaks.SongDataCoreTweaks.ModLoaded || _rankedAppliedValue == RankFilterOption.Off)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                if (_rankedAppliedValue == RankFilterOption.Ranked)
                {
                    if (Tweaks.SongDataCoreTweaks.IsRanked(detailsList[i].LevelID, out var ppList))
                    {
                        // filter by min/max values
                        if (ppList.Any(x => (!_minEnabledAppliedValue || x >= _minAppliedValue) && (!_maxEnabledAppliedValue || x <= _maxAppliedValue)))
                            ++i;
                        else
                            detailsList.RemoveAt(i);
                    }
                    else
                    {
                        // not ranked, remove
                        detailsList.RemoveAt(i);
                    }
                }
                else if (Tweaks.SongDataCoreTweaks.IsRanked(detailsList[i].LevelID, out _))
                {
                    // ranked, remove
                    detailsList.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }
    }

    internal enum RankFilterOption
    {
        Off,
        Ranked,
        NotRanked
    }
}
