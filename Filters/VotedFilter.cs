using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.UI;
using EnhancedSearchAndFilters.SongData;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class VotedFilter : IFilter
    {
        public string FilterName { get { return "Voted Songs"; } }
        public bool IsAvailable { get { return Tweaks.BeatSaverDownloaderTweaks.ModLoaded; } }
        public FilterStatus Status
        {
            get
            {
                if (ApplyFilter)
                {
                    if (_upvotedAppliedValue != _upvotedStagingValue ||
                        _noVoteAppliedValue != _noVoteStagingValue ||
                        _downvotedAppliedValue != _downvotedStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_upvotedStagingValue || _noVoteStagingValue || _downvotedStagingValue)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool ApplyFilter
        {
            get
            {
                return _upvotedAppliedValue || _noVoteAppliedValue || _downvotedAppliedValue;
            }
            set
            {
                if (value)
                {
                    _upvotedAppliedValue = _upvotedStagingValue;
                    _noVoteAppliedValue = _noVoteStagingValue;
                    _downvotedAppliedValue = _downvotedStagingValue;
                }
                else
                {
                    _upvotedAppliedValue = false;
                    _noVoteAppliedValue = false;
                    _downvotedAppliedValue = false;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[1];

        public event Action SettingChanged;

        private Toggle _upvotedToggle;
        private Toggle _noVoteToggle;
        private Toggle _downvotedToggle;

        private bool _isInitialized = false;

        private bool _upvotedStagingValue = false;
        private bool _noVoteStagingValue = false;
        private bool _downvotedStagingValue = false;
        private bool _upvotedAppliedValue = false;
        private bool _noVoteAppliedValue = false;
        private bool _downvotedAppliedValue = false;

        private const string votedSongsPath = "UserData\\votedSongs.json";

        public void Init()
        {
            if (_isInitialized)
                return;

            // since we're using BeatSaverDownloader's votedSong.json file, we need the mod to be present
            if (!Tweaks.BeatSaverDownloaderTweaks.ModLoaded)
            {
                var noModMessage = BeatSaberUI.CreateText(null, "<color=#FFAAAA>Sorry!\n\n<size=80%>This filter requires the BeatSaverDownloader mod\n to be installed.</size></color>", Vector2.zero);
                noModMessage.alignment = TextAlignmentOptions.Center;
                noModMessage.fontSize = 5.5f;

                Controls[0] = new FilterControl(noModMessage.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 50f), new Vector2(0f, 10f));
            }
            else
            {
                var container = new GameObject("VotedFilterContainer");
                Controls[0] = new FilterControl(container, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                    delegate ()
                    {
                        _upvotedToggle.isOn = _upvotedStagingValue;
                        _noVoteToggle.isOn = _noVoteStagingValue;
                        _downvotedToggle.isOn = _downvotedStagingValue;
                    });

                // blank image assigned to container so it'll have a RectTransform
                var unused = container.AddComponent<Image>();
                unused.color = new Color(0f, 0f, 0f, 0f);
                unused.material = UIUtilities.NoGlowMaterial;

                var text = BeatSaberUI.CreateText(container.transform as RectTransform, "Keep Songs That You Voted", Vector2.zero);
                text.fontSize = 5.5f;
                var rt = text.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = rt.anchorMin;
                rt.pivot = rt.anchorMin;
                BeatSaberUI.AddHintText(text.rectTransform, "Keep songs depending on how you have voted (or not voted)");

                var togglePrefab = Utilities.GetTogglePrefab();

                _upvotedToggle = CreateToggleControl(container.transform as RectTransform, "Upvoted Songs", 0, togglePrefab.toggle);
                _upvotedToggle.onValueChanged.AddListener(delegate (bool value)
                {
                    _upvotedStagingValue = value;
                    SettingChanged?.Invoke();
                });

                _noVoteToggle = CreateToggleControl(container.transform as RectTransform, "Songs With No Vote", 1, togglePrefab.toggle);
                _noVoteToggle.onValueChanged.AddListener(delegate (bool value)
                {
                    _noVoteStagingValue = value;
                    SettingChanged?.Invoke();
                });

                _downvotedToggle = CreateToggleControl(container.transform as RectTransform, "Downvoted Songs", 2, togglePrefab.toggle, false);
                _downvotedToggle.onValueChanged.AddListener(delegate (bool value)
                {
                    _downvotedStagingValue = value;
                    SettingChanged?.Invoke();
                });

                Object.Destroy(togglePrefab.gameObject);
            }

            _isInitialized = true;
        }

        private Toggle CreateToggleControl(RectTransform parent, string label, int index, Toggle prefab, bool createDivider = true)
        {
            var text = BeatSaberUI.CreateText(parent, label, new Vector2(4f, -9.5f - (10f * index)), new Vector2(30f, 10f));
            text.fontSize = 5f;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var toggle = Utilities.CreateToggleFromPrefab(prefab, parent);
            rt = toggle.transform as RectTransform;
            rt.anchorMin = Vector2.one;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(6f, 6f);
            rt.anchoredPosition = new Vector2(-6f, -13f - (10f * index));

            if (createDivider)
            {
                var divider = Utilities.CreateHorizontalDivider(parent, 0f, false);
                divider.rectTransform.anchoredPosition = new Vector2(0f, -18f - (10f * index));
            }

            return toggle;
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized || !Tweaks.BeatSaverDownloaderTweaks.ModLoaded)
                return;

            _upvotedStagingValue = false;
            _noVoteStagingValue = false;
            _downvotedStagingValue = false;

            _upvotedToggle.isOn = false;
            _noVoteToggle.isOn = false;
            _downvotedToggle.isOn = false;
        }

        public void ResetValues()
        {
            if (!_isInitialized || !Tweaks.BeatSaverDownloaderTweaks.ModLoaded)
                return;

            _upvotedStagingValue = _upvotedAppliedValue;
            _noVoteStagingValue = _noVoteAppliedValue;
            _downvotedStagingValue = _downvotedAppliedValue;

            _upvotedToggle.isOn = _upvotedAppliedValue;
            _noVoteToggle.isOn = _noVoteAppliedValue;
            _downvotedToggle.isOn = _downvotedAppliedValue;
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || !Tweaks.BeatSaverDownloaderTweaks.ModLoaded || !File.Exists(votedSongsPath))
                return;

            // because the player could vote on songs in game, we can't cache the
            // votedSongs json contents and it has to be acquired every time
            var voteList = JsonConvert.DeserializeObject<Dictionary<string, SongVote>>(File.ReadAllText(votedSongsPath, Encoding.UTF8));

            for (int i = 0; i < detailsList.Count;)
            {
                var song = detailsList[i];

                // don't remove OST songs
                if (song.IsOST)
                {
                    ++i;
                    continue;
                }

                var hash = song.LevelID.Substring(13).ToLower();      // get hash from LevelID ("custom_level_HASH")
                bool remove = true;

                if (voteList.ContainsKey(hash))
                {
                    VoteType vote = voteList[hash].voteType;

                    if (vote == VoteType.Upvote && _upvotedAppliedValue)
                        remove = false;
                    else if (vote == VoteType.Downvote && _downvotedAppliedValue)
                        remove = false;
                }
                else if (_noVoteAppliedValue)
                {
                    remove = false;
                }

                if (remove)
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }
    }

    // re-implementation of voting stuff from:
    // https://github.com/Kylemc1413/BeatSaverDownloader/blob/master/BeatSaverDownloader/Misc/PluginConfig.cs
    internal enum VoteType
    {
        Upvote,
        Downvote,
    };

    [Serializable]
    internal struct SongVote
    {
        public string key;
        [JsonConverter(typeof(StringEnumConverter))]
        public VoteType voteType;

        [JsonConstructor]
        public SongVote(string key, VoteType voteType)
        {
            this.key = key;
            this.voteType = voteType;
        }
    }
}
