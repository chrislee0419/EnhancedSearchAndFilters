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
    class PPFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return "Ranked Songs (PP)"; } }
        [UIValue("is-available")]
        public bool IsAvailable { get { return Tweaks.SongDataCoreTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
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
        public bool IsFilterApplied => _rankedAppliedValue != RankFilterOption.Off;

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

        [UIValue("rank-value")]
        private RankFilterOption _rankedStagingValue = RankFilterOption.Off;
        [UIValue("min-checkbox-value")]
        private bool _minEnabledStagingValue = false;
        [UIValue("max-checkbox-value")]
        private bool _maxEnabledStagingValue = false;
        [UIValue("min-increment-value")]
        private float _minStagingValue = DefaultMinValue;
        [UIValue("max-increment-value")]
        private float _maxStagingValue = DefaultMaxValue;

        private RankFilterOption _rankedAppliedValue = RankFilterOption.Off;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private bool _isInitialized = false;

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

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.PPFilterViews.bsml"), viewContainer, this);
            _viewGameObject.name = "PPFilterViewContainer";

            _isInitialized = true;
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

            NotifyAllPropertiesChanged();
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

            NotifyAllPropertiesChanged();
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

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("rank-changed")]
        private void OnRankOptionChanged(object value)
        {
            bool isRankedOption = (RankFilterOption)value == RankFilterOption.Ranked;

            _minCheckbox.gameObject.SetActive(isRankedOption);
            _maxCheckbox.gameObject.SetActive(isRankedOption);
            _minIncrement.gameObject.SetActive(isRankedOption && _minEnabledStagingValue);
            _maxIncrement.gameObject.SetActive(isRankedOption && _maxEnabledStagingValue);

            if (_minEnabledStagingValue)
                ValidateMinValue();
            if (_maxEnabledStagingValue)
                ValidateMaxValue();

            SettingChanged?.Invoke();
        }

        [UIAction("min-checkbox-changed")]
        private void OnMinCheckboxChanged(bool value)
        {
            _minIncrement.gameObject.SetActive(_minEnabledStagingValue);

            if (_minEnabledStagingValue)
                ValidateMinValue();

            SettingChanged?.Invoke();
        }

        [UIAction("min-value-changed")]
        private void OnMinValueChanged(int value)
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
                        _minIncrement.maxValue = _maxStagingValue;

                        if (_minStagingValue > _maxStagingValue)
                            _minStagingValue = _maxStagingValue;
                    }
                    else
                    {
                        _minIncrement.maxValue = MaxValue;
                    }

                    // notify min value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_minStagingValue)));
                }
                else
                {
                    _maxIncrement.minValue = MinValue;

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
            _maxIncrement.gameObject.SetActive(_maxEnabledStagingValue);

            if (_maxEnabledStagingValue)
                ValidateMaxValue();

            SettingChanged?.Invoke();
        }

        [UIAction("max-value-changed")]
        private void OnMaxValueChanged(int value)
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
                        _maxIncrement.minValue = _minStagingValue;

                        if (_maxStagingValue < _minStagingValue)
                            _maxStagingValue = _minStagingValue;
                    }
                    else
                    {
                        _maxIncrement.minValue = MinValue;
                    }

                    // notify max value changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_maxStagingValue)));
                }
                else
                {
                    _minIncrement.maxValue = MaxValue;

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

        private void NotifyAllPropertiesChanged()
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_rankedStagingValue)));
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

    internal enum RankFilterOption
    {
        Off,
        Ranked,
        NotRanked
    }
}
