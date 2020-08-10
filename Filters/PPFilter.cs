using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class PPFilter : FilterBase
    {
        public override string Name => "Ranked Songs (PP)";
        [UIValue("is-available")]
        public override bool IsAvailable => Tweaks.SongDataCoreTweaks.IsModAvailable;
        public override FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                    return HasChanges ? FilterStatus.AppliedAndChanged : FilterStatus.Applied;
                else if (_rankedStagingValue != RankFilterOption.Off && HasChanges)
                    return FilterStatus.NotAppliedAndChanged;
                else
                    return FilterStatus.NotApplied;
            }
        }
        public override bool IsFilterApplied => _rankedAppliedValue != RankFilterOption.Off;
        public override bool HasChanges => _rankedAppliedValue != _rankedStagingValue ||
            _minEnabledAppliedValue != _minEnabledStagingValue ||
            _maxEnabledAppliedValue != _maxEnabledStagingValue ||
            _minAppliedValue != _minStagingValue ||
            _maxAppliedValue != _maxStagingValue;
        public override bool IsStagingDefaultValues => _rankedStagingValue == RankFilterOption.Off &&
            _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.PPFilterView.bsml";
        protected override string ContainerGameObjectName => "PPFilterViewContainer";

#pragma warning disable CS0649
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

                if (_viewGameObject != null)
                {
                    bool isRankedOption = value == RankFilterOption.Ranked;

                    _minCheckbox.gameObject.SetActive(isRankedOption);
                    _maxCheckbox.gameObject.SetActive(isRankedOption);
                    _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
                    _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);
                }

                InvokeSettingChanged();
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
                ValidateMinValue();
                InvokeSettingChanged();
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
                ValidateMaxValue();
                InvokeSettingChanged();
            }
        }
        private int _minStagingValue = DefaultMinValue;
        [UIValue("min-increment-value")]
        public int MinStagingValue
        {
            get => _minStagingValue;
            set
            {
                _minStagingValue = value;
                ValidateMinValue();
                InvokeSettingChanged();
            }
        }
        private int _maxStagingValue = DefaultMaxValue;
        [UIValue("max-increment-value")]
        public int MaxStagingValue
        {
            get => _maxStagingValue;
            set
            {
                _maxStagingValue = value;
                ValidateMaxValue();
                InvokeSettingChanged();
            }
        }

        private RankFilterOption _rankedAppliedValue = RankFilterOption.Off;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private int _minAppliedValue = DefaultMinValue;
        private int _maxAppliedValue = DefaultMaxValue;

        private const int DefaultMinValue = 200;
        private const int DefaultMaxValue = 300;
        [UIValue("min-value")]
        private const int MinValue = 0;
        [UIValue("max-value")]
        private const int MaxValue = 500;
        [UIValue("inc-value")]
        private const int IncrementValue = 25;
        [UIValue("missing-requirements-text")]
        private const string MissingRequirementsMessage = "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the SongDataCore mod to be\n installed.</size></color>";
        [UIValue("rank-options")]
        private static readonly List<object> RankFilterOptions = Enum.GetValues(typeof(RankFilterOption)).Cast<RankFilterOption>().Select(x => (object)x).ToList();

        public override void Init(GameObject viewContainer)
        {
            base.Init(viewContainer);

            if (!IsAvailable)
                return;

            // ensure that the UI correctly reflects the staging values
            bool isRankedOption = _rankedStagingValue == RankFilterOption.Ranked;
            _minCheckbox.gameObject.SetActive(isRankedOption);
            _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
            _maxCheckbox.gameObject.SetActive(isRankedOption);
            _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);
        }

        public override void SetDefaultValuesToStaging()
        {
            if (!IsAvailable)
                return;

            _rankedStagingValue = RankFilterOption.Off;
            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            if (_viewGameObject != null)
            {
                _minCheckbox.gameObject.SetActive(false);
                _minIncrement.gameObject.SetActive(false);
                _maxCheckbox.gameObject.SetActive(false);
                _maxIncrement.gameObject.SetActive(false);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void SetAppliedValuesToStaging()
        {
            if (!IsAvailable)
                return;

            _rankedStagingValue = _rankedAppliedValue;
            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            if (_viewGameObject != null)
            {
                bool isRankedOption = _rankedStagingValue == RankFilterOption.Ranked;
                _minCheckbox.gameObject.SetActive(isRankedOption);
                _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
                _maxCheckbox.gameObject.SetActive(isRankedOption);
                _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void ApplyStagingValues()
        {
            if (!IsAvailable)
                return;

            _rankedAppliedValue = _rankedStagingValue;
            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            if (!IsAvailable)
                return;

            _rankedAppliedValue = RankFilterOption.Off;
            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsAvailable || !IsFilterApplied)
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

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "rank", _rankedAppliedValue,
                "minEnabled", _minEnabledAppliedValue,
                "minValue", _minAppliedValue,
                "maxEnabled", _maxEnabledAppliedValue,
                "maxValue", _maxAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            if (!IsAvailable)
                return;

            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (bool.TryParse(pair.Value, out bool boolValue))
                {
                    if (pair.Key == "minEnabled")
                        _minEnabledStagingValue = boolValue;
                    else if (pair.Key == "maxEnabled")
                        _maxEnabledStagingValue = boolValue;
                }
                else if (int.TryParse(pair.Value, out int intValue))
                {
                    if (pair.Key == "minValue")
                        _minStagingValue = intValue;
                    else if (pair.Key == "maxValue")
                        _maxStagingValue = intValue;
                }
                else if (pair.Key == "rank" && Enum.TryParse(pair.Value, out RankFilterOption rankValue))
                {
                    _rankedStagingValue = rankValue;
                }
            }

            ValidateMinValue();
            ValidateMaxValue();
            if (_viewGameObject != null)
            {
                _minCheckbox.gameObject.SetActive(_rankedStagingValue == RankFilterOption.Ranked);
                _maxCheckbox.gameObject.SetActive(_rankedStagingValue == RankFilterOption.Ranked);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        private void ValidateMinValue()
        {
            // NOTE: this changes staging values without calling setters
            // (since this is intended to be used by the setters)
            if (_viewGameObject == null)
                return;

            _minIncrement.gameObject.SetActive(_minEnabledStagingValue && _rankedStagingValue == RankFilterOption.Ranked);

            if (_minEnabledStagingValue)
            {
                if (_maxEnabledStagingValue)
                {
                    if (_minStagingValue > _maxStagingValue)
                    {
                        _minStagingValue = _maxStagingValue;
                        _parserParams.EmitEvent(RefreshValuesEvent);
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
            if (_viewGameObject == null)
                return;

            _maxIncrement.gameObject.SetActive(_maxEnabledStagingValue && _rankedStagingValue == RankFilterOption.Ranked);

            if (_maxEnabledStagingValue)
            {
                if (_minEnabledStagingValue)
                {
                    if (_maxStagingValue < _minStagingValue)
                    {
                        _maxStagingValue = _minStagingValue;
                        _parserParams.EmitEvent(RefreshValuesEvent);
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
