using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class CharacteristicsFilter : FilterBase
    {
        public override string Name => "Beatmap Characteristics";
        public override bool IsFilterApplied => _oneSaberAppliedValue ||
            _noArrowsAppliedValue ||
            _90AppliedValue ||
            _360AppliedValue ||
            _lightshowAppliedValue;
        public override bool HasChanges => _oneSaberAppliedValue != _oneSaberStagingValue ||
            _noArrowsAppliedValue != _noArrowsStagingValue ||
            _90AppliedValue != _90StagingValue ||
            _360AppliedValue != _360StagingValue ||
            _lightshowAppliedValue != _lightshowStagingValue;
        public override bool IsStagingDefaultValues => _oneSaberStagingValue == false &&
            _noArrowsStagingValue == false &&
            _90StagingValue == false &&
            _360StagingValue == false &&
            _lightshowStagingValue == false;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.CharacteristicsFilterView.bsml";
        protected override string ContainerGameObjectName => "CharacteristicsFilterViewContainer";

        private bool _oneSaberStagingValue = false;
        [UIValue("one-saber-value")]
        public bool OneSaberStagingValue
        {
            get => _oneSaberStagingValue;
            set
            {
                _oneSaberStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _noArrowsStagingValue = false;
        [UIValue("no-arrows-value")]
        public bool NoArrowsStagingValue
        {
            get => _noArrowsStagingValue;
            set
            {
                _noArrowsStagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _90StagingValue = false;
        [UIValue("90-value")]
        public bool Mode90StagingValue
        {
            get => _90StagingValue;
            set
            {
                _90StagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _360StagingValue = false;
        [UIValue("360-value")]
        public bool Mode360StagingValue
        {
            get => _360StagingValue;
            set
            {
                _360StagingValue = value;
                InvokeSettingChanged();
            }
        }
        private bool _lightshowStagingValue = false;
        [UIValue("lightshow-value")]
        public bool LightshowStagingValue
        {
            get => _lightshowStagingValue;
            set
            {
                _lightshowStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _oneSaberAppliedValue = false;
        private bool _noArrowsAppliedValue = false;
        private bool _90AppliedValue = false;
        private bool _360AppliedValue = false;
        private bool _lightshowAppliedValue = false;

        public override void SetDefaultValuesToStaging()
        {
            _oneSaberStagingValue = false;
            _noArrowsStagingValue = false;
            _90StagingValue = false;
            _360StagingValue = false;
            _lightshowStagingValue = false;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _oneSaberStagingValue = _oneSaberAppliedValue;
            _noArrowsStagingValue = _noArrowsAppliedValue;
            _90StagingValue = _90AppliedValue;
            _360StagingValue = _360AppliedValue;
            _lightshowStagingValue = _lightshowAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            _oneSaberAppliedValue = _oneSaberStagingValue;
            _noArrowsAppliedValue = _noArrowsStagingValue;
            _90AppliedValue = _90StagingValue;
            _360AppliedValue = _360StagingValue;
            _lightshowAppliedValue = _lightshowStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _oneSaberAppliedValue = false;
            _noArrowsAppliedValue = false;
            _90AppliedValue = false;
            _360AppliedValue = false;
            _lightshowAppliedValue = false;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails beatmap = detailsList[i];

                if (_lightshowAppliedValue &&
                    !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.DifficultyBeatmaps.Any(diff => diff.NotesCount == 0)))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_oneSaberAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "OneSaber"))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_noArrowsAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "NoArrows"))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_90AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "90Degree"))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_360AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "360Degree"))
                {
                    detailsList.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "oneSaber", _oneSaberAppliedValue,
                "noArrows", _noArrowsAppliedValue,
                "90Degree", _90AppliedValue,
                "360Degree", _360AppliedValue,
                "lightshow", _lightshowAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (bool.TryParse(pair.Value, out bool boolValue))
                {
                    switch (pair.Key)
                    {
                        case "oneSaber":
                            _oneSaberStagingValue = boolValue;
                            break;
                        case "noArrows":
                            _noArrowsStagingValue = boolValue;
                            break;
                        case "90Degree":
                            _90StagingValue = boolValue;
                            break;
                        case "360Degree":
                            _360StagingValue = boolValue;
                            break;
                        case "lightshow":
                            _lightshowStagingValue = boolValue;
                            break;
                    }
                }
            }

            RefreshValues();
        }
    }
}
