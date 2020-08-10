using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    public class CharacteristicsFilter : FilterBase
    {
        public override string Name => FilterName;
        public override bool IsFilterApplied => OneSaberAppliedValue ||
            NoArrowsAppliedValue ||
            Mode90AppliedValue ||
            Mode360AppliedValue ||
            LightshowAppliedValue;
        public override bool HasChanges => OneSaberAppliedValue != _oneSaberStagingValue ||
            NoArrowsAppliedValue != _noArrowsStagingValue ||
            Mode90AppliedValue != _90StagingValue ||
            Mode360AppliedValue != _360StagingValue ||
            LightshowAppliedValue != _lightshowStagingValue;
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

        public bool OneSaberAppliedValue { get; private set; } = false;
        public bool NoArrowsAppliedValue { get; private set; } = false;
        public bool Mode90AppliedValue { get; private set; } = false;
        public bool Mode360AppliedValue { get; private set; } = false;
        public bool LightshowAppliedValue { get; private set; } = false;

        public const string FilterName = "Beatmap Characteristics";

        public const string OneSaberSerializedCharacteristicName = "OneSaber";
        public const string NoArrowsSerializedCharacteristicName = "NoArrows";
        public const string Mode90DegreeSerializedCharacteristicName = "90Degree";
        public const string Mode360DegreeSerializedCharacteristicName = "360Degree";

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
            _oneSaberStagingValue = OneSaberAppliedValue;
            _noArrowsStagingValue = NoArrowsAppliedValue;
            _90StagingValue = Mode90AppliedValue;
            _360StagingValue = Mode360AppliedValue;
            _lightshowStagingValue = LightshowAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            OneSaberAppliedValue = _oneSaberStagingValue;
            NoArrowsAppliedValue = _noArrowsStagingValue;
            Mode90AppliedValue = _90StagingValue;
            Mode360AppliedValue = _360StagingValue;
            LightshowAppliedValue = _lightshowStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            OneSaberAppliedValue = false;
            NoArrowsAppliedValue = false;
            Mode90AppliedValue = false;
            Mode360AppliedValue = false;
            LightshowAppliedValue = false;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails beatmap = detailsList[i];

                if (LightshowAppliedValue &&
                    !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.DifficultyBeatmaps.Any(diff => diff.NotesCount == 0)))
                {
                    detailsList.RemoveAt(i);
                }
                else if (OneSaberAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == OneSaberSerializedCharacteristicName))
                {
                    detailsList.RemoveAt(i);
                }
                else if (NoArrowsAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == NoArrowsSerializedCharacteristicName))
                {
                    detailsList.RemoveAt(i);
                }
                else if (Mode90AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == Mode90DegreeSerializedCharacteristicName))
                {
                    detailsList.RemoveAt(i);
                }
                else if (Mode360AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == Mode360DegreeSerializedCharacteristicName))
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
                "oneSaber", OneSaberAppliedValue,
                "noArrows", NoArrowsAppliedValue,
                "90Degree", Mode90AppliedValue,
                "360Degree", Mode360AppliedValue,
                "lightshow", LightshowAppliedValue);
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
