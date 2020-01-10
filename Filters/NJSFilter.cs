using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class NJSFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Note Jump Speed (NJS)"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status {
            get
            {
                if (IsFilterApplied)
                {
                    if (HasChanges)
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
                    return FilterStatus.NotApplied;
                }
            }
        }
        public bool IsFilterApplied => (_minEnabledAppliedValue || _maxEnabledAppliedValue) &&
            (_easyAppliedValue || _normalAppliedValue || _hardAppliedValue || _expertAppliedValue || _expertPlusAppliedValue);
        public bool HasChanges => _minEnabledStagingValue != _minEnabledAppliedValue ||
            _maxEnabledStagingValue != _maxEnabledAppliedValue ||
            _minStagingValue != _minAppliedValue ||
            _maxStagingValue != _maxAppliedValue ||
            _easyStagingValue != _easyAppliedValue ||
            _normalStagingValue != _normalAppliedValue ||
            _hardStagingValue != _hardAppliedValue ||
            _expertStagingValue != _expertAppliedValue ||
            _expertPlusStagingValue != _expertPlusAppliedValue;
        public bool IsStagingDefaultValues => _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue &&
            _easyStagingValue == false &&
            _normalStagingValue == false &&
            _hardStagingValue == false &&
            _expertStagingValue == false &&
            _expertPlusStagingValue == false;

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
        public bool MinEnableStagingValue
        {
            get => _minEnabledStagingValue;
            set
            {
                _minEnabledStagingValue = value;
                _minSetting.gameObject.SetActive(_minEnabledStagingValue);

                ValidateMinValue();

                SettingChanged?.Invoke();
            }
        }
        private bool _maxEnabledStagingValue = false;
        [UIValue("max-checkbox-value")]
        public bool MaxEnableStagingValue
        {
            get => _maxEnabledStagingValue;
            set
            {
                _maxEnabledStagingValue = value;
                _maxSetting.gameObject.SetActive(_maxEnabledStagingValue);

                ValidateMaxValue();

                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
            }
        }
        private bool _easyStagingValue = false;
        [UIValue("easy-value")]
        public bool EasyStagingValue
        {
            get => _easyStagingValue;
            set
            {
                _easyStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private bool _normalStagingValue = false;
        [UIValue("normal-value")]
        public bool NormalStagingValue
        {
            get => _normalStagingValue;
            set
            {
                _normalStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private bool _hardStagingValue = false;
        [UIValue("hard-value")]
        public bool HardStagingValue
        {
            get => _hardStagingValue;
            set
            {
                _hardStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private bool _expertStagingValue = false;
        [UIValue("expert-value")]
        public bool ExpertStagingValue
        {
            get => _expertStagingValue;
            set
            {
                _expertStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private bool _expertPlusStagingValue = false;
        [UIValue("expert-plus-value")]
        public bool ExpertPlusStagingValue
        {
            get => _expertPlusStagingValue;
            set
            {
                _expertPlusStagingValue = value;
                SettingChanged?.Invoke();
            }
        }

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
        private BSMLParserParams _parserParams;

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

            _parserParams = BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.NJSFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "NJSFilterViewContainer";

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
            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

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
                if (difficultyBeatmapSet.DifficultyBeatmaps.Any(x => x.Difficulty == difficulty))
                {
                    difficultyFound = true;

                    var difficultyBeatmap = difficultyBeatmapSet.DifficultyBeatmaps.First(x => x.Difficulty == difficulty);

                    if ((!_minEnabledAppliedValue || difficultyBeatmap.NoteJumpMovementSpeed >= _minAppliedValue) &&
                        (!_maxEnabledAppliedValue || difficultyBeatmap.NoteJumpMovementSpeed <= _maxAppliedValue))
                        return true;
                }
            }

            // if we don't find a difficulty that we can test, then we don't filter it out
            return !difficultyFound;
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
