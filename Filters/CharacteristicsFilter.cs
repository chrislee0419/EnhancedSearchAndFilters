using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class CharacteristicsFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Beatmap Characteristics"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                {
                    if (HasChanges)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (!IsStagingDefaultValues)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotApplied;
                }
            }
        }
        public bool IsFilterApplied => _oneSaberAppliedValue ||
            _noArrowsAppliedValue ||
            _90AppliedValue ||
            _360AppliedValue ||
            _lightshowAppliedValue;
        public bool HasChanges => _oneSaberAppliedValue != _oneSaberStagingValue ||
            _noArrowsAppliedValue != _noArrowsStagingValue ||
            _90AppliedValue != _90StagingValue ||
            _360AppliedValue != _360StagingValue ||
            _lightshowAppliedValue != _lightshowStagingValue;
        public bool IsStagingDefaultValues => _oneSaberStagingValue == false &&
            _noArrowsStagingValue == false &&
            _90StagingValue == false &&
            _360StagingValue == false &&
            _lightshowStagingValue == false;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;
#pragma warning restore CS0649

        private bool _oneSaberStagingValue = false;
        [UIValue("one-saber-value")]
        public bool OneSaberStagingValue
        {
            get => _oneSaberStagingValue;
            set
            {
                _oneSaberStagingValue = value;
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
            }
        }

        private bool _oneSaberAppliedValue = false;
        private bool _noArrowsAppliedValue = false;
        private bool _90AppliedValue = false;
        private bool _360AppliedValue = false;
        private bool _lightshowAppliedValue = false;

        private BSMLParserParams _parserParams;

        public void Init(GameObject viewContainer)
        {
            if (_viewGameObject != null)
                return;

            _parserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.Filters.CharacteristicsFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "CharacteristicsFilterViewContainer";
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
            _oneSaberStagingValue = false;
            _noArrowsStagingValue = false;
            _90StagingValue = false;
            _360StagingValue = false;
            _lightshowStagingValue = false;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            _oneSaberStagingValue = _oneSaberAppliedValue;
            _noArrowsStagingValue = _noArrowsAppliedValue;
            _90StagingValue = _90AppliedValue;
            _360StagingValue = _360AppliedValue;
            _lightshowStagingValue = _lightshowAppliedValue;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            _oneSaberAppliedValue = _oneSaberStagingValue;
            _noArrowsAppliedValue = _noArrowsStagingValue;
            _90AppliedValue = _90StagingValue;
            _360AppliedValue = _360StagingValue;
            _lightshowAppliedValue = _lightshowStagingValue;
        }

        public void ApplyDefaultValues()
        {
            _oneSaberAppliedValue = false;
            _noArrowsAppliedValue = false;
            _90AppliedValue = false;
            _360AppliedValue = false;
            _lightshowAppliedValue = false;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
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

        public List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "oneSaber", _oneSaberAppliedValue,
                "noArrows", _noArrowsAppliedValue,
                "90Degree", _90AppliedValue,
                "360Degree", _360AppliedValue,
                "lightshow", _lightshowAppliedValue);
        }

        public void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
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

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }
    }
}
