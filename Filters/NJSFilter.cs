using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class NJSFilter : FilterBase
    {
        public override string Name => "Note Jump Speed (NJS)";
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
        public override bool HasChanges => _minEnabledStagingValue != _minEnabledAppliedValue ||
            _maxEnabledStagingValue != _maxEnabledAppliedValue ||
            _minStagingValue != _minAppliedValue ||
            _maxStagingValue != _maxAppliedValue;
        public override bool IsStagingDefaultValues => _minEnabledStagingValue == false &&
            _maxEnabledStagingValue == false &&
            _minStagingValue == DefaultMinValue &&
            _maxStagingValue == DefaultMaxValue;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.NJSFilterView.bsml";
        protected override string ContainerGameObjectName => "NJSFilterViewContainer";

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

        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private int _minAppliedValue = DefaultMinValue;
        private int _maxAppliedValue = DefaultMaxValue;

        private const int DefaultMinValue = 10;
        private const int DefaultMaxValue = 20;
        [UIValue("min-value")]
        private const int MinValue = 1;
        [UIValue("max-value")]
        private const int MaxValue = 50;

        public override void Init(GameObject viewContainer)
        {
            base.Init(viewContainer);

            // ensure that the UI correctly reflects the staging values
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

            bool testEasy = true;
            bool testNormal = true;
            bool testHard = true;
            bool testExpert = true;
            bool testExpertPlus = true;

            DifficultyFilter diffFilter = FilterList.ActiveFilters.Where(x => x.Name == DifficultyFilter.FilterName && x.IsFilterApplied).FirstOrDefault() as DifficultyFilter;
            if (diffFilter != null)
            {
                testEasy = diffFilter.EasyAppliedValue;
                testNormal = diffFilter.NormalAppliedValue;
                testHard = diffFilter.HardAppliedValue;
                testExpert = diffFilter.ExpertAppliedValue;
                testExpertPlus = diffFilter.ExpertPlusAppliedValue;
            }

            const string StandardCharacteristicName = "Standard";
            const string LightmapCharacteristicName = "Lightmap";
            List<string> testCharacteristics;
            CharacteristicsFilter charFilter = FilterList.ActiveFilters.Where(x => x.Name == CharacteristicsFilter.FilterName && x.IsFilterApplied).FirstOrDefault() as CharacteristicsFilter;
            if (charFilter != null)
            {
                if (charFilter.LightshowAppliedValue && !charFilter.OneSaberAppliedValue && !charFilter.NoArrowsAppliedValue && !charFilter.Mode90AppliedValue && !charFilter.Mode360AppliedValue)
                {
                    testCharacteristics = new List<string>
                    {
                        StandardCharacteristicName,
                        LightmapCharacteristicName,
                        CharacteristicsFilter.OneSaberSerializedCharacteristicName,
                        CharacteristicsFilter.NoArrowsSerializedCharacteristicName,
                        CharacteristicsFilter.Mode90DegreeSerializedCharacteristicName,
                        CharacteristicsFilter.Mode360DegreeSerializedCharacteristicName,
                    };
                }
                else
                {
                    testCharacteristics = new List<string>();
                    if (charFilter.OneSaberAppliedValue)
                        testCharacteristics.Add(CharacteristicsFilter.OneSaberSerializedCharacteristicName);
                    if (charFilter.NoArrowsAppliedValue)
                        testCharacteristics.Add(CharacteristicsFilter.NoArrowsSerializedCharacteristicName);
                    if (charFilter.Mode90AppliedValue)
                        testCharacteristics.Add(CharacteristicsFilter.Mode90DegreeSerializedCharacteristicName);
                    if (charFilter.Mode360AppliedValue)
                        testCharacteristics.Add(CharacteristicsFilter.Mode360DegreeSerializedCharacteristicName);
                }
            }
            else
            {
                testCharacteristics = new List<string>
                    {
                        StandardCharacteristicName,
                        LightmapCharacteristicName,
                        CharacteristicsFilter.OneSaberSerializedCharacteristicName,
                        CharacteristicsFilter.NoArrowsSerializedCharacteristicName,
                        CharacteristicsFilter.Mode90DegreeSerializedCharacteristicName,
                        CharacteristicsFilter.Mode360DegreeSerializedCharacteristicName,
                    };
            }

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails details = detailsList[i];

                // don't filter out OST beatmaps
                if (details.IsOST)
                {
                    ++i;
                    continue;
                }

                var difficultySets = details.DifficultyBeatmapSets.Where(x => testCharacteristics.Contains(x.CharacteristicName));

                if (TestDifficulty(BeatmapDifficulty.Easy, testEasy, difficultySets) &&
                    TestDifficulty(BeatmapDifficulty.Normal, testNormal, difficultySets) &&
                    TestDifficulty(BeatmapDifficulty.Hard, testHard, difficultySets) &&
                    TestDifficulty(BeatmapDifficulty.Expert, testExpert, difficultySets) &&
                    TestDifficulty(BeatmapDifficulty.ExpertPlus, testExpertPlus, difficultySets))
                    ++i;
                else
                    detailsList.RemoveAt(i);
            }
        }

        /// <summary>
        /// Checks whether a beatmap fulfills the NJS filter settings.
        /// </summary>
        /// <param name="difficulty">The difficulty to check.</param>
        /// <param name="difficultyAppliedValue">The applied value of the difficulty.</param>
        /// <param name="difficultyBeatmapSets"></param>
        /// <returns>True, if the beatmap contains at least one difficulty that fulfills the filter settings or we don't need to check. Otherwise, false.</returns>
        private bool TestDifficulty(BeatmapDifficulty difficulty, bool difficultyAppliedValue, IEnumerable<SimplifiedDifficultyBeatmapSet> difficultyBeatmapSets)
        {
            if (!difficultyAppliedValue)
                return true;

            bool difficultyFound = false;
            foreach (var difficultyBeatmapSet in difficultyBeatmapSets)
            {
                if (difficultyBeatmapSet.DifficultyBeatmaps.Any(x => x.Difficulty == difficulty))
                {
                    var difficultyBeatmap = difficultyBeatmapSet.DifficultyBeatmaps.First(x => x.Difficulty == difficulty);

                    // do not count lightmaps
                    if (difficultyBeatmap.NotesCount == 0)
                        continue;

                    difficultyFound = true;

                    if ((!_minEnabledAppliedValue || difficultyBeatmap.NoteJumpMovementSpeed >= _minAppliedValue) &&
                        (!_maxEnabledAppliedValue || difficultyBeatmap.NoteJumpMovementSpeed <= _maxAppliedValue))
                        return true;
                }
            }

            // if we don't find a difficulty that we can test, then we don't filter it out
            return !difficultyFound;
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
                else if (int.TryParse(pair.Value, out int intValue))
                {
                    if (pair.Key == "minValue")
                        _minStagingValue = intValue;
                    else if (pair.Key == "maxValue")
                        _maxStagingValue = intValue;
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
