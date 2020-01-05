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
    class PlayerStatsFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name { get { return "Player Stats"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                {
                    if (_hasCompletedStagingValue != _hasCompletedAppliedValue ||
                        _hasFullComboStagingValue != _hasFullComboAppliedValue ||
                        _easyStagingValue != _easyAppliedValue ||
                        _normalStagingValue != _normalAppliedValue ||
                        _hardStagingValue != _hardAppliedValue ||
                        _expertStagingValue != _expertAppliedValue ||
                        _expertPlusStagingValue != _expertPlusAppliedValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_hasCompletedStagingValue != SongCompletedFilterOption.Off ||
                    _hasFullComboStagingValue != SongFullComboFilterOption.Off)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool IsFilterApplied => _hasCompletedAppliedValue != SongCompletedFilterOption.Off || _hasFullComboAppliedValue != SongFullComboFilterOption.Off;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;
#pragma warning restore CS0649

        private SongCompletedFilterOption _hasCompletedStagingValue = SongCompletedFilterOption.Off;
        [UIValue("completed-value")]
        public SongCompletedFilterOption HasCompletedStagingValue
        {
            get => _hasCompletedStagingValue;
            set
            {
                _hasCompletedStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private SongFullComboFilterOption _hasFullComboStagingValue = SongFullComboFilterOption.Off;
        [UIValue("full-combo-value")]
        public SongFullComboFilterOption HasFullComboStagingValue
        {
            get => _hasFullComboStagingValue;
            set
            {
                _hasFullComboStagingValue = value;
                SettingChanged?.Invoke();
            }
        }
        private bool _easyStagingValue = false;
        [UIValue("easy-value")]
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
        [UIValue("normal-value")]
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
        [UIValue("hard-value")]
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
        [UIValue("expert-value")]
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
        [UIValue("expert-plus-value")]
        public bool ExpertPlusStagingValue
        {
            get => _expertPlusStagingValue;
            set
            {
                _expertPlusStagingValue = value;
                SettingChanged?.Invoke();
            }
        }

        private SongCompletedFilterOption _hasCompletedAppliedValue = SongCompletedFilterOption.Off;
        private SongFullComboFilterOption _hasFullComboAppliedValue = SongFullComboFilterOption.Off;
        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        private bool _isInitialized = false;
        private BSMLParserParams _parserParams;

        [UIValue("completed-options")]
        private static readonly List<object> SongCompletedOptions = Enum.GetValues(typeof(SongCompletedFilterOption)).Cast<SongCompletedFilterOption>().Select(x => (object)x).ToList();
        [UIValue("full-combo-options")]
        private static readonly List<object> FullComboOptions = Enum.GetValues(typeof(SongFullComboFilterOption)).Cast<SongFullComboFilterOption>().Select(x => (object)x).ToList();

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            _parserParams = BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.PlayerStatsFilterView.bsml"), viewContainer, this);
            _viewGameObject.name = "PlayerStatsFilterViewContainer";

            _isInitialized = true;
        }

        public GameObject GetView() => _viewGameObject;

        public void SetDefaultValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _hasCompletedStagingValue = SongCompletedFilterOption.Off;
            _hasFullComboStagingValue = SongFullComboFilterOption.Off;
            _easyStagingValue = false;
            _normalStagingValue = false;
            _hardStagingValue = false;
            _expertStagingValue = false;
            _expertPlusStagingValue = false;

            _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            if (!_isInitialized)
                return;

            _hasCompletedStagingValue = _hasCompletedAppliedValue;
            _hasFullComboStagingValue = _hasFullComboAppliedValue;
            _easyStagingValue = _easyAppliedValue;
            _normalStagingValue = _normalAppliedValue;
            _hardStagingValue = _hardAppliedValue;
            _expertStagingValue = _expertAppliedValue;
            _expertPlusStagingValue = _expertPlusAppliedValue;

            _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            if (!_isInitialized)
                return;

            _hasCompletedAppliedValue = _hasCompletedStagingValue;
            _hasFullComboAppliedValue = _hasFullComboStagingValue;
            _easyAppliedValue = _easyStagingValue;
            _normalAppliedValue = _normalStagingValue;
            _hardAppliedValue = _normalStagingValue;
            _expertAppliedValue = _expertStagingValue;
            _expertPlusAppliedValue = _expertPlusStagingValue;
        }

        public void ApplyDefaultValues()
        {
            if (!_isInitialized)
                return;

            _hasCompletedAppliedValue = SongCompletedFilterOption.Off;
            _hasFullComboAppliedValue = SongFullComboFilterOption.Off;
            _easyAppliedValue = false;
            _normalAppliedValue = false;
            _hardAppliedValue = false;
            _expertAppliedValue = false;
            _expertPlusAppliedValue = false;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !IsFilterApplied)
                return;

            List<BeatmapDifficulty> diffList = new List<BeatmapDifficulty>(5);
            if (_easyAppliedValue)
                diffList.Add(BeatmapDifficulty.Easy);
            if (_normalAppliedValue)
                diffList.Add(BeatmapDifficulty.Normal);
            if (_hardAppliedValue)
                diffList.Add(BeatmapDifficulty.Hard);
            if (_expertAppliedValue)
                diffList.Add(BeatmapDifficulty.Expert);
            if (_expertPlusAppliedValue)
                diffList.Add(BeatmapDifficulty.ExpertPlus);

            var levelsToRemove = detailsList.AsParallel().AsOrdered().Where(delegate (BeatmapDetails details)
            {
                bool remove = false;

                // NOTE: if any difficulties are selected, this filter also has the same behaviour as the difficulty filter
                if (diffList.Count > 0)
                {
                    bool hasDifficultiesToCheck = details.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diffList.Contains(diff.Difficulty)));
                    remove |= !hasDifficultiesToCheck;
                }
                if (_hasCompletedAppliedValue != SongCompletedFilterOption.Off && !remove)
                {
                    // if PlayerData object cannot be found, assume level has not been completed
                    bool hasBeenCompleted = PlayerDataHelper.Instance?.HasCompletedLevel(details.LevelID, null, diffList) ?? false;
                    remove |= hasBeenCompleted != (_hasCompletedAppliedValue == SongCompletedFilterOption.HasCompleted);
                }
                if (_hasFullComboAppliedValue != SongFullComboFilterOption.Off && !remove)
                {
                    // if PlayerData object cannot be found, assume level has not been full combo'd
                    bool hasFullCombo = PlayerDataHelper.Instance?.HasFullComboForLevel(details.LevelID, null, diffList) ?? false;
                    remove |= hasFullCombo != (_hasFullComboAppliedValue == SongFullComboFilterOption.HasFullCombo);
                }

                return remove;
            }).ToArray();

            foreach (var level in levelsToRemove)
                detailsList.Remove(level);
        }

        public string SerializeFromStaging()
        {
            throw new NotImplementedException();
        }

        public void DeserializeToStaging(string serializedSettings)
        {
            throw new NotImplementedException();
        }

        [UIAction("completed-formatter")]
        private string SongCompletedFormatter(object value)
        {
            switch ((SongCompletedFilterOption)value)
            {
                case SongCompletedFilterOption.Off:
                    return "Off";
                case SongCompletedFilterOption.HasCompleted:
                    return "<size=62%>Keep Completed</size>";
                case SongCompletedFilterOption.HasNeverCompleted:
                    return "<size=62%>Has Never Completed</size>";
                default:
                    return "ERROR!";
            }
        }

        [UIAction("full-combo-formatter")]
        private string SongFullComboFormatter(object value)
        {
            switch ((SongFullComboFilterOption)value)
            {
                case SongFullComboFilterOption.Off:
                    return "Off";
                case SongFullComboFilterOption.HasFullCombo:
                    return "<size=75%>Keep Songs With FC</size>";
                case SongFullComboFilterOption.HasNoFullCombo:
                    return "<size=75%>Keep Songs Without FC</size>";
                default:
                    return "ERROR!";
            }
        }
    }

    internal enum SongCompletedFilterOption
    {
        Off = 0,
        HasNeverCompleted = 1,
        HasCompleted = 2
    }

    internal enum SongFullComboFilterOption
    {
        Off = 0,
        HasNoFullCombo = 1,
        HasFullCombo = 2
    }
}
