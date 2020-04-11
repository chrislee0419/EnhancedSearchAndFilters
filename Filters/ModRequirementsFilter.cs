using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SongCore;
using SongCore.Utilities;
using SongCore.Data;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class ModRequirementsFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name => "Mod Requirements";
        public bool IsAvailable => true;
        public FilterStatus Status
        {
            get
            {
                if (HasChanges)
                    return IsFilterApplied ? FilterStatus.AppliedAndChanged : FilterStatus.NotAppliedAndChanged;
                else
                    return IsFilterApplied ? FilterStatus.Applied : FilterStatus.NotApplied;
            }
        }
        public bool IsFilterApplied => _mappingExtensionsAppliedValue != ModRequirementFilterOption.Off ||
            _noodleExtensionsAppliedValue != ModRequirementFilterOption.Off ||
            _chromaAppliedValue != ModRequirementFilterOption.Off;
        public bool HasChanges => _mappingExtensionsStagingValue != _mappingExtensionsAppliedValue ||
            _noodleExtensionsStagingValue != _noodleExtensionsAppliedValue ||
            _chromaStagingValue != _chromaAppliedValue;
        public bool IsStagingDefaultValues => _mappingExtensionsStagingValue == ModRequirementFilterOption.Off &&
            _noodleExtensionsStagingValue == ModRequirementFilterOption.Off &&
            _chromaStagingValue == ModRequirementFilterOption.Off;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;
#pragma warning restore CS0649

        private ModRequirementFilterOption _mappingExtensionsStagingValue = ModRequirementFilterOption.Off;
        [UIValue("mapping-extensions-value")]
        public ModRequirementFilterOption MappingExtensionsValue
        {
            get => _mappingExtensionsStagingValue;
            set
            {
                _mappingExtensionsStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private ModRequirementFilterOption _noodleExtensionsStagingValue = ModRequirementFilterOption.Off;
        [UIValue("noodle-extensions-value")]
        public ModRequirementFilterOption NoodleExtensionsValue
        {
            get => _noodleExtensionsStagingValue;
            set
            {
                _noodleExtensionsStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private ModRequirementFilterOption _chromaStagingValue = ModRequirementFilterOption.Off;
        [UIValue("chroma-value")]
        public ModRequirementFilterOption ChromaValue
        {
            get => _chromaStagingValue;
            set
            {
                _chromaStagingValue = value;
                SettingChanged?.Invoke();
            }
        }

        private ModRequirementFilterOption _mappingExtensionsAppliedValue = ModRequirementFilterOption.Off;
        private ModRequirementFilterOption _noodleExtensionsAppliedValue = ModRequirementFilterOption.Off;
        private ModRequirementFilterOption _chromaAppliedValue = ModRequirementFilterOption.Off;

        private BSMLParserParams _parserParams;

        [UIValue("mod-requirements-options")]
        private static readonly List<object> ModRequirementsOptions = Enum.GetValues(typeof(ModRequirementFilterOption)).Cast<ModRequirementFilterOption>().Select(x => (object)x).ToList();

        public void Init(GameObject viewContainer)
        {
            if (_viewGameObject != null)
                return;

            _parserParams = UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.Filters.ModRequirementsFilterView.bsml", viewContainer, this);
            _viewGameObject.name = "ModRequirementsViewContainer";
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
            _mappingExtensionsStagingValue = ModRequirementFilterOption.Off;
            _noodleExtensionsStagingValue = ModRequirementFilterOption.Off;
            _chromaStagingValue = ModRequirementFilterOption.Off;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            _mappingExtensionsStagingValue = _mappingExtensionsAppliedValue;
            _noodleExtensionsStagingValue = _noodleExtensionsAppliedValue;
            _chromaStagingValue = _chromaAppliedValue;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            _mappingExtensionsAppliedValue = _mappingExtensionsStagingValue;
            _noodleExtensionsAppliedValue = _noodleExtensionsStagingValue;
            _chromaAppliedValue = _chromaStagingValue;
        }

        public void ApplyDefaultValues()
        {
            _mappingExtensionsAppliedValue = ModRequirementFilterOption.Off;
            _noodleExtensionsAppliedValue = ModRequirementFilterOption.Off;
            _chromaAppliedValue = ModRequirementFilterOption.Off;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            List<CustomPreviewBeatmapLevel> allCustomLevels = Loader.CustomLevels.Values.ToList();
            allCustomLevels.AddRange(Loader.CustomWIPLevels.Values);
            allCustomLevels.AddRange(Loader.CachedWIPLevels.Values);
            foreach (var folder in Loader.SeperateSongFolders)
                allCustomLevels.AddRange(folder.Levels.Values);

            bool mappingExtensionsApplied = _mappingExtensionsAppliedValue != ModRequirementFilterOption.Off;
            bool noodleExtensionsApplied = _noodleExtensionsAppliedValue != ModRequirementFilterOption.Off;
            bool chromaApplied = _chromaAppliedValue != ModRequirementFilterOption.Off;

            var levelsToRemove = detailsList.AsParallel().Where(delegate (BeatmapDetails details)
            {
                if (details.IsOST)
                    return true;

                CustomPreviewBeatmapLevel customLevel = allCustomLevels.FirstOrDefault(x => x.levelID.StartsWith(details.LevelID));
                if (customLevel == null)
                    return true;

                ExtraSongData songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                if (songData == null)
                    return true;

                if (mappingExtensionsApplied)
                {
                    bool meRequired = songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Mapping Extensions") ?? false) ?? false;
                    if ((_mappingExtensionsAppliedValue == ModRequirementFilterOption.Required && !meRequired) ||
                        (_mappingExtensionsAppliedValue == ModRequirementFilterOption.NotRequired && meRequired))
                        return true;
                }
                if (noodleExtensionsApplied)
                {
                    bool nRequired = songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Noodle Extensions") ?? false) ?? false;
                    if ((_noodleExtensionsAppliedValue == ModRequirementFilterOption.Required && !nRequired) ||
                        (_noodleExtensionsAppliedValue == ModRequirementFilterOption.NotRequired && nRequired))
                        return true;
                }
                if (chromaApplied)
                {
                    bool cRequired = songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Chroma") ?? false) ?? false;
                    if ((_chromaAppliedValue == ModRequirementFilterOption.Required && !cRequired) ||
                        (_chromaAppliedValue == ModRequirementFilterOption.NotRequired && cRequired))
                        return true;
                }

                return false;
            }).ToList();

            foreach (var level in levelsToRemove)
                detailsList.Remove(level);
        }

        public List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "mappingExtensions", _mappingExtensionsAppliedValue,
                "noodleExtensions", _noodleExtensionsAppliedValue,
                "chroma", _chromaAppliedValue);
        }

        public void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (Enum.TryParse(pair.Value, out ModRequirementFilterOption value))
                {
                    switch (pair.Key)
                    {
                        case "mappingExtensions":
                            _mappingExtensionsStagingValue = value;
                            break;
                        case "noodleExtensions":
                            _noodleExtensionsStagingValue = value;
                            break;
                        case "chroma":
                            _chromaStagingValue = value;
                            break;
                    }
                }
            }

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        [UIAction("mod-requirements-formatter")]
        private string ModRequirementsFormatter(object value)
        {
            switch ((ModRequirementFilterOption)value)
            {
                case ModRequirementFilterOption.Off:
                    return "Off";
                case ModRequirementFilterOption.NotRequired:
                    return "Not Required";
                case ModRequirementFilterOption.Required:
                    return "Required";
                default:
                    return "ERROR!";
            }
        }
    }

    internal enum ModRequirementFilterOption
    {
        Off,
        Required,
        NotRequired
    }
}
