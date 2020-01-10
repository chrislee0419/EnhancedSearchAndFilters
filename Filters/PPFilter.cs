using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class PPFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Ranked Songs (PP)"; } }
        [UIValue("is-available")]
        public bool IsAvailable { get { return Tweaks.SongDataCoreTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                {
                    if (HasChanges)
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
                    return FilterStatus.NotApplied;
                }
            }
        }
        public bool IsFilterApplied => _rankedAppliedValue != RankFilterOption.Off;
        public bool HasChanges => _rankedAppliedValue != _rankedStagingValue ||
            _minEnabledAppliedValue != _minEnabledStagingValue ||
            _maxEnabledAppliedValue != _maxEnabledStagingValue ||
            _minAppliedValue != _minStagingValue ||
            _maxAppliedValue != _maxStagingValue;
        public bool IsStagingDefaultValues => _rankedStagingValue == RankFilterOption.Off &&
            _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;

        [UIComponent("min-checkbox")]
        private CheckboxSetting _minCheckbox;
        [UIComponent("min-increment")]
        private IncrementSetting _minIncrement;
        [UIComponent("max-checkbox")]
        private CheckboxSetting _maxCheckbox;
        [UIComponent("max-increment")]
        private IncrementSetting _maxIncrement;
#pragma warning restore CS0649

        private RankFilterOption _rankedStagingValue = RankFilterOption.Off;
        [UIValue("rank-value")]
        public RankFilterOption RankedStagingValue
        {
            get => _rankedStagingValue;
            set
            {
                _rankedStagingValue = value;

                bool isRankedOption = (RankFilterOption)value == RankFilterOption.Ranked;

                _minCheckbox.gameObject.SetActive(isRankedOption);
                _maxCheckbox.gameObject.SetActive(isRankedOption);
                _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
                _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);

                SettingChanged?.Invoke();
            }
        }
        private bool _minEnabledStagingValue = false;
        [UIValue("min-checkbox-value")]
        public bool MinEnabledStagingValue
        {
            get => _minEnabledStagingValue;
            set
            {
                _minEnabledStagingValue = value;
                _minIncrement.gameObject.SetActive(_minEnabledStagingValue);

                ValidateMinValue();

                SettingChanged?.Invoke();
            }
        }
        private bool _maxEnabledStagingValue = false;
        [UIValue("max-checkbox-value")]
        public bool MaxEnabledStagingValue
        {
            get => _maxEnabledStagingValue;
            set
            {
                _maxEnabledStagingValue = value;
                _maxIncrement.gameObject.SetActive(_maxEnabledStagingValue);

                ValidateMaxValue();

                SettingChanged?.Invoke();
            }
        }
        private float _minStagingValue = DefaultMinValue;
        [UIValue("min-increment-value")]
        public float MinStagingValue
        {
            get => _minStagingValue;
            set
            {
                _minStagingValue = value;
                ValidateMinValue();
                SettingChanged?.Invoke();
            }
        }
        private float _maxStagingValue = DefaultMaxValue;
        [UIValue("max-increment-value")]
        public float MaxStagingValue
        {
            get => _maxStagingValue;
            set
            {
                _maxStagingValue = value;
                ValidateMaxValue();
                SettingChanged?.Invoke();
            }
        }

        private RankFilterOption _rankedAppliedValue = RankFilterOption.Off;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private bool _isInitialized = false;
        private BSMLParserParams _parserParams;

        private const float DefaultMinValue = 200f;
        private const float DefaultMaxValue = 300f;
        [UIValue("min-value")]
        private const float MinValue = 0f;
        [UIValue("max-value")]
        private const float MaxValue = 500f;
        [UIValue("inc-value")]
        private const float IncrementValue = 25f;
        [UIValue("missing-requirements-text")]
        private const string MissingRequirementsMessage = "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the SongDataCore mod to be\n installed.</size></color>";
        [UIValue("rank-options")]
        private static readonly List<object> RankFilterOptions = Enum.GetValues(typeof(RankFilterOption)).Cast<RankFilterOption>().Select(x => (object)x).ToList();

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            _parserParams = BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.PPFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "PPFilterViewContainer";

            _minCheckbox.gameObject.SetActive(false);
            _minIncrement.gameObject.SetActive(false);
            _maxCheckbox.gameObject.SetActive(false);
            _maxIncrement.gameObject.SetActive(false);

            _isInitialized = true;
        }

        public void Cleanup()
        {
            if (_viewGameObject != null)
            {
                UnityEngine.Object.Destroy(_viewGameObject);
                _viewGameObject = null;
            }
        }

        public GameObject GetView() => _viewGameObject;

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _rankedStagingValue = RankFilterOption.Off;
            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            _minCheckbox.gameObject.SetActive(false);
            _minIncrement.gameObject.SetActive(false);
            _maxCheckbox.gameObject.SetActive(false);
            _maxIncrement.gameObject.SetActive(false);

            _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _rankedStagingValue = _rankedAppliedValue;
            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            bool isRankedOption = _rankedStagingValue == RankFilterOption.Ranked;
            _minCheckbox.gameObject.SetActive(isRankedOption);
            _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
            _maxCheckbox.gameObject.SetActive(isRankedOption);
            _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);

            _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _rankedAppliedValue = _rankedStagingValue;
            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _rankedAppliedValue = RankFilterOption.Off;
            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsAvailable || !IsFilterApplied)
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

        public string SerializeFromAppliedValues()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        private void ValidateMinValue()
        {
            // NOTE: this changes staging values without calling setters
            // (since this is intended to be used by the setters)
            if (_minEnabledStagingValue)
            {
                if (_maxEnabledStagingValue)
                {
                    if (_minStagingValue > _maxStagingValue)
                    {
                        _minStagingValue = _maxStagingValue;
                        _parserParams.EmitEvent("refresh-values");
                    }

                    _minIncrement.maxValue = _maxStagingValue;
                    _maxIncrement.minValue = _minStagingValue;

                    _maxIncrement.EnableDec = _maxIncrement.Value > _maxIncrement.minValue;
                }
                else
                {
                    _minIncrement.maxValue = MaxValue;
                }

                _minIncrement.EnableInc = _minIncrement.Value < _minIncrement.maxValue;
            }
            else
            {
                _maxIncrement.minValue = MinValue;
                _maxIncrement.EnableDec = _maxIncrement.Value > _maxIncrement.minValue;
            }
        }

        private void ValidateMaxValue()
        {
            // NOTE: this changes staging values without calling setters
            // (since this is intended to be used by the setters)
            if (_maxEnabledStagingValue)
            {
                if (_minEnabledStagingValue)
                {
                    if (_maxStagingValue < _minStagingValue)
                    {
                        _maxStagingValue = _minStagingValue;
                        _parserParams.EmitEvent("refresh-values");
                    }

                    _maxIncrement.minValue = _minStagingValue;
                    _minIncrement.maxValue = _maxStagingValue;

                    _minIncrement.EnableInc = _minIncrement.Value < _minIncrement.maxValue;
                }
                else
                {
                    _maxIncrement.minValue = MinValue;
                }

                _maxIncrement.EnableDec = _maxIncrement.Value > _maxIncrement.minValue;
            }
            else
            {
                _minIncrement.maxValue = MaxValue;
                _minIncrement.EnableInc = _minIncrement.Value < _minIncrement.maxValue;
            }
        }

        [UIAction("rank-formatter")]
        private string RankFormatter(object value)
        {
            switch ((RankFilterOption)value)
            {
                case RankFilterOption.Off:
                    return "Off";
                case RankFilterOption.NotRanked:
                    return "Not Ranked";
                case RankFilterOption.Ranked:
                    return "Ranked";
                default:
                    return "ERROR!";
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
