using System;
using System.Collections.Generic;
using System.Linq;
using SongDataCorePlugin = SongDataCore.Plugin;
using SongDataCore.BeatSaver;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongDataCoreTweaks
    {
        public static bool ModLoaded { get; set; } = false;

        /// <summary>
        /// Get the BeatmapDetails associated with a specific levelID from data retrieved by SongDataCore.BeatSaver.
        /// </summary>
        /// <param name="levelID">The level ID associated with the song.</param>
        /// <param name="beatmapDetails">The returned BeatmapDetails object, or null if it could not be retrieved.</param>
        /// <returns>True, if a valid BeatmapDetails object was returned. Otherwise, false.</returns>
        public static bool GetBeatmapDetails(string levelID, out BeatmapDetails beatmapDetails)
        {
            if (!ModLoaded)
            {
                beatmapDetails = null;
                return false;
            }

            return _GetBeatmapDetails(levelID, out beatmapDetails);
        }

        private static bool _GetBeatmapDetails(string levelID, out BeatmapDetails beatmapDetails)
        {
            if (!SongDataCorePlugin.BeatSaver.IsDataAvailable() ||
                !SongDataCorePlugin.BeatSaver.Data.Songs.TryGetValue(levelID.Substring(13).ToLower(), out var song))
            {
                beatmapDetails = null;
                return false;
            }

            try
            {
                string songName = song.metadata.songName;
                float bpm = float.Parse(song.metadata.bpm);
                float duration = float.Parse(song.metadata.characteristics.First().Value.difficulties.First().Value.length);
                SimplifiedDifficultyBeatmapSet[] difficultyBeatmapSets = song.metadata.characteristics.Select(delegate (KeyValuePair<string, BeatSaverSongCharacteristics> characteristicSetPair)
                {
                    return new SimplifiedDifficultyBeatmapSet(characteristicSetPair.Value.name,
                        characteristicSetPair.Value.difficulties.Select(delegate (KeyValuePair<string, BeatSaverSongCharacteristicData> characteristicDataPair)
                        {
                            var difficultyName = characteristicDataPair.Key.First().ToString().ToUpper().Concat(characteristicDataPair.Key.Skip(1)).ToString();
                            var data = characteristicDataPair.Value;

                        // NOTE: this will may need to be changed in the future, if the 360 notes thing is ever added to PCVR Beat Saber
                        return new SimplifiedDifficultyBeatmap(difficultyName, float.Parse(data.njs), int.Parse(data.notes), int.Parse(data.bombs), int.Parse(data.obstacles), 0);
                        }).ToArray());
                }).ToArray();

                beatmapDetails = new BeatmapDetails(levelID, songName, bpm, duration, difficultyBeatmapSets);
                return true;
            }
            catch (Exception e)
            {
                Logger.log.Warn("Error occured when attempting to create BeatmapDetails object from information stored in SongDataCore");
                Logger.log.Warn(e);

                beatmapDetails = null;
                return false;
            }
        }

        /// <summary>
        /// Determine whether a song is ranked.
        /// </summary>
        /// <param name="levelID">The level ID associated with the song.</param>
        /// <param name="ppList">An array of PP values (not sorted, not associated with difficulty) or null if the song is not ranked.</param>
        /// <returns>True, if the song is ranked. Otherwise, false.</returns>
        public static bool IsRanked(string levelID, out float[] ppList)
        {
            if (!ModLoaded)
            {
                ppList = null;
                return false;
            }

            return _IsRanked(levelID, out ppList);
        }

        private static bool _IsRanked(string levelID, out float[] ppList)
        {
            if (!SongDataCorePlugin.ScoreSaber.IsDataAvailable() ||
                !SongDataCorePlugin.ScoreSaber.Data.Songs.TryGetValue(levelID.Substring(13).ToLower(), out var song))
            {
                ppList = null;
                return false;
            }

            ppList = song.diffs.Select(x => (float)x.pp).Where(x => x > 0).ToArray();
            return ppList.Any();
        }

        /// <summary>
        /// Gets the 'star' difficulty ratings for each difficulty of a song.
        /// </summary>
        /// <param name="levelID">The level ID associated with the song.</param>
        /// <returns>A list of Tuples containing difficulty name/star rating pairs.</returns>
        public static Tuple<string, double>[] GetStarDifficultyRating(string levelID)
        {
            if (!ModLoaded)
                return null;

            return _GetStarDifficultyRating(levelID);
        }

        private static Tuple<string, double>[] _GetStarDifficultyRating(string levelID)
        {
            if (!SongDataCorePlugin.ScoreSaber.IsDataAvailable() ||
                !SongDataCorePlugin.ScoreSaber.Data.Songs.TryGetValue(levelID.Substring(13).ToLower(), out var song))
                return null;

            return song.diffs.Select(x => new Tuple<string, double>(x.diff, x.star)).ToArray();
        }
    }
}
