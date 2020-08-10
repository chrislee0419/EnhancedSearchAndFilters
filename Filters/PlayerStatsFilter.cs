using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class PlayerStatsFilter : FilterBase
    {
        public override string Name => "Player Stats";
        public override bool IsFilterApplied => HasCompletedAppliedValue != SongCompletedFilterOption.Off || HasFullComboAppliedValue != SongFullComboFilterOption.Off || RankAppliedValue != null;
        public override bool HasChanges => _hasCompletedStagingValue != HasCompletedAppliedValue ||
            _hasFullComboStagingValue != HasFullComboAppliedValue ||
            _rankStagingValue != RankAppliedValue;
        public override bool IsStagingDefaultValues => _hasCompletedStagingValue == SongCompletedFilterOption.Off &&
            _hasFullComboStagingValue == SongFullComboFilterOption.Off &&
            _rankStagingValue == null;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.PlayerStatsFilterView.bsml";
        protected override string ContainerGameObjectName => "PlayerStatsFilterViewContainer";

        private SongCompletedFilterOption _hasCompletedStagingValue = SongCompletedFilterOption.Off;
        [UIValue("completed-value")]
        public SongCompletedFilterOption HasCompletedStagingValue
        {
            get => _hasCompletedStagingValue;
            set
            {
                _hasCompletedStagingValue = value;
                InvokeSettingChanged();
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
                InvokeSettingChanged();
            }
        }
        private RankModel.Rank? _rankStagingValue = null;
        [UIValue("rank-value")]
        public RankModel.Rank? RankStagingValue
        {
            get => _rankStagingValue;
            set
            {
                _rankStagingValue = value;
                InvokeSettingChanged();
            }
        }

        public SongCompletedFilterOption HasCompletedAppliedValue { get; private set; } = SongCompletedFilterOption.Off;
        public SongFullComboFilterOption HasFullComboAppliedValue { get; private set; } = SongFullComboFilterOption.Off;
        public RankModel.Rank? RankAppliedValue { get; private set; } = null;

        [UIValue("completed-options")]
        private static readonly List<object> SongCompletedOptions = Enum.GetValues(typeof(SongCompletedFilterOption)).Cast<SongCompletedFilterOption>().Select(x => (object)x).ToList();
        [UIValue("full-combo-options")]
        private static readonly List<object> FullComboOptions = Enum.GetValues(typeof(SongFullComboFilterOption)).Cast<SongFullComboFilterOption>().Select(x => (object)x).ToList();
        [UIValue("rank-options")]
        private static readonly List<object> RankOptions = Enum.GetValues(typeof(RankModel.Rank)).Cast<RankModel.Rank>().Select(x => x == RankModel.Rank.SSS ? null : (object)x).Reverse().ToList();

        private const string RankFilterOffString = "Off";

        public override void SetDefaultValuesToStaging()
        {
            _hasCompletedStagingValue = SongCompletedFilterOption.Off;
            _hasFullComboStagingValue = SongFullComboFilterOption.Off;
            _rankStagingValue = null;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _hasCompletedStagingValue = HasCompletedAppliedValue;
            _hasFullComboStagingValue = HasFullComboAppliedValue;
            _rankStagingValue = RankAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            HasCompletedAppliedValue = _hasCompletedStagingValue;
            HasFullComboAppliedValue = _hasFullComboStagingValue;
            RankAppliedValue = _rankStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            HasCompletedAppliedValue = SongCompletedFilterOption.Off;
            HasFullComboAppliedValue = SongFullComboFilterOption.Off;
            RankAppliedValue = null;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            DifficultyFilter diffFilter = FilterList.ActiveFilters.Where(x => x.Name == DifficultyFilter.FilterName && x.IsFilterApplied).FirstOrDefault() as DifficultyFilter;

            List<BeatmapDifficulty> diffList;
            if (diffFilter != null)
            {
                diffList = new List<BeatmapDifficulty>(5);
                if (diffFilter.EasyAppliedValue)
                    diffList.Add(BeatmapDifficulty.Easy);
                if (diffFilter.NormalAppliedValue)
                    diffList.Add(BeatmapDifficulty.Normal);
                if (diffFilter.HardAppliedValue)
                    diffList.Add(BeatmapDifficulty.Hard);
                if (diffFilter.ExpertAppliedValue)
                    diffList.Add(BeatmapDifficulty.Expert);
                if (diffFilter.ExpertPlusAppliedValue)
                    diffList.Add(BeatmapDifficulty.ExpertPlus);
            }
            else
            {
                diffList = new List<BeatmapDifficulty>(5)
                {
                    BeatmapDifficulty.Easy,
                    BeatmapDifficulty.Normal,
                    BeatmapDifficulty.Hard,
                    BeatmapDifficulty.Expert,
                    BeatmapDifficulty.ExpertPlus,
                };
            }

            const string StandardCharacteristicName = "Standard";
            const string LightmapCharacteristicName = "Lightmap";
            List<string> characteristics = null;
            CharacteristicsFilter charFilter = FilterList.ActiveFilters.Where(x => x.Name == CharacteristicsFilter.FilterName && x.IsFilterApplied).FirstOrDefault() as CharacteristicsFilter;
            if (charFilter != null)
            {
                if (charFilter.LightshowAppliedValue && !charFilter.OneSaberAppliedValue && !charFilter.NoArrowsAppliedValue && !charFilter.Mode90AppliedValue && !charFilter.Mode360AppliedValue)
                {
                    characteristics = new List<string>
                    {
                        StandardCharacteristicName,
                        LightmapCharacteristicName,
                        CharacteristicsFilter.OneSaberSerializedCharacteristicName,
                        CharacteristicsFilter.NoArrowsSerializedCharacteristicName,
                        CharacteristicsFilter.Mode90DegreeSerializedCharacteristicName,
                        CharacteristicsFilter.Mode360DegreeSerializedCharacteristicName,
                    };
                }
                else
                {
                    characteristics = new List<string>();
                    if (charFilter.OneSaberAppliedValue)
                        characteristics.Add(CharacteristicsFilter.OneSaberSerializedCharacteristicName);
                    if (charFilter.NoArrowsAppliedValue)
                        characteristics.Add(CharacteristicsFilter.NoArrowsSerializedCharacteristicName);
                    if (charFilter.Mode90AppliedValue)
                        characteristics.Add(CharacteristicsFilter.Mode90DegreeSerializedCharacteristicName);
                    if (charFilter.Mode360AppliedValue)
                        characteristics.Add(CharacteristicsFilter.Mode360DegreeSerializedCharacteristicName);
                }
            }

            // touch the helper singletons to make sure they're instantiated before the parallel query
            // also, might as well do a check to see if they were able to be instantiated
            if (PlayerDataHelper.Instance == null && LocalLeaderboardDataHelper.Instance == null)
            {
                Logger.log.Warn("Both PlayerDataHelper and LocalLeaderboardDataHelper objects could not be instantiated (unable to apply player stats filter)");
                return;
            }

            var levelsToRemove = detailsList.AsParallel().Where(delegate (BeatmapDetails details)
            {
                bool remove = false;

                // NOTE: if any difficulties are selected, this filter also has the same behaviour as the difficulty filter
                if (diffList.Count > 0)
                {
                    bool hasDifficultiesToCheck = details.DifficultyBeatmapSets.Any(set => set.DifficultyBeatmaps.Any(diff => diffList.Contains(diff.Difficulty)));
                    remove |= !hasDifficultiesToCheck;
                }
                if (HasCompletedAppliedValue != SongCompletedFilterOption.Off && !remove)
                {
                    // if PlayerData and LocalLeaderboardModel objects cannot be found, assume level has not been completed
                    bool hasBeenCompleted = PlayerDataHelper.Instance?.HasCompletedLevel(details.LevelID, null, diffList) ?? false;
                    hasBeenCompleted |= LocalLeaderboardDataHelper.Instance?.HasCompletedLevel(details.LevelID, diffList) ?? false;

                    remove = hasBeenCompleted != (HasCompletedAppliedValue == SongCompletedFilterOption.HasCompleted);
                }
                if (HasFullComboAppliedValue != SongFullComboFilterOption.Off && !remove)
                {
                    // if PlayerData and LocalLeaderboardModel objects cannot be found, assume level has not been full combo'd
                    bool hasFullCombo = PlayerDataHelper.Instance?.HasFullComboForLevel(details.LevelID, null, diffList) ?? false;
                    hasFullCombo |= LocalLeaderboardDataHelper.Instance?.HasFullComboForLevel(details.LevelID, diffList) ?? false;

                    remove = hasFullCombo != (HasFullComboAppliedValue == SongFullComboFilterOption.HasFullCombo);
                }
                if (RankAppliedValue.HasValue && !remove)
                {
                    RankModel.Rank? soloHighestRank = PlayerDataHelper.Instance?.GetHighestRankForLevel(details.LevelID, diffList, characteristics);
                    RankModel.Rank? localHighestRank = LocalLeaderboardDataHelper.Instance?.GetHighestRankForLevel(details, diffList, characteristics);

                    int highestRankValue = -1;
                    if (soloHighestRank != null)
                        highestRankValue = (int)soloHighestRank.Value;
                    if (localHighestRank != null)
                        highestRankValue = Math.Max(highestRankValue, (int)localHighestRank.Value);

                    remove = (int)RankAppliedValue.Value < highestRankValue;
                }

                return remove;
            }).ToList();

            foreach (var level in levelsToRemove)
                detailsList.Remove(level);
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "completed", HasCompletedAppliedValue,
                "fullCombo", HasFullComboAppliedValue,
                "rank", RankFormatter(RankAppliedValue));
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (pair.Key == "completed" && Enum.TryParse(pair.Value, out SongCompletedFilterOption completedValue))
                {
                    _hasCompletedStagingValue = completedValue;
                }
                else if (pair.Key == "fullCombo" && Enum.TryParse(pair.Value, out SongFullComboFilterOption fcValue))
                {
                    _hasFullComboStagingValue = fcValue;
                }
                else if (pair.Key == "rank")
                {
                    if (Enum.TryParse(pair.Value, out RankModel.Rank rankValue))
                        _rankStagingValue = rankValue;
                    else if (pair.Value == RankFilterOffString)
                        _rankStagingValue = null;
                }
            }

            RefreshValues();
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
                    return "<size=62%>Keep Never Completed</size>";
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

        [UIAction("rank-formatter")]
        private string RankFormatter(object value)
        {
            RankModel.Rank? rank = (RankModel.Rank?)value;
            if (rank == null)
                return RankFilterOffString;
            else
                return rank.ToString();
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
