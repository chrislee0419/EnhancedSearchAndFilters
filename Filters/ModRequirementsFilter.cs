using System;
using System.Collections.Generic;
using System.Linq;
using SongCore;
using SongCore.Utilities;
using SongCore.Data;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class ModRequirementsFilter : FilterBase
    {
        public override string Name => "Mod Requirements";
        public override bool IsFilterApplied => _mappingExtensionsAppliedValue != ModRequirementFilterOption.Off ||
            _noodleExtensionsAppliedValue != ModRequirementFilterOption.Off ||
            _chromaAppliedValue != ModRequirementFilterOption.Off;
        public override bool HasChanges => _mappingExtensionsStagingValue != _mappingExtensionsAppliedValue ||
            _noodleExtensionsStagingValue != _noodleExtensionsAppliedValue ||
            _chromaStagingValue != _chromaAppliedValue;
        public override bool IsStagingDefaultValues => _mappingExtensionsStagingValue == ModRequirementFilterOption.Off &&
            _noodleExtensionsStagingValue == ModRequirementFilterOption.Off &&
            _chromaStagingValue == ModRequirementFilterOption.Off;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.ModRequirementsFilterView.bsml";
        protected override string ContainerGameObjectName => "ModRequirementsViewContainer";

        private ModRequirementFilterOption _mappingExtensionsStagingValue = ModRequirementFilterOption.Off;
        [UIValue("mapping-extensions-value")]
        public ModRequirementFilterOption MappingExtensionsValue
        {
            get => _mappingExtensionsStagingValue;
            set
            {
                _mappingExtensionsStagingValue = value;
                InvokeSettingChanged();
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
                InvokeSettingChanged();
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
                InvokeSettingChanged();
            }
        }

        private ModRequirementFilterOption _mappingExtensionsAppliedValue = ModRequirementFilterOption.Off;
        private ModRequirementFilterOption _noodleExtensionsAppliedValue = ModRequirementFilterOption.Off;
        private ModRequirementFilterOption _chromaAppliedValue = ModRequirementFilterOption.Off;

        [UIValue("mod-requirements-options")]
        private static readonly List<object> ModRequirementsOptions = Enum.GetValues(typeof(ModRequirementFilterOption)).Cast<ModRequirementFilterOption>().Select(x => (object)x).ToList();

        public override void SetDefaultValuesToStaging()
        {
            _mappingExtensionsStagingValue = ModRequirementFilterOption.Off;
            _noodleExtensionsStagingValue = ModRequirementFilterOption.Off;
            _chromaStagingValue = ModRequirementFilterOption.Off;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _mappingExtensionsStagingValue = _mappingExtensionsAppliedValue;
            _noodleExtensionsStagingValue = _noodleExtensionsAppliedValue;
            _chromaStagingValue = _chromaAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            _mappingExtensionsAppliedValue = _mappingExtensionsStagingValue;
            _noodleExtensionsAppliedValue = _noodleExtensionsStagingValue;
            _chromaAppliedValue = _chromaStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _mappingExtensionsAppliedValue = ModRequirementFilterOption.Off;
            _noodleExtensionsAppliedValue = ModRequirementFilterOption.Off;
            _chromaAppliedValue = ModRequirementFilterOption.Off;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            bool mappingExtensionsApplied = _mappingExtensionsAppliedValue != ModRequirementFilterOption.Off;
            bool noodleExtensionsApplied = _noodleExtensionsAppliedValue != ModRequirementFilterOption.Off;
            bool chromaApplied = _chromaAppliedValue != ModRequirementFilterOption.Off;

            var levelsToRemove = detailsList.AsParallel().Where(delegate (BeatmapDetails details)
            {
                if (details.IsOST)
                    return true;

                ExtraSongData songData = Collections.RetrieveExtraSongData(BeatmapDetailsLoader.GetCustomLevelHash(details));
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

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "mappingExtensions", _mappingExtensionsAppliedValue,
                "noodleExtensions", _noodleExtensionsAppliedValue,
                "chroma", _chromaAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
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

            RefreshValues();
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
