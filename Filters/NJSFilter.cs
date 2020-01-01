using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class NJSFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return "Note Jump Speed (NJS)"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status {
            get
            {
                if (IsFilterApplied)
                {
                    if (_minEnabledStagingValue != _minEnabledAppliedValue ||
                        _maxEnabledStagingValue != _maxEnabledAppliedValue ||
                        _minStagingValue != _minAppliedValue ||
                        _maxStagingValue != _maxAppliedValue ||
                        _easyStagingValue != _easyAppliedValue ||
                        _normalStagingValue != _normalAppliedValue ||
                        _hardStagingValue != _hardAppliedValue ||
                        _expertStagingValue != _expertAppliedValue ||
                        _expertPlusStagingValue != _expertPlusAppliedValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if ((_minEnabledStagingValue || _maxEnabledStagingValue) &&
                    (_easyStagingValue || _normalStagingValue || _hardStagingValue || _expertStagingValue || _expertPlusStagingValue))
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool IsFilterApplied => (_minEnabledAppliedValue || _maxEnabledAppliedValue) &&
            (_easyAppliedValue || _normalAppliedValue || _hardAppliedValue || _expertAppliedValue || _expertPlusAppliedValue);

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
        private int _minStagingValue = DefaultMinValue;
        [UIValue("max-increment-value")]
        private int _maxStagingValue = DefaultMaxValue;
        [UIValue("easy-value")]
        private bool _easyStagingValue = false;
        [UIValue("normal-value")]
        private bool _normalStagingValue = false;
        [UIValue("hard-value")]
        private bool _hardStagingValue = false;
        [UIValue("expert-value")]
        private bool _expertStagingValue = false;
        [UIValue("expert-plus-value")]
        private bool _expertPlusStagingValue = false;

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private int _minAppliedValue = DefaultMinValue;
        private int _maxAppliedValue = DefaultMaxValue;
        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        private bool _isInitialized = false;

        private const int DefaultMinValue = 10;
        private const int DefaultMaxValue = 20;
        [UIValue("min-value")]
        private const int MinValue = 1;
        [UIValue("max-value")]
        private const int MaxValue = 50;

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.NJSFilterView.bsml"), viewContainer, this);
            ViewGameObject.name = "NJSFilterViewContainer";

            _isInitialized = true;
        }

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;
            _easyStagingValue = false;
            _normalStagingValue = false;
            _hardStagingValue = false;
            _expertStagingValue = false;
            _expertPlusStagingValue = false;

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
            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

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
            _easyAppliedValue = _easyStagingValue;
            _normalAppliedValue = _normalStagingValue;
            _hardAppliedValue = _hardStagingValue;
            _expertAppliedValue = _expertStagingValue;
            _expertPlusAppliedValue = _expertPlusStagingValue;
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledAppliedValue = false;
            _maxEnabledAppliedValue = false;
            _minAppliedValue = DefaultMinValue;
            _maxAppliedValue = DefaultMaxValue;
            _easyAppliedValue = false;
            _normalAppliedValue = false;
            _hardAppliedValue = false;
            _expertAppliedValue = false;
            _expertPlusAppliedValue = false;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails details = detailsList[i];

                // don't filter out OST beatmaps
                if (details.IsOST)
                {
                    ++i;
                    continue;
                }

                var difficultySets = details.DifficultyBeatmapSets;

                if (!(TestDifficulty(BeatmapDifficulty.Easy, _easyAppliedValue, difficultySets) ||
                    TestDifficulty(BeatmapDifficulty.Normal, _normalAppliedValue, difficultySets) ||
                    TestDifficulty(BeatmapDifficulty.Hard, _hardAppliedValue, difficultySets) ||
                    TestDifficulty(BeatmapDifficulty.Expert, _expertAppliedValue, difficultySets) ||
                    TestDifficulty(BeatmapDifficulty.ExpertPlus, _expertPlusAppliedValue, difficultySets)))
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }

        /// <summary>
        /// Checks whether a beatmap fulfills the NJS filter settings.
        /// </summary>
        /// <param name="difficulty">The difficulty to check.</param>
        /// <param name="difficultyAppliedValue">The applied value of the difficulty.</param>
        /// <param name="difficultyBeatmapSets"></param>
        /// <returns>True, if the beatmap contains at least one difficulty that fulfills the filter settings. Otherwise, false.</returns>
        private bool TestDifficulty(BeatmapDifficulty difficulty, bool difficultyAppliedValue, SimplifiedDifficultyBeatmapSet[] difficultyBeatmapSets)
        {
            if (!difficultyAppliedValue)
                return true;

            bool difficultyFound = false;
            foreach (var difficultyBeatmapSet in difficultyBeatmapSets)
            {
                var difficultyBeatmap = difficultyBeatmapSet.DifficultyBeatmaps.Cast<SimplifiedDifficultyBeatmap?>().FirstOrDefault(x => x.Value.Difficulty == difficulty);
                if (!(difficultyBeatmap is null))
                {
                    difficultyFound = true;

                    if ((!_minEnabledAppliedValue || difficultyBeatmap.Value.NoteJumpMovementSpeed >= _minAppliedValue) &&
                        (!_maxEnabledAppliedValue || difficultyBeatmap.Value.NoteJumpMovementSpeed <= _maxAppliedValue))
                        return true;
                }
            }

            // if we don't find a difficulty that we can test, then we don't filter it out
            return !difficultyFound;
        }

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("difficulty-changed")]
        private void OnDifficultyChanged(bool value) => SettingChanged?.Invoke();

        [UIAction("min-checkbox-changed")]
        private void OnMinCheckboxChanged(bool value)
        {
            _minSetting.gameObject.SetActive(_minEnabledStagingValue);

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
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_easyStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_normalStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_hardStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_expertStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_expertPlusStagingValue)));
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
