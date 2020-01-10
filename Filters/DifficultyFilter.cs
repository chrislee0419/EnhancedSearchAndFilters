using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.Filters
{
    internal class DifficultyFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Difficulty"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status {
            get
            {
                if (HasChanges)
                    return IsFilterApplied ? FilterStatus.AppliedAndChanged : FilterStatus.NotAppliedAndChanged;
                else
                    return IsFilterApplied ? FilterStatus.Applied : FilterStatus.NotApplied;
            }
        }
        public bool IsFilterApplied => _easyAppliedValue || _normalAppliedValue || _hardAppliedValue || _expertAppliedValue || _expertPlusAppliedValue;
        public bool HasChanges => _easyStagingValue != _easyAppliedValue ||
            _normalStagingValue != _normalAppliedValue ||
            _hardStagingValue != _hardAppliedValue ||
            _expertStagingValue != _expertAppliedValue ||
            _expertPlusStagingValue != _expertPlusAppliedValue;
        public bool IsStagingDefaultValues => _easyStagingValue == false &&
            _normalStagingValue == false &&
            _hardStagingValue == false &&
            _expertStagingValue == false &&
            _expertPlusStagingValue == false;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;
#pragma warning restore CS0649

        private bool _easyStagingValue = false;
        [UIValue("easy-checkbox-value")]
        public bool EasyStagingValue
        {
            get => _easyStagingValue;
            set
            {
                _easyStagingValue = value;
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
            }
        }

        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        private BSMLParserParams _parserParams;

        public void Init(GameObject viewContainer)
        {
            if (_viewGameObject != null)
                return;

            _parserParams = BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.DifficultyFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "DifficultyFilterViewContainer";
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
            _easyStagingValue = false;
            _normalStagingValue = false;
            _hardStagingValue = false;
            _expertStagingValue = false;
            _expertPlusStagingValue = false;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            _easyAppliedValue = _easyStagingValue;
            _normalAppliedValue = _normalStagingValue;
            _hardAppliedValue = _hardStagingValue;
            _expertAppliedValue = _expertStagingValue;
            _expertPlusAppliedValue = _expertPlusStagingValue;
        }

        public void ApplyDefaultValues()
        {
            _easyAppliedValue = false;
            _normalAppliedValue = false;
            _hardAppliedValue = false;
            _expertAppliedValue = false;
            _expertPlusAppliedValue = false;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
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

        public List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "easy", _easyAppliedValue,
                "normal", _normalAppliedValue,
                "hard", _hardAppliedValue,
                "expert", _expertAppliedValue,
                "expertPlus", _expertPlusAppliedValue);
        }

        public void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
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

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }
    }
}
