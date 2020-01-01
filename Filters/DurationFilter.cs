using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class DurationFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return "Song Length"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                {
                    if (_minEnabledAppliedValue != _minEnabledStagingValue ||
                        _maxEnabledAppliedValue != _maxEnabledStagingValue ||
                        _minAppliedValue != _minStagingValue ||
                        _maxAppliedValue != _maxStagingValue)
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
        public bool IsFilterApplied => _minEnabledAppliedValue || _maxEnabledAppliedValue;

        [UIObject("root")]
        public GameObject ViewGameObject { get; private set; }

#pragma warning disable CS0649
        [UIComponent("min-increment-setting")]
        private IncrementSetting _minSetting;
        [UIComponent("max-increment-setting")]
        private IncrementSetting _maxSetting;
#pragma warning restore CS0649

        [UIValue("min-checkbox-value")]
        private bool _minEnabledStagingValue = false;
        [UIValue("max-checkbox-value")]
        private bool _maxEnabledStagingValue = false;
        [UIValue("min-increment-value")]
        private float _minStagingValue = DefaultMinValue;
        [UIValue("max-increment-value")]
        private float _maxStagingValue = DefaultMaxValue;

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private bool _isInitialized = false;

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

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.DurationFilterView.bsml"), viewContainer, this);
            ViewGameObject.name = "DurationFilterViewContainer";

            _isInitialized = true;
        }

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

            NotifyAllPropertiesChanged();
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            NotifyAllPropertiesChanged();
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

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("min-checkbox-changed")]
        private void OnMinCheckboxChanged()
        {
            _minSetting.gameObject.SetActive(_minEnabledStagingValue);

            if (_minEnabledStagingValue)
                ValidateMinValue();

            SettingChanged?.Invoke();
        }

        [UIAction("min-value-changed")]
        private void OnMinValueChanged()
        {
            ValidateMinValue();

            SettingChanged?.Invoke();
        }

        private void ValidateMinValue()
        {
            try
            {
                if (_minEnabledStagingValue)
                {
                    if (_maxEnabledStagingValue)
                    {
                        _minSetting.maxValue = _maxStagingValue;

                        if (_minStagingValue > _maxStagingValue)
                            _minStagingValue = _maxStagingValue;
                    }
                    else
                    {
                        _minSetting.maxValue = MaxValue;
                    }

                    // notify min value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_minStagingValue)));
                }
                else
                {
                    _maxSetting.minValue = MinValue;

                    // notify max value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_maxStagingValue)));
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error occurred while validating min duration value: {ex.Message}");
                Logger.log?.Error(ex);
            }
        }

        [UIAction("max-checkbox-changed")]
        private void OnMaxCheckboxChanged()
        {
            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

            if (_maxEnabledStagingValue)
                ValidateMaxValue();

            SettingChanged?.Invoke();
        }

        [UIAction("max-value-changed")]
        private void OnMaxValueChanged()
        {
            ValidateMaxValue();

            SettingChanged?.Invoke();
        }

        private void ValidateMaxValue()
        {
            try
            {
                if (_maxEnabledStagingValue)
                {
                    if (_minEnabledStagingValue)
                    {
                        _maxSetting.minValue = _minStagingValue;

                        if (_maxStagingValue < _minStagingValue)
                            _maxStagingValue = _minStagingValue;
                    }
                    else
                    {
                        _maxSetting.minValue = MinValue;
                    }

                    // notify max value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_maxStagingValue)));
                }
                else
                {
                    _minSetting.maxValue = MaxValue;

                    // notify min value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_maxStagingValue)));
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error occurred while validating max duration value: {ex.Message}");
                Logger.log.Error(ex);
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_minEnabledStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_maxEnabledStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_minStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_maxStagingValue)));
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error Invoking PropertyChanged: {ex.Message}");
                Logger.log.Error(ex);
            }
        }
    }
}
