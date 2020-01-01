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
    class PlayerStatsFilter : IFilter, INotifiableHost
    {
        public event Action SettingChanged;
        public event PropertyChangedEventHandler PropertyChanged;

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

        [UIObject("root")]
        public GameObject ViewGameObject { get; private set; }

        [UIValue("completed-value")]
        private SongCompletedFilterOption _hasCompletedStagingValue = SongCompletedFilterOption.Off;
        [UIValue("full-combo-value")]
        private SongFullComboFilterOption _hasFullComboStagingValue = SongFullComboFilterOption.Off;
        [UIValue("easy-value")]
        private bool _easyStagingValue = false;
        [UIValue("normal-value")]
        private bool _normalStagingValue = false;
        [UIValue("hard-value")]
        private bool _hardStagingValue = false;
        [UIValue("expert-value")]
        private bool _expertStagingValue = false;
        [UIValue("expert-plus-value")]
        private bool _expertPlusStagingValue = false;

        private SongCompletedFilterOption _hasCompletedAppliedValue = SongCompletedFilterOption.Off;
        private SongFullComboFilterOption _hasFullComboAppliedValue = SongFullComboFilterOption.Off;
        private bool _easyAppliedValue = false;
        private bool _normalAppliedValue = false;
        private bool _hardAppliedValue = false;
        private bool _expertAppliedValue = false;
        private bool _expertPlusAppliedValue = false;

        private bool _isInitialized = false;

        [UIValue("completed-options")]
        private static readonly List<object> SongCompletedOptions = Enum.GetValues(typeof(SongCompletedFilterOption)).Cast<SongCompletedFilterOption>().Select(x => (object)x).ToList();
        [UIValue("full-combo-options")]
        private static readonly List<object> FullComboOptions = Enum.GetValues(typeof(SongFullComboFilterOption)).Cast<SongFullComboFilterOption>().Select(x => (object)x).ToList();

        public void Init(GameObject viewContainer)
        {
            if (_isInitialized)
                return;

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.PlayerStatsView.bsml"), viewContainer, this);
            ViewGameObject.name = "PlayerStatsFilterViewContainer";

            _isInitialized = true;
        }

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

            NotifyAllPropertiesChanged();
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

            NotifyAllPropertiesChanged();
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

        [UIAction("difficulty-changed")]
        private void OnDifficultyChanged(bool value) => SettingChanged?.Invoke();

        [UIAction("list-setting-changed")]
        private void OnListSettingChanged(object value) => SettingChanged?.Invoke();

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

        private void NotifyAllPropertiesChanged()
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_hasCompletedStagingValue)));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(_hasFullComboStagingValue)));
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
