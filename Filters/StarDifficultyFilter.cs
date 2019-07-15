using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using EnhancedSearchAndFilters.UI;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class StarDifficultyFilter : IFilter
    {
        public string FilterName { get { return "Star Rating"; } }
        public bool IsAvailable { get { return Tweaks.SongDataCoreTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (ApplyFilter)
                {
                    if (_minEnabledAppliedValue != _minEnabledStagingValue ||
                        _maxEnabledAppliedValue != _maxEnabledStagingValue ||
                        _minAppliedValue != _minStagingValue ||
                        _maxAppliedValue != _maxStagingValue ||
                        _includeUnratedAppliedValue != _includeUnratedStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_minEnabledStagingValue || _maxEnabledStagingValue)
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
                return _minEnabledAppliedValue || _maxEnabledAppliedValue;
            }
            set
            {
                if (value)
                {
                    _minEnabledAppliedValue = _minEnabledStagingValue;
                    _maxEnabledAppliedValue = _maxEnabledStagingValue;
                    _minAppliedValue = _minStagingValue;
                    _maxAppliedValue = _maxStagingValue;
                    _includeUnratedAppliedValue = _includeUnratedStagingValue;
                }
                else
                {
                    _minEnabledAppliedValue = false;
                    _maxEnabledAppliedValue = false;
                    _minAppliedValue = DefaultMaxValue;
                    _maxAppliedValue = _maxStagingValue;
                    _includeUnratedAppliedValue = false;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[4];

        public event Action SettingChanged;

        private ListViewController _minViewController;
        private ListViewController _maxViewController;
        private BoolViewController _includeUnratedViewController;

        private bool _isInitialized = false;

        private bool _minEnabledStagingValue = false;
        private bool _maxEnabledStagingValue = false;
        private float _minStagingValue = DefaultMinValue;
        private float _maxStagingValue = DefaultMaxValue;
        private bool _includeUnratedStagingValue = false;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;
        private bool _includeUnratedAppliedValue = false;

        private const float DefaultMinValue = 3f;
        private const float DefaultMaxValue = 5f;
        private const float MinValue = 0f;
        private const float MaxValue = 40f;
        private const float IncrementValue = 0.25f;

        public void Init()
        {
            if (_isInitialized)
                return;

            // we're using SongDataCore's ScoreSaber data storage to find out the star rating
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

            var togglePrefab = Utilities.GetTogglePrefab();

            // title text
            var text = BeatSaberUI.CreateText(null, "Keep Songs Between Some Star Rating", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // min view controller
            float[] values = Enumerable.Range((int)MinValue, (int)((MaxValue - MinValue) / IncrementValue) + 1).Select(x => x * IncrementValue).ToArray();
            _minViewController = Utilities.CreateListViewController("Minimum Star Rating", values, "Filters out songs that have a lesser star difficulty rating than this value");
            _minViewController.GetTextForValue += x => x.ToString("0.00");
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
            Utilities.CreateHorizontalDivider(_minViewController.transform);

            Controls[1] = new FilterControl(_minViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f),
                delegate ()
                {
                    // disabling buttons needs to be done after the view controller is enabled to override the interactable assignments of ListSettingsController:OnEnable()
                    _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _minEnabledStagingValue && _minStagingValue > MinValue;
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _minEnabledStagingValue && (!_maxEnabledStagingValue || _minStagingValue < _maxStagingValue) && _minStagingValue < MaxValue;
                });

            // max view controller
            _maxViewController = Utilities.CreateListViewController("Maximum Star Rating", values, "Filters out songs that have a greater star difficulty rating than this value");
            _maxViewController.GetTextForValue += x => x.ToString("0.00");
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
            Utilities.CreateHorizontalDivider(_maxViewController.transform);

            Controls[2] = new FilterControl(_maxViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -20f),
                delegate ()
                {
                    // disabling buttons needs to be done after the view controller is enabled to override the interactable assignments of ListSettingsController:OnEnable()
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _maxEnabledStagingValue && (!_minEnabledStagingValue || _maxStagingValue > _minStagingValue) && _maxStagingValue > MinValue;
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _maxEnabledStagingValue && _maxStagingValue < MaxValue;
                });

            // include unrated songs toggle
            _includeUnratedViewController = Utilities.CreateBoolViewController("Include Unrated Songs", "Do not filter out songs that do not have a star rating provided by ScoreSaber");
            _includeUnratedViewController.GetValue += () => _includeUnratedStagingValue;
            _includeUnratedViewController.SetValue += delegate (bool value)
            {
                _includeUnratedStagingValue = value;

                SettingChanged?.Invoke();
            };
            _includeUnratedViewController.Init();
            _includeUnratedViewController.applyImmediately = true;

            Utilities.MoveIncDecViewControllerElements(_includeUnratedViewController);

            Controls[3] = new FilterControl(_includeUnratedViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -32f));

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
                    _includeUnratedViewController.applyImmediately = false;
                }

                _minViewController.Init();
                _maxViewController.Init();
                _includeUnratedViewController.Init();

                if (!refreshValue)
                {
                    _minViewController.applyImmediately = true;
                    _maxViewController.applyImmediately = true;
                    _includeUnratedViewController.applyImmediately = true;
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

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;
            _includeUnratedStagingValue = false;

            _minViewController.GetComponentInChildren<Toggle>().isOn = false;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = false;
            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized || !Tweaks.SongDataCoreTweaks.ModLoaded)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;
            _includeUnratedStagingValue = _includeUnratedAppliedValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = _minEnabledAppliedValue;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = _maxEnabledAppliedValue;
            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !Tweaks.SongDataCoreTweaks.ModLoaded || (!_minEnabledAppliedValue && !_maxEnabledAppliedValue))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                var tuples = Tweaks.SongDataCoreTweaks.GetStarDifficultyRating(detailsList[i].LevelID);

                if (tuples?.Any(x => (x.Item2 >= _minAppliedValue || !_minEnabledAppliedValue) && (x.Item2 <= _maxAppliedValue || !_maxEnabledAppliedValue)) == true)
                    ++i;
                else if (_includeUnratedAppliedValue && tuples == null)
                    ++i;
                else
                    detailsList.RemoveAt(i);
            }
        }
    }
}
