using System;
using System.Collections.Generic;
using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Tweaks;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class VotedFilter : IFilter
    {
        public event Action SettingChanged;

        public string Name => "Voted Songs";
        [UIValue("is-available")]
        public bool IsAvailable => BeatSaverVotingTweaks.ModLoaded;
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
        public bool IsFilterApplied => _upvotedAppliedValue || _noVoteAppliedValue || _downvotedAppliedValue;
        public bool HasChanges => _upvotedStagingValue != _upvotedAppliedValue ||
            _noVoteStagingValue != _noVoteAppliedValue ||
            _downvotedStagingValue != _downvotedAppliedValue;
        public bool IsStagingDefaultValues => _upvotedStagingValue == false ||
            _noVoteStagingValue == false ||
            _downvotedStagingValue == false;

#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _viewGameObject;
#pragma warning restore CS0649

        private bool _upvotedStagingValue = false;
        [UIValue("upvoted-checkbox-value")]
        public bool UpvotedStagingValue
        {
            get => _upvotedStagingValue;
            set
            {
                _upvotedStagingValue = value;
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
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
                SettingChanged?.Invoke();
            }
        }

        private bool _upvotedAppliedValue = false;
        private bool _noVoteAppliedValue = false;
        private bool _downvotedAppliedValue = false;

        private BSMLParserParams _parserParams;

        [UIValue("missing-requirements-text")]
        private const string MissingRequirementsText = "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the BeatSaverVoting mod to be\n installed.</size></color>";

        public void Init(GameObject viewContainer)
        {
            if (_viewGameObject != null)
                return;

            _parserParams = UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.Filters.VotedFilterView.bsml", viewContainer, this);
            _viewGameObject.name = "VotedFilterViewContainer";
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
            _upvotedStagingValue = false;
            _noVoteStagingValue = false;
            _downvotedStagingValue = false;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void SetAppliedValuesToStaging()
        {
            _upvotedStagingValue = _upvotedAppliedValue;
            _noVoteStagingValue = _noVoteAppliedValue;
            _downvotedStagingValue = _downvotedAppliedValue;

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }

        public void ApplyStagingValues()
        {
            _upvotedAppliedValue = _upvotedStagingValue;
            _noVoteAppliedValue = _noVoteStagingValue;
            _downvotedAppliedValue = _downvotedStagingValue;
        }

        public void ApplyDefaultValues()
        {
            _upvotedAppliedValue = false;
            _noVoteAppliedValue = false;
            _downvotedAppliedValue = false;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
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

        public List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "upvoted", _upvotedAppliedValue,
                "noVote", _noVoteAppliedValue,
                "downvoted", _downvotedAppliedValue);
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

            if (_viewGameObject != null)
                _parserParams.EmitEvent("refresh-values");
        }
    }
}
