﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class DurationFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Song Length"; } }
        public bool IsAvailable { get { return true; } }
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
                else if (_minEnabledStagingValue || _maxEnabledStagingValue)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotApplied;
                }
            }
        }
        public bool IsFilterApplied => _minEnabledAppliedValue || _maxEnabledAppliedValue;
        public bool HasChanges => _minEnabledAppliedValue != _minEnabledStagingValue ||
            _maxEnabledAppliedValue != _maxEnabledStagingValue ||
            _minAppliedValue != _minStagingValue ||
            _maxAppliedValue != _maxStagingValue;
        public bool IsStagingDefaultValues => _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;

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
                _minSetting.gameObject.SetActive(value);

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
                _maxSetting.gameObject.SetActive(value);

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

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private bool _isInitialized = false;
        private BSMLParserParams _parserParams;

        private const float DefaultMinValue = 60f;
        private const float DefaultMaxValue = 120f;
        [UIValue("min-value")]
        private const float MinValue = 0f;
        [UIValue("max-value")]
        private const float MaxValue = 1800f;       // 30 minutes
        [UIValue("inc-value")]
        private const float IncrementValue = 15f;

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            _parserParams = BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.DurationFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "DurationFilterViewContainer";

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

        [UIAction("time-formatter")]
        private string ConvertFloatToTimeString(float duration)
        {
            return TimeSpan.FromSeconds(duration).ToString("m':'ss");
        }

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            _minSetting.gameObject.SetActive(false);
            _maxSetting.gameObject.SetActive(false);

            _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            _minSetting.gameObject.SetActive(_minEnabledStagingValue);
            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

            _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                if ((_minEnabledAppliedValue && detailsList[i].SongDuration < _minAppliedValue) ||
                    (_maxEnabledAppliedValue && detailsList[i].SongDuration > _maxAppliedValue))
                    detailsList.RemoveAt(i);
                else
                    ++i;
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
            if (_maxEnabledStagingValue)
            {
                if (_minEnabledStagingValue)
                {
                    if (_maxStagingValue < _minStagingValue)
                    {
                        _maxStagingValue = _minStagingValue;
                        _parserParams.EmitEvent("refresh-values");
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
