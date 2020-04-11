using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Tweaks;

namespace EnhancedSearchAndFilters.Filters
{
    internal class VotedFilter : FilterBase
    {
        public override string Name => "Voted Songs";
        [UIValue("is-available")]
        public override bool IsAvailable => BeatSaverVotingTweaks.ModLoaded;
        public override bool IsFilterApplied => _upvotedAppliedValue || _noVoteAppliedValue || _downvotedAppliedValue;
        public override bool HasChanges => _upvotedStagingValue != _upvotedAppliedValue ||
            _noVoteStagingValue != _noVoteAppliedValue ||
            _downvotedStagingValue != _downvotedAppliedValue;
        public override bool IsStagingDefaultValues => _upvotedStagingValue == false ||
            _noVoteStagingValue == false ||
            _downvotedStagingValue == false;

        protected override string ViewResource => "EnhancedSearchAndFilters.UI.Views.Filters.VotedFilterView.bsml";
        protected override string ContainerGameObjectName => "VotedFilterViewContainer";

        private bool _upvotedStagingValue = false;
        [UIValue("upvoted-checkbox-value")]
        public bool UpvotedStagingValue
        {
            get => _upvotedStagingValue;
            set
            {
                _upvotedStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _noVoteStagingValue = false;
        [UIValue("no-vote-checkbox-value")]
        public bool NoVoteStagingValue
        {
            get => _noVoteStagingValue;
            set
            {
                _noVoteStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _downvotedStagingValue = false;
        [UIValue("downvoted-checkbox-value")]
        public bool DownvotedStagingValue
        {
            get => _downvotedStagingValue;
            set
            {
                _downvotedStagingValue = value;
                InvokeSettingChanged();
            }
        }

        private bool _upvotedAppliedValue = false;
        private bool _noVoteAppliedValue = false;
        private bool _downvotedAppliedValue = false;

        [UIValue("missing-requirements-text")]
        private const string MissingRequirementsText = "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the BeatSaverVoting mod to be\n installed.</size></color>";

        public override void SetDefaultValuesToStaging()
        {
            _upvotedStagingValue = false;
            _noVoteStagingValue = false;
            _downvotedStagingValue = false;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _upvotedStagingValue = _upvotedAppliedValue;
            _noVoteStagingValue = _noVoteAppliedValue;
            _downvotedStagingValue = _downvotedAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            _upvotedAppliedValue = _upvotedStagingValue;
            _noVoteAppliedValue = _noVoteStagingValue;
            _downvotedAppliedValue = _downvotedStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _upvotedAppliedValue = false;
            _noVoteAppliedValue = false;
            _downvotedAppliedValue = false;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied)
                return;

            BeatSaverVotingTweaks.ReadVotedSongsData();
            for (int i = 0; i < detailsList.Count;)
            {
                BeatSaverVotingTweaks.VoteStatus vote = BeatSaverVotingTweaks.GetVoteStatus(detailsList[i]);
                if ((vote == BeatSaverVotingTweaks.VoteStatus.Upvoted && !_upvotedAppliedValue) ||
                    (vote == BeatSaverVotingTweaks.VoteStatus.NoVote && !_noVoteAppliedValue) ||
                    (vote == BeatSaverVotingTweaks.VoteStatus.Downvoted && !_downvotedAppliedValue))
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
            BeatSaverVotingTweaks.Cleanup();
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "upvoted", _upvotedAppliedValue,
                "noVote", _noVoteAppliedValue,
                "downvoted", _downvotedAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (!bool.TryParse(pair.Value, out bool value))
                    continue;

                switch (pair.Key)
                {
                    case "upvoted":
                        _upvotedStagingValue = value;
                        break;
                    case "noVote":
                        _noVoteStagingValue = value;
                        break;
                    case "downvoted":
                        _downvotedStagingValue = value;
                        break;
                }
            }

            RefreshValues();
        }
    }
}
