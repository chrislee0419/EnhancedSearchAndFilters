using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class StarDifficultyFilter : FilterBase
    {
        public override string Name => "Star Rating";
        [UIValue("is-available")]
        public override bool IsAvailable => Tweaks.SongDataCoreTweaks.IsModAvailable;
        public override FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                    return HasChanges ? FilterStatus.AppliedAndChanged : FilterStatus.Applied;
                else if ((_minEnabledStagingValue || _maxEnabledStagingValue) && HasChanges)
                    return FilterStatus.NotAppliedAndChanged;
                else
                    return FilterStatus.NotApplied;
            }
        }
        public override bool IsFilterApplied => _minEnabledAppliedValue || _maxEnabledAppliedValue;
        public override bool HasChanges => _minEnabledAppliedValue != _minEnabledStagingValue ||
            _maxEnabledAppliedValue != _maxEnabledStagingValue ||
            _minAppliedValue != _minStagingValue ||
            _maxAppliedValue != _maxStagingValue ||
            _includeUnratedAppliedValue != _includeUnratedStagingValue;
        public override bool IsStagingDefaultValues => _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue &&
            _includeUnratedStagingValue == false;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.StarDifficultyFilterView.bsml";
        protected override string ContainerGameObjectName => "StarDifficultyFilterViewContainer";

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
        private bool _includeUnratedStagingValue = false;
        [UIValue("unrated-value")]
        public bool IncludeUnratedStagingValue
        {
            get => _includeUnratedStagingValue;
            set
            {
                _includeUnratedStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;
        private bool _includeUnratedAppliedValue = false;

        private const float DefaultMinValue = 3f;
        private const float DefaultMaxValue = 5f;
        [UIValue("min-value")]
        private const float MinValue = 0f;
        [UIValue("max-value")]
        private const float MaxValue = 40f;
        [UIValue("inc-value")]
        private const float IncrementValue = 0.25f;
        [UIValue("missing-requirements-text")]
        private const string MissingRequirementsText = "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the SongDataCore mod to be\n installed.</size></color>";

        public override void Init(GameObject viewContainer)
        {
            base.Init(viewContainer);

            if (!IsAvailable)
                return;

            // ensure that the UI correctly reflects the staging values
            _minSetting.gameObject.SetActive(_minEnabledStagingValue);
            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);
        }

        public override void SetDefaultValuesToStaging()
        {
            if (!IsAvailable)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;
            _includeUnratedStagingValue = false;

            if (_viewGameObject != null)
            {
                _minSetting.gameObject.SetActive(false);
                _maxSetting.gameObject.SetActive(false);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void SetAppliedValuesToStaging()
        {
            if (!IsAvailable)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;
            _includeUnratedStagingValue = _includeUnratedAppliedValue;

            if (_viewGameObject != null)
            {
                _minSetting.gameObject.SetActive(_minEnabledStagingValue);
                _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

                _parserParams.EmitEvent(RefreshValuesEvent);
            }
        }

        public override void ApplyStagingValues()
        {
            if (!IsAvailable)
                return;

            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
            _includeUnratedAppliedValue = _includeUnratedStagingValue;

        }

        public override void ApplyDefaultValues()
        {
            if (!IsAvailable)
                return;

            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
            _includeUnratedAppliedValue = false;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!Tweaks.SongDataCoreTweaks.IsModAvailable || (!_minEnabledAppliedValue && !_maxEnabledAppliedValue))
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

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "minEnabled", _minEnabledAppliedValue,
                "minValue", _minAppliedValue,
                "maxEnabled", _maxEnabledAppliedValue,
                "maxValue", _maxAppliedValue,
                "includeUnrated", _includeUnratedAppliedValue);
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
                    switch (pair.Key)
                    {
                        case "minEnabled":
                            _minEnabledStagingValue = boolValue;
                            break;
                        case "maxEnabled":
                            _maxEnabledStagingValue = boolValue;
                            break;
                        case "includeUnrated":
                            _includeUnratedStagingValue = boolValue;
                            break;
                    }
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
