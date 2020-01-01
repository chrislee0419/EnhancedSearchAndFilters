using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class DifficultyFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return "Difficulty"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status {
            get
            {
                if (_easyStagingValue != _easyAppliedValue ||
                    _normalStagingValue != _normalAppliedValue ||
                    _hardStagingValue != _hardAppliedValue ||
                    _expertStagingValue != _expertAppliedValue ||
                    _expertPlusStagingValue != _expertPlusAppliedValue)
                    return IsFilterApplied ? FilterStatus.AppliedAndChanged : FilterStatus.NotAppliedAndChanged;
                else
                    return IsFilterApplied ? FilterStatus.Applied : FilterStatus.NotAppliedAndDefault;
            }
        }
        public bool IsFilterApplied => _easyAppliedValue || _normalAppliedValue || _hardAppliedValue || _expertAppliedValue || _expertPlusAppliedValue;

        [UIObject("root")]
        public GameObject ViewGameObject { get; private set; }

        [UIValue("easy-checkbox-value")]
        private bool _easyStagingValue { get; set; } = false;
        [UIValue("normal-checkbox-value")]
        private bool _normalStagingValue { get; set; } = false;
        [UIValue("hard-checkbox-value")]
        private bool _hardStagingValue { get; set; } = false;
        [UIValue("expert-checkbox-value")]
        private bool _expertStagingValue { get; set; } = false;
        [UIValue("expert-plus-checkbox-value")]
        private bool _expertPlusStagingValue { get; set; } = false;

        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        private bool _isInitialized = false;

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.DifficultyFilterView.bsml"), viewContainer, this);
            ViewGameObject.name = "DifficultyFilterViewContainer";

            _isInitialized = true;
        }

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _easyStagingValue = false;
            _normalStagingValue = false;
            _hardStagingValue = false;
            _expertStagingValue = false;
            _expertPlusStagingValue = false;

            NotifyAllPropertiesChanged();
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

            NotifyAllPropertiesChanged();
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized)
                return;

            _easyAppliedValue = _easyStagingValue;
            _normalAppliedValue = _normalStagingValue;
            _hardAppliedValue = _hardStagingValue;
            _expertAppliedValue = _expertStagingValue;
            _expertPlusAppliedValue = _expertPlusStagingValue;

            NotifyAllPropertiesChanged();
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized)
                return;

            _easyAppliedValue = false;
            _normalAppliedValue = false;
            _hardAppliedValue = false;
            _expertAppliedValue = false;
            _expertPlusAppliedValue = false;

            NotifyAllPropertiesChanged();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsFilterApplied)
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                bool remove = true;
                foreach (var difficultySet in detailsList[i].DifficultyBeatmapSets)
                {
                    var difficulties = difficultySet.DifficultyBeatmaps.Select(x => (x.Difficulty, x.NotesCount != 0)).ToArray();

                    if ((!_easyAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)) &&
                        (!_normalAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Normal && x.Item2)) &&
                        (!_hardAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)) &&
                        (!_easyAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)) &&
                        (!_easyAppliedValue || difficulties.Any(x => x.Difficulty == BeatmapDifficulty.Easy && x.Item2)))
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

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("setting-changed")]
        private void OnSettingChanged(bool value) => SettingChanged?.Invoke();

        private void NotifyAllPropertiesChanged()
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_easyStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_normalStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_hardStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_expertStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_expertPlusStagingValue)));
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error Invoking PropertyChanged: {ex.Message}");
                Logger.log.Error(ex);
            }
        }
    }
}
