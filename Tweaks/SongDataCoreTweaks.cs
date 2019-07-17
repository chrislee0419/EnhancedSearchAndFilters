using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedSearchAndFilters.SongData;
using SongDataCorePlugin = SongDataCore.Plugin;
using SongDataCore.BeatSaver;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongDataCoreTweaks
    {
        public static bool ModLoaded { get; set; } = false;

        /// <summary>
        /// Provide action delegates to the 'OnDataFinishedProcessing' events for each data source handled by SongDataCore.
        /// </summary>
        /// <param name="onBeatSaverDataLoadedHandler">An action delegate that will be called when the BeatSaver data has finished loading.</param>
        /// <param name="onScoreSaberDataLoadedHandler">An action delegate that will be called when the ScoreSaber data has finished loading.</param>
        public static void InstallOnDataLoadedHandlers(Action onBeatSaverDataLoadedHandler, Action onScoreSaberDataLoadedHandler)
        {
            if (!ModLoaded)
                return;

            _InstallOnDataLoadedHandlers(onBeatSaverDataLoadedHandler, onScoreSaberDataLoadedHandler);
        }

        public static void _InstallOnDataLoadedHandlers(Action onBeatSaverDataLoadedHandler, Action onScoreSaberDataLoadedHandler)
        {
            if (onBeatSaverDataLoadedHandler != null)
                SongDataCorePlugin.BeatSaver.OnDataFinishedProcessing += onBeatSaverDataLoadedHandler;
            if (onScoreSaberDataLoadedHandler != null)
                SongDataCorePlugin.ScoreSaber.OnDataFinishedProcessing += onScoreSaberDataLoadedHandler;
        }

        /// <summary>
        /// Remove action delegates from the 'OnDataFinishedProcessing' events added by InstallOnDataLoadedHandlers.
        /// </summary>
        /// <param name="onBeatSaverDataLoadedHandler">An action delegate to stop listening for the BeatSaver's OnDataFinishedProcessing event.</param>
        /// <param name="onScoreSaberDataLoadedHandler">An action delegate to stop listening for the ScoreSaber's OnDataFinishedProcessing event.</param>
        public static void RemoveOnDataLoadedHandlers(Action onBeatSaverDataLoadedHandler, Action onScoreSaberDataLoadedHandler)
        {
            if (!ModLoaded)
                return;

            _RemoveOnDataLoadedHandlers(onBeatSaverDataLoadedHandler, onScoreSaberDataLoadedHandler);
        }

        public static void _RemoveOnDataLoadedHandlers(Action onBeatSaverDataLoadedHandler, Action onScoreSaberDataLoadedHandler)
        {
            if (onBeatSaverDataLoadedHandler != null)
                SongDataCorePlugin.BeatSaver.OnDataFinishedProcessing -= onBeatSaverDataLoadedHandler;
            if (onScoreSaberDataLoadedHandler != null)
                SongDataCorePlugin.ScoreSaber.OnDataFinishedProcessing -= onScoreSaberDataLoadedHandler;
        }

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
                // NOTE: since BeatSaver calculates the duration of a song using the last note (or event?) of a beatmap, instead of the using the length of the audio file,
                //       it is extremely likely that this duration is going to be a bit shorter than the actual length of the audio (typically < 10 seconds shorter)
                //       (or even vastly shorter if there is a long period of no notes at the end of a beatmap)
                //       despite that, i'll keep this limitation since the difference should usually be minimal
                //       and the speedup compared to loading beatmap details for the first time is fairly massive
                float duration = song.metadata.characteristics.First().Value.difficulties.Select(x => x.Value == null ? 0f : float.Parse(x.Value.length)).Max();

                SimplifiedDifficultyBeatmapSet[] difficultyBeatmapSets = song.metadata.characteristics.Select(delegate (KeyValuePair<string, BeatSaverSongCharacteristics> characteristicSetPair)
                {
                    string loadedCharacteristicName = characteristicSetPair.Value.name.ToLower();
                    string actualCharacteristicName = null;

                    if (loadedCharacteristicName == "standard")
                        actualCharacteristicName = "LEVEL_STANDARD";
                    else if (loadedCharacteristicName == "onesaber")
                        actualCharacteristicName = "LEVEL_ONE_SABER";
                    else if (loadedCharacteristicName == "noarrows")
                        actualCharacteristicName = "LEVEL_NO_ARROWS";
                    else
                        actualCharacteristicName = characteristicSetPair.Value.name;    // currently, only the 'Lightshow' and 'Lawless' custom characteristics should be possible

                    if (string.IsNullOrEmpty(actualCharacteristicName))
                    {
                        Logger.log.Debug($"Unable to create SimplifiedDifficultyBeatmapSet from BeatSaver data: could not parse '{(loadedCharacteristicName == null ? "null" : loadedCharacteristicName)}' as a characteristic.");
                        return new SimplifiedDifficultyBeatmapSet(null, null);
                    }

                    SimplifiedDifficultyBeatmap[] difficultyBeatmaps = characteristicSetPair.Value.difficulties.Where(x => x.Value != null).Select(delegate (KeyValuePair<string, BeatSaverSongCharacteristicData> characteristicDataPair)
                    {
                        var difficultyName = string.Concat(characteristicDataPair.Key.First().ToString().ToUpper(), string.Concat(characteristicDataPair.Key.Skip(1)));
                        var data = characteristicDataPair.Value;

                        // NOTE: this will may need to be changed in the future, if the 360 notes thing is ever added to PCVR Beat Saber
                        //       also, from my testing, the parsed NJS could be 0, so that should be fixed by loading the details stored locally
                        return new SimplifiedDifficultyBeatmap(difficultyName, float.Parse(data.njs), int.Parse(data.notes), int.Parse(data.bombs), int.Parse(data.obstacles), 0);
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

                beatmapDetails = new BeatmapDetails(levelID, songName, bpm, duration, difficultyBeatmapSets);
                return true;
            }
            catch (Exception e)
            {
                Logger.log.Warn("Error occurred when attempting to create BeatmapDetails object from information stored in SongDataCore");
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
