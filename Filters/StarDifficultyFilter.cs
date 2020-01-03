using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class StarDifficultyFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get { return "Star Rating"; } }
        public bool IsAvailable { get { return Tweaks.SongDataCoreTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
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
        public bool IsFilterApplied => _minEnabledAppliedValue || _maxEnabledAppliedValue;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;

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
        [UIValue("unrated-value")]
        private bool _includeUnratedStagingValue = false;

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;
        private bool _includeUnratedAppliedValue = false;

        private bool _isInitialized = false;

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

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.StarDifficultyFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "StarDifficultyFilterViewContainer";

            _isInitialized = true;
        }

        public GameObject GetView() => _viewGameObject;

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;
            _includeUnratedStagingValue = false;

            NotifyAllPropertiesChanged();
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;
            _includeUnratedStagingValue = _includeUnratedAppliedValue;

            NotifyAllPropertiesChanged();
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _minEnabledAppliedValue = _minEnabledStagingValue;
            _maxEnabledAppliedValue = _maxEnabledStagingValue;
            _minAppliedValue = _minStagingValue;
            _maxAppliedValue = _maxStagingValue;
            _includeUnratedAppliedValue = _includeUnratedStagingValue;

        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized || !IsAvailable)
                return;

            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
            _includeUnratedAppliedValue = false;
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

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("unrated-changed")]
        private void OnUnratedChanged(bool value) => SettingChanged?.Invoke();

        [UIAction("min-checkbox-changed")]
        private void OnMinCheckboxChanged(bool value)
        {
            _minSetting.gameObject.SetActive(_minEnabledStagingValue);

            if (_minEnabledStagingValue)
                ValidateMinValue();

            SettingChanged?.Invoke();
        }

        [UIAction("min-value-changed")]
        private void OnMinValueChanged(float value)
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
        private void OnMaxCheckboxChanged(bool value)
        {
            _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

            if (_maxEnabledStagingValue)
                ValidateMaxValue();

            SettingChanged?.Invoke();
        }

        [UIAction("max-value-changed")]
        private void OnMaxValueChanged(float value)
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
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_includeUnratedStagingValue)));
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
