using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EnhancedSearchAndFilters.SongData
{
    [Serializable]
    public class BeatmapDetails
    {
        public string LevelID { get; }
        [JsonIgnore]
        public bool IsOST { get { return !LevelID.StartsWith("custom_level"); } }
        public string SongName { get; }
        //public string SongSubName { get; }
        //public string SongAuthorName { get; }
        //public string LevelAuthoName { get; }
        public float BeatsPerMinute { get; }
        public float SongDuration { get; }
        public SimplifiedDifficultyBeatmapSet[] DifficultyBeatmapSets { get; }

        public BeatmapDetails(IBeatmapLevel level)
        {
            LevelID = level.levelID;
            SongName = level.songName;
            BeatsPerMinute = level.beatsPerMinute;

            if (level.beatmapLevelData.audioClip != null)
                SongDuration = level.beatmapLevelData.audioClip.length;
            else
            {
                SongDuration = level.songDuration;
                if (level is CustomBeatmapLevel)
                    Logger.log.Debug("Using stored song duration for custom song (might not work)");
            }

            var levelDifficultyBeatmapSets = level.beatmapLevelData.difficultyBeatmapSets;
            DifficultyBeatmapSets = new SimplifiedDifficultyBeatmapSet[levelDifficultyBeatmapSets.Count()];
            for (int i = 0; i < levelDifficultyBeatmapSets.Count(); ++i)
            {
                DifficultyBeatmapSets[i].CharacteristicName = levelDifficultyBeatmapSets[i].beatmapCharacteristic.serializedName;

                var levelDifficultyBeatmaps = levelDifficultyBeatmapSets[i].difficultyBeatmaps;
                DifficultyBeatmapSets[i].DifficultyBeatmaps = new SimplifiedDifficultyBeatmap[levelDifficultyBeatmaps.Length];
                for (int j = 0; j < levelDifficultyBeatmaps.Length; ++j)
                {
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].Difficulty = levelDifficultyBeatmaps[j].difficulty;
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].NoteJumpMovementSpeed = levelDifficultyBeatmaps[j].noteJumpMovementSpeed;
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].NotesCount = levelDifficultyBeatmaps[j].beatmapData.notesCount;
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].BombsCount = levelDifficultyBeatmaps[j].beatmapData.bombsCount;
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].ObstaclesCount = levelDifficultyBeatmaps[j].beatmapData.obstaclesCount;
                    DifficultyBeatmapSets[i].DifficultyBeatmaps[j].SpawnRotationEventsCount = levelDifficultyBeatmaps[j].beatmapData.spawnRotationEventsCount;
                }
            }
        }

        [JsonConstructor]
        public BeatmapDetails(string levelID, string songName, float beatsPerMinute, float songDuration, SimplifiedDifficultyBeatmapSet[] difficultyBeatmapSets)
        {
            LevelID = levelID;
            SongName = songName;
            BeatsPerMinute = beatsPerMinute;
            SongDuration = songDuration;
            DifficultyBeatmapSets = difficultyBeatmapSets;
        }
    }

    [Serializable]
    public struct SimplifiedDifficultyBeatmapSet
    {
        public string CharacteristicName;
        public SimplifiedDifficultyBeatmap[] DifficultyBeatmaps;

        [JsonConstructor]
        public SimplifiedDifficultyBeatmapSet(string characteristicName, SimplifiedDifficultyBeatmap[] difficultyBeatmaps)
        {
            CharacteristicName = characteristicName;
            DifficultyBeatmaps = difficultyBeatmaps;
        }
    }

    [Serializable]
    public struct SimplifiedDifficultyBeatmap
    {
        public BeatmapDifficulty Difficulty;
        public float NoteJumpMovementSpeed;
        public int NotesCount;
        public int BombsCount;
        public int ObstaclesCount;
        public int SpawnRotationEventsCount;

        [JsonConstructor]
        public SimplifiedDifficultyBeatmap(string difficulty, float noteJumpMovementSpeed, int notesCount, int bombsCount, int obstaclesCount, int spawnRotationEventsCount)
        {
            Difficulty = (BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), difficulty, true);
            NoteJumpMovementSpeed = noteJumpMovementSpeed;
            NotesCount = notesCount;
            BombsCount = bombsCount;
            ObstaclesCount = obstaclesCount;
            SpawnRotationEventsCount = spawnRotationEventsCount;
        }
    }
}
