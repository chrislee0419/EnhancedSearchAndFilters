using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedSearchAndFilters.SongData;
using SongDataCorePlugin = SongDataCore.Plugin;
using SongDataCore.BeatStar;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongDataCoreTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static bool IsDataAvailable => SongDataCorePlugin.Songs?.IsDataAvailable() ?? false;

        /// <summary>
        /// Get the BeatmapDetails associated with a specific levelID from data retrieved by SongDataCore.BeatSaver.
        /// </summary>
        /// <param name="level">The song's associated preview beatmap level.</param>
        /// <param name="beatmapDetails">The returned BeatmapDetails object, or null if it could not be retrieved.</param>
        /// <returns>True, if a valid BeatmapDetails object was returned. Otherwise, false.</returns>
        public static bool GetBeatmapDetails(CustomPreviewBeatmapLevel level, out BeatmapDetails beatmapDetails)
        {
            if (!ModLoaded)
            {
                beatmapDetails = null;
                return false;
            }

            return _GetBeatmapDetails(level, out beatmapDetails);
        }

        private static bool _GetBeatmapDetails(CustomPreviewBeatmapLevel level, out BeatmapDetails beatmapDetails)
        {
            if (!IsDataAvailable ||
                !SongDataCorePlugin.Songs.Data.Songs.TryGetValue(GetCustomLevelHash(level), out var song))
            {
                beatmapDetails = null;
                return false;
            }

            try
            {
                float bpm = song.bmp;
                // NOTE: since BeatSaver calculates the duration of a song using the last note (or event?) of a beatmap, instead of the using the length of the audio file,
                //       it is extremely likely that this duration is going to be a bit shorter than the actual length of the audio (typically < 10 seconds shorter)
                //       (or even vastly shorter if there is a long period of no notes at the end of a beatmap)
                //       despite that, i'll keep this limitation since the difference should usually be minimal
                //       and the speedup compared to loading beatmap details for the first time is fairly massive
                Func<KeyValuePair<BeatStarCharacteristics, Dictionary<string, BeatStarSongDifficultyStats>>, float> getDuration = delegate (KeyValuePair<BeatStarCharacteristics, Dictionary<string, BeatStarSongDifficultyStats>> characteristics)
                {
                    var characteristicDurations = characteristics.Value.Select(delegate (KeyValuePair<string, BeatStarSongDifficultyStats> data)
                    {
                        if (data.Value == null)
                            return 0f;
                        else
                            return Convert.ToSingle(data.Value.len);
                    });

                    return characteristicDurations.Any() ? characteristicDurations.Max() : 0f;
                };
                var durations = song.characteristics.Select(getDuration);
                float duration;

                if (durations.Any(x => x > 0f))
                {
                    // assuming the maximum is the actual duration
                    duration = durations.Max();
                }
                else
                {
                    Logger.log.Warn($"Unable to acquire valid song duration for level ID '{level.levelID}' from information provided by SongDataCore");

                    beatmapDetails = null;
                    return false;
                }

                SimplifiedDifficultyBeatmapSet[] difficultyBeatmapSets = song.characteristics.Select(delegate (KeyValuePair<BeatStarCharacteristics, Dictionary<string, BeatStarSongDifficultyStats>> characteristicPair)
                {
                    BeatStarCharacteristics loadedCharacteristicName = characteristicPair.Key;
                    string actualCharacteristicName = loadedCharacteristicName != BeatStarCharacteristics.Unkown ? loadedCharacteristicName.ToString() : null;

                    if (string.IsNullOrEmpty(actualCharacteristicName))
                    {
                        Logger.log.Warn($"Unable to create SimplifiedDifficultyBeatmapSet from BeatSaver data: could not parse '{loadedCharacteristicName.ToString()}' as a valid characteristic.");
                        return new SimplifiedDifficultyBeatmapSet(null, null);
                    }

                    SimplifiedDifficultyBeatmap[] difficultyBeatmaps = characteristicPair.Value.Where(x => x.Value != null).Select(delegate (KeyValuePair<string, BeatStarSongDifficultyStats> difficultyPair)
                    {
                        // this will throw an exception (that will be caught) if the difficulty name cannot be parsed
                        var diffString = difficultyPair.Key == "Expert+" ? "ExpertPlus" : difficultyPair.Key;
                        var diff = (BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), diffString);

                        BeatStarSongDifficultyStats data = difficultyPair.Value;

                        // NOTE: from my testing, the parsed NJS could be 0, so that should be fixed by loading the details stored locally
                        return new SimplifiedDifficultyBeatmap(diff, Convert.ToSingle(data.njs), data.nts, data.bmb, data.obs, 0);
                    }).ToArray();

                    return new SimplifiedDifficultyBeatmapSet(actualCharacteristicName, difficultyBeatmaps);
                }).ToArray();

                // if there were any errors during the creation of the SimplifiedDifficultyBeatmapSet objects, do not create a BeatmapDetails object from it
                if (difficultyBeatmapSets.Any(x => string.IsNullOrEmpty(x.CharacteristicName) || x.DifficultyBeatmaps == null))
                {
                    Logger.log.Warn("Error occurred when attempting to create SimplifiedDifficultyBeatmapSet array from object stored in SongDataCore (could not create BeatmapDetails object).");

                    beatmapDetails = null;
                    return false;
                }

                beatmapDetails = new BeatmapDetails(level.levelID, level.songName, bpm, duration, difficultyBeatmapSets);
                return true;
            }
            catch (Exception e)
            {
                Logger.log.Warn($"Unable to create BeatmapDetails object for level ID '{level.levelID}' from information provided by SongDataCore");
                Logger.log.Debug(e);

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
            if (!IsDataAvailable ||
                !SongDataCorePlugin.Songs.Data.Songs.TryGetValue(GetCustomLevelHash(levelID), out var song))
            {
                ppList = null;
                return false;
            }

            ppList = song.diffs.Select(x => Convert.ToSingle(x.pp)).Where(x => x > 0).ToArray();
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
            if (!IsDataAvailable ||
                !SongDataCorePlugin.Songs.Data.Songs.TryGetValue(GetCustomLevelHash(levelID), out var song))
                return null;

            return song.diffs.Select(x => new Tuple<string, double>(x.diff, x.star)).ToArray();
        }

        private static string GetCustomLevelHash(CustomPreviewBeatmapLevel level) => GetCustomLevelHash(level.levelID);

        private static string GetCustomLevelHash(string levelID) => BeatmapDetailsLoader.GetSimplifiedLevelID(levelID).Substring(CustomLevelLoader.kCustomLevelPrefixId.Length);
    }
}
