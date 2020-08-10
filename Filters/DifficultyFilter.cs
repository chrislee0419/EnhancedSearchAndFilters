using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    public class DifficultyFilter : FilterBase
    {
        public override string Name => FilterName;
        public override bool IsFilterApplied => EasyAppliedValue || NormalAppliedValue || HardAppliedValue || ExpertAppliedValue || ExpertPlusAppliedValue;
        public override bool HasChanges => _easyStagingValue != EasyAppliedValue ||
            _normalStagingValue != NormalAppliedValue ||
            _hardStagingValue != HardAppliedValue ||
            _expertStagingValue != ExpertAppliedValue ||
            _expertPlusStagingValue != ExpertPlusAppliedValue;
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

        public bool EasyAppliedValue { get; private set; } = false;
        public bool NormalAppliedValue { get; private set; } = false;
        public bool HardAppliedValue { get; private set; } = false;
        public bool ExpertAppliedValue { get; private set; } = false;
        public bool ExpertPlusAppliedValue { get; private set; } = false;

        public const string FilterName = "Difficulty";

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
            _easyStagingValue = EasyAppliedValue;
            _normalStagingValue = NormalAppliedValue;
            _hardStagingValue = HardAppliedValue;
            _expertStagingValue = ExpertAppliedValue;
            _expertPlusStagingValue = ExpertPlusAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            EasyAppliedValue = _easyStagingValue;
            NormalAppliedValue = _normalStagingValue;
            HardAppliedValue = _hardStagingValue;
            ExpertAppliedValue = _expertStagingValue;
            ExpertPlusAppliedValue = _expertPlusStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            EasyAppliedValue = false;
            NormalAppliedValue = false;
            HardAppliedValue = false;
            ExpertAppliedValue = false;
            ExpertPlusAppliedValue = false;
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

                    if ((!EasyAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)) &&
                        (!NormalAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Normal && x.Item2)) &&
                        (!HardAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Hard && x.Item2)) &&
                        (!ExpertAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Expert && x.Item2)) &&
                        (!ExpertPlusAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.ExpertPlus && x.Item2)))
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
                "easy", EasyAppliedValue,
                "normal", NormalAppliedValue,
                "hard", HardAppliedValue,
                "expert", ExpertAppliedValue,
                "expertPlus", ExpertPlusAppliedValue);
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
