using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SongCore;
using SongCore.Data;
using SongCore.Utilities;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    class OtherFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return "Other"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                {
                    if (_oneSaberAppliedValue != _oneSaberStagingValue ||
                        _noArrowsAppliedValue != _noArrowsStagingValue ||
                        _90AppliedValue != _90StagingValue ||
                        _360AppliedValue != _360StagingValue ||
                        _lightshowAppliedValue != _lightshowStagingValue ||
                        _mappingExtensionsAppliedValue != _mappingExtensionsStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_oneSaberStagingValue ||
                         _noArrowsStagingValue ||
                         _90StagingValue ||
                         _360StagingValue ||
                         _lightshowStagingValue ||
                         _mappingExtensionsStagingValue != SongRequirementFilterOption.Off)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool IsFilterApplied => _oneSaberAppliedValue ||_noArrowsAppliedValue || _90AppliedValue || _360AppliedValue ||
            _lightshowAppliedValue || _mappingExtensionsAppliedValue != SongRequirementFilterOption.Off;

        [UIObject("root")]
        public GameObject ViewGameObject { get; private set; }

        [UIValue("one-saber-value")]
        private bool _oneSaberStagingValue = false;
        [UIValue("no-arrows-value")]
        private bool _noArrowsStagingValue = false;
        [UIValue("90-value")]
        private bool _90StagingValue = false;
        [UIValue("360-value")]
        private bool _360StagingValue = false;
        [UIValue("lightshow-value")]
        private bool _lightshowStagingValue = false;
        [UIValue("mapping-extensions-value")]
        private SongRequirementFilterOption _mappingExtensionsStagingValue = SongRequirementFilterOption.Off;

        private bool _oneSaberAppliedValue = false;
        private bool _noArrowsAppliedValue = false;
        private bool _90AppliedValue = false;
        private bool _360AppliedValue = false;
        private bool _lightshowAppliedValue = false;
        private SongRequirementFilterOption _mappingExtensionsAppliedValue = SongRequirementFilterOption.Off;

        private bool _isInitialized = false;

        [UIValue("mapping-extensions-options")]
        private static readonly List<object> MappingExtensionsOptions = Enum.GetValues(typeof(SongRequirementFilterOption)).Cast<SongRequirementFilterOption>().Select(x => (object)x).ToList();

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.OtherFilterView.bsml"), viewContainer, this);
            ViewGameObject.name = "OtherFilterViewContainer";
            _isInitialized = true;
        }

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = false;
            _noArrowsStagingValue = false;
            _90StagingValue = false;
            _360StagingValue = false;
            _lightshowStagingValue = false;
            _mappingExtensionsStagingValue = SongRequirementFilterOption.Off;

            NotifyAllPropertiesChanged();
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = _oneSaberAppliedValue;
            _noArrowsStagingValue = _noArrowsAppliedValue;
            _90StagingValue = _90AppliedValue;
            _360StagingValue = _360AppliedValue;
            _lightshowStagingValue = _lightshowAppliedValue;
            _mappingExtensionsStagingValue = _mappingExtensionsAppliedValue;

            NotifyAllPropertiesChanged();
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberAppliedValue = _oneSaberStagingValue;
            _noArrowsAppliedValue = _noArrowsStagingValue;
            _90AppliedValue = _90StagingValue;
            _360AppliedValue = _360StagingValue;
            _lightshowAppliedValue = _lightshowStagingValue;
            _mappingExtensionsAppliedValue = _mappingExtensionsStagingValue;
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberAppliedValue = false;
            _noArrowsAppliedValue = false;
            _90AppliedValue = false;
            _360AppliedValue = false;
            _lightshowAppliedValue = false;
            _mappingExtensionsAppliedValue = SongRequirementFilterOption.Off;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsFilterApplied)
                return;

            List<CustomPreviewBeatmapLevel> customLevels = null;
            if (_mappingExtensionsAppliedValue != SongRequirementFilterOption.Off)
                customLevels = Loader.CustomLevels.Values.ToList();

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
                else if (_90AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == ""))
                {
                    // TODO: find out what the 90 degrees difficulty beatmap set is called
                }
                else if (_360AppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == ""))
                {
                    // TODO: find out what the 360 degrees difficulty beatmap set is called
                }
                else if (_mappingExtensionsAppliedValue != SongRequirementFilterOption.Off && !beatmap.IsOST)
                {
                    // remove songs that somehow aren't OST, but also aren't custom levels handled by SongCore
                    CustomPreviewBeatmapLevel customLevel = customLevels.FirstOrDefault(x => x.levelID == beatmap.LevelID);
                    if (customLevel == null)
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    ExtraSongData songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                    if (songData == null)
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    bool required = songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements?.Any(y => y == "Mapping Extensions") == true) == true;
                    if ((_mappingExtensionsAppliedValue == SongRequirementFilterOption.Required && !required) ||
                        (_mappingExtensionsAppliedValue == SongRequirementFilterOption.NotRequired && required))
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    // passes check, requires mapping extensions
                    ++i;
                }
                else
                {
                    ++i;
                }
            }
        }

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("mode-changed")]
        private void OnModeChanged(bool value) => SettingChanged?.Invoke();

        [UIAction("mapping-extensions-changed")]
        private void OnMappingExtensionsChanged(object value) => SettingChanged?.Invoke();

        [UIAction("mapping-extensions-formatter")]
        private string MappingExtensionsFormatter(object value)
        {
            switch ((SongRequirementFilterOption)value)
            {
                case SongRequirementFilterOption.Off:
                    return "Off";
                case SongRequirementFilterOption.NotRequired:
                    return "Not Required";
                case SongRequirementFilterOption.Required:
                    return "Required";
                default:
                    return "ERROR!";
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_oneSaberStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_noArrowsStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_90StagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_360StagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_lightshowStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_mappingExtensionsStagingValue)));
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error Invoking PropertyChanged: {ex.Message}");
                Logger.log.Error(ex);
            }
        }
    }

    internal enum SongRequirementFilterOption
    {
        Off = 0,
        Required = 1,
        NotRequired = 2
    }
}
