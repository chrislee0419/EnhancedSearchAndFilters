using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class DifficultyFilter : FilterBase
    {
        public override string Name => "Difficulty";
        public override bool IsFilterApplied => _easyAppliedValue || _normalAppliedValue || _hardAppliedValue || _expertAppliedValue || _expertPlusAppliedValue;
        public override bool HasChanges => _easyStagingValue != _easyAppliedValue ||
            _normalStagingValue != _normalAppliedValue ||
            _hardStagingValue != _hardAppliedValue ||
            _expertStagingValue != _expertAppliedValue ||
            _expertPlusStagingValue != _expertPlusAppliedValue;
        public override bool IsStagingDefaultValues => _easyStagingValue == false &&
            _normalStagingValue == false &&
            _hardStagingValue == false &&
            _expertStagingValue == false &&
            _expertPlusStagingValue == false;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.DifficultyFilterView.bsml";
        protected override string ContainerGameObjectName => "DifficultyFilterViewContainer";

        private bool _easyStagingValue = false;
        [UIValue("easy-checkbox-value")]
        public bool EasyStagingValue
        {
            get => _easyStagingValue;
            set
            {
                _easyStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _normalStagingValue = false;
        [UIValue("normal-checkbox-value")]
        public bool NormalStagingValue
        {
            get => _normalStagingValue;
            set
            {
                _normalStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _hardStagingValue = false;
        [UIValue("hard-checkbox-value")]
        public bool HardStagingValue
        {
            get => _hardStagingValue;
            set
            {
                _hardStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _expertStagingValue = false;
        [UIValue("expert-checkbox-value")]
        public bool ExpertStagingValue
        {
            get => _expertStagingValue;
            set
            {
                _expertStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _expertPlusStagingValue = false;
        [UIValue("expert-plus-checkbox-value")]
        public bool ExpertPlusStagingValue
        {
            get => _expertPlusStagingValue;
            set
            {
                _expertPlusStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        public override void SetDefaultValuesToStaging()
        {
            _easyStagingValue = false;
            _normalStagingValue = false;
            _hardStagingValue = false;
            _expertStagingValue = false;
            _expertPlusStagingValue = false;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            _easyAppliedValue = _easyStagingValue;
            _normalAppliedValue = _normalStagingValue;
            _hardAppliedValue = _hardStagingValue;
            _expertAppliedValue = _expertStagingValue;
            _expertPlusAppliedValue = _expertPlusStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _easyAppliedValue = false;
            _normalAppliedValue = false;
            _hardAppliedValue = false;
            _expertAppliedValue = false;
            _expertPlusAppliedValue = false;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                bool remove = true;
                foreach (var difficultySet in detailsList[i].DifficultyBeatmapSets)
                {
                    var difficulties = difficultySet.DifficultyBeatmaps.Select(x => (x.Difficulty, x.NotesCount != 0)).ToArray();

                    if ((!_easyAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)) &&
                        (!_normalAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Normal && x.Item2)) &&
                        (!_hardAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Hard && x.Item2)) &&
                        (!_expertAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Expert && x.Item2)) &&
                        (!_expertPlusAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.ExpertPlus && x.Item2)))
                    {
                        remove = false;
                        break;
                    }
                }

                if (remove)
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "easy", _easyAppliedValue,
                "normal", _normalAppliedValue,
                "hard", _hardAppliedValue,
                "expert", _expertAppliedValue,
                "expertPlus", _expertPlusAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (!bool.TryParse(pair.Value, out bool value))
                    continue;

                switch (pair.Key)
                {
                    case "easy":
                        _easyStagingValue = value;
                        break;
                    case "normal":
                        _normalStagingValue = value;
                        break;
                    case "hard":
                        _hardStagingValue = value;
                        break;
                    case "expert":
                        _expertStagingValue = value;
                        break;
                    case "expertPlus":
                        _expertPlusStagingValue = value;
                        break;
                }
            }

            RefreshValues();
        }
    }
}
