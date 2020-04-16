using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class NoteDensityFilter : FilterBase
    {
        public override string Name => "Note Density";
        public override bool IsFilterApplied => _minEnabledAppliedValue || _maxEnabledAppliedValue;
        public override bool HasChanges => _minEnabledStagingValue != _minEnabledAppliedValue ||
            _minStagingValue != _minAppliedValue ||
            _maxEnabledStagingValue != _maxEnabledAppliedValue ||
            _maxStagingValue != _maxAppliedValue;
        public override bool IsStagingDefaultValues => _minEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxEnabledStagingValue == false &&
            _maxStagingValue == DefaultMaxValue;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.NoteDensityFilterView.bsml";
        protected override string ContainerGameObjectName => "NoteDensityFilterViewContainer";

#pragma warning disable CS0649
        [UIComponent("min-increment-setting")]
        private IncrementSetting _minSetting;
        [UIComponent("max-increment-setting")]
        private IncrementSetting _maxSetting;
#pragma warning restore CS0649

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
        private float _minStagingValue = DefaultMinValue;
        [UIValue("min-increment-value")]
        public float MinStagingValue
        {
            get => _minStagingValue;
            set
            {
                _minStagingValue = value;
                ValidateMinValue();
                InvokeSettingChanged();
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
                InvokeSettingChanged();
            }
        }

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private const float DefaultMinValue = 3f;
        private const float DefaultMaxValue = 5f;
        [UIValue("min-value")]
        private const float MinValue = 0.5f;
        [UIValue("max-value")]
        private const float MaxValue = 25f;
        [UIValue("inc-value")]
        private const float IncrementValue = 0.5f;

        public override void Init(GameObject viewContainer)
        {
            base.Init(viewContainer);

            _minSetting.gameObject.SetActive(_minEnabledStagingValue);
            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);
        }

        public override void SetDefaultValuesToStaging()
        {
            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            if (_viewGameObject != null)
            {
                _minSetting.gameObject.SetActive(false);
                _maxSetting.gameObject.SetActive(false);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void SetAppliedValuesToStaging()
        {
            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            if (_viewGameObject != null)
            {
                _minSetting.gameObject.SetActive(_minEnabledStagingValue);
                _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void ApplyStagingValues()
        {
            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails details = detailsList[i];
                bool remove = details.DifficultyBeatmapSets.Any(delegate (SimplifiedDifficultyBeatmapSet set)
                {
                    return set.DifficultyBeatmaps.Any(delegate (SimplifiedDifficultyBeatmap diff)
                    {
                        float noteDensity = (float)diff.NotesCount / details.SongDuration;
                        return (noteDensity < _minAppliedValue && _minEnabledAppliedValue) || (noteDensity > _maxAppliedValue && _maxEnabledAppliedValue);
                    });
                });

                if (remove)
                    detailsList.RemoveAt(i);
                else
                    ++i;

            }
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "minEnabled", _minEnabledAppliedValue,
                "minValue", _minAppliedValue,
                "maxEnabled", _maxEnabledAppliedValue,
                "maxValue", _maxAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
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
                else if (StringUtilities.TryParseInvariantFloat(pair.Value, out float floatValue))
                {
                    if (pair.Key == "minValue")
                        _minStagingValue = floatValue;
                    else if (pair.Key == "maxValue")
                        _maxStagingValue = floatValue;
                }
            }

            ValidateMinValue();
            ValidateMaxValue();
            RefreshValues();
        }

        private void ValidateMinValue()
        {
            // NOTE: this changes staging values without calling setters
            // (since this is intended to be used by the setters)
            if (_viewGameObject == null)
                return;

            _minSetting.gameObject.SetActive(_minEnabledStagingValue);

            if (_minEnabledStagingValue)
            {
                if (_maxEnabledStagingValue)
                {
                    if (_minStagingValue > _maxStagingValue)
                    {
                        _minStagingValue = _maxStagingValue;
                        _parserParams.EmitEvent(RefreshValuesEvent);
                    }

                    _minSetting.maxValue = _maxStagingValue;
                    _maxSetting.minValue = _minStagingValue;

                    _maxSetting.EnableDec = _maxSetting.Value > _maxSetting.minValue;
                }
                else
                {
                    _minSetting.maxValue = MaxValue;
                }

                _minSetting.EnableInc = _minSetting.Value < _minSetting.maxValue;
            }
            else
            {
                _maxSetting.minValue = MinValue;
                _maxSetting.EnableDec = _maxSetting.Value > _maxSetting.minValue;
            }
        }

        private void ValidateMaxValue()
        {
            // NOTE: this changes staging values without calling setters
            // (since this is intended to be used by the setters)
            if (_viewGameObject == null)
                return;

            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

            if (_maxEnabledStagingValue)
            {
                if (_minEnabledStagingValue)
                {
                    if (_maxStagingValue < _minStagingValue)
                    {
                        _maxStagingValue = _minStagingValue;
                        _parserParams.EmitEvent(RefreshValuesEvent);
                    }

                    _maxSetting.minValue = _minStagingValue;
                    _minSetting.maxValue = _maxStagingValue;

                    _minSetting.EnableInc = _minSetting.Value < _minSetting.maxValue;
                }
                else
                {
                    _maxSetting.minValue = MinValue;
                }

                _maxSetting.EnableDec = _maxSetting.Value > _maxSetting.minValue;
            }
            else
            {
                _minSetting.maxValue = MaxValue;
                _minSetting.EnableInc = _minSetting.Value < _minSetting.maxValue;
            }
        }
    }
}
