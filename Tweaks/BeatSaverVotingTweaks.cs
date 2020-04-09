using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using EnhancedSearchAndFilters.SongData;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class BeatSaverVotingTweaks
    {
        public static bool ModLoaded { get; set; } = false;

        private static Dictionary<string, SongVote> _votedSongs = null;

        private static string VotedSongsFilePath = $"{Environment.CurrentDirectory}\\UserData\\votedSongs.json";
        private static readonly JsonSerializerSettings deserializerSettings = new JsonSerializerSettings
        {
            Error = delegate (object sender, ErrorEventArgs eventArgs)
            {
                eventArgs.ErrorContext.Handled = true;
            }
        };

        public static void ReadVotedSongsData()
        {
            if (File.Exists(VotedSongsFilePath))
            {
                _votedSongs = JsonConvert.DeserializeObject<Dictionary<string, SongVote>>(
                    File.ReadAllText(VotedSongsFilePath, Encoding.UTF8), deserializerSettings);
            }
        }

        public static VoteStatus GetVoteStatus(BeatmapDetails details)
        {
            if (_votedSongs == null || details.IsOST)
                return VoteStatus.NoVote;

            string levelHash = details.GetLevelHash();
            SongVote? songVote = null;
            if (_votedSongs.ContainsKey(levelHash.ToLower()))
                songVote = _votedSongs[levelHash.ToLower()];
            else if (_votedSongs.ContainsKey(levelHash.ToUpper()))
                songVote = _votedSongs[levelHash.ToUpper()];

            if (songVote.HasValue)
                return songVote.Value.voteType == VoteType.Upvote ? VoteStatus.Upvoted : VoteStatus.Downvoted;
            else
                return VoteStatus.NoVote;
        }

        public static void Cleanup()
        {
            _votedSongs = null;
        }

        public enum VoteStatus
        {
            Upvoted,
            NoVote,
            Downvoted
        }

        // re-implementation of stuff in https://github.com/Kylemc1413/BeatSaverVoting/blob/master/BeatSaverVoting/Plugin.cs
        // so i can read from the votedSongs.json file
        private struct SongVote
        {
            public string key;
            [JsonConverter(typeof(StringEnumConverter))]
            public VoteType voteType;

            public SongVote(string key, VoteType voteType)
            {
                this.key = key;
                this.voteType = voteType;
            }
        }
        private enum VoteType
        {
            Upvote,
            Downvote
        }
    }
}
