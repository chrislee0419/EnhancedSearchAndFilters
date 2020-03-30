using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace EnhancedSearchAndFilters.SongData
{
    [Serializable]
    public class BeatmapDetails
    {
        public string LevelID { get; private set; }
        [JsonIgnore]
        public bool IsOST { get { return !LevelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId); } }
        public string SongName { get; private set; }
        //public string SongSubName { get; private set; }
        //public string SongAuthorName { get; private set; }
        //public string LevelAuthorName { get; private set; }
        public float BeatsPerMinute { get; private set; }
        public float SongDuration { get; private set; }
        public SimplifiedDifficultyBeatmapSet[] DifficultyBeatmapSets { get; private set; }

        private BeatmapDetails()
        { }

        public BeatmapDetails(IBeatmapLevel level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level), "IBeatmapLevel parameter 'level' cannot be null");
            else if (level.beatmapLevelData == null)
                throw new ArgumentException("Provided IBeatmapLevel object cannot have the 'beatmapLevelData' property be null", nameof(level));

            // remove the directory part of a custom level ID
            LevelID = BeatmapDetailsLoader.GetSimplifiedLevelID(level);

            SongName = level.songName;
            BeatsPerMinute = level.beatsPerMinute;

            if (level.beatmapLevelData.audioClip != null)
            {
                SongDuration = level.beatmapLevelData.audioClip.length;
            }
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

        public static async Task<BeatmapDetails> CreateBeatmapDetailsFromFilesAsync(CustomPreviewBeatmapLevel customLevel, CancellationToken token)
        {
            StandardLevelInfoSaveData infoData = customLevel.standardLevelInfoSaveData;
            BeatmapDetails beatmapDetails = new BeatmapDetails();

            beatmapDetails.LevelID = BeatmapDetailsLoader.GetSimplifiedLevelID(customLevel);
            beatmapDetails.SongName = customLevel.songName;
            beatmapDetails.BeatsPerMinute = infoData.beatsPerMinute;

            // load difficulties for note info
            beatmapDetails.DifficultyBeatmapSets = await Task.Run(async delegate ()
            {
                SimplifiedDifficultyBeatmapSet[] simplifiedDifficultySets = new SimplifiedDifficultyBeatmapSet[infoData.difficultyBeatmapSets.Length];
                for (int i = 0; i < infoData.difficultyBeatmapSets.Length; ++i)
                {
                    token.ThrowIfCancellationRequested();

                    var currentSimplifiedSet = new SimplifiedDifficultyBeatmapSet();
                    simplifiedDifficultySets[i] = currentSimplifiedSet;
                    var currentSet = infoData.difficultyBeatmapSets[i];

                    currentSimplifiedSet.CharacteristicName = currentSet.beatmapCharacteristicName;
                    currentSimplifiedSet.DifficultyBeatmaps = new SimplifiedDifficultyBeatmap[currentSet.difficultyBeatmaps.Length];

                    for (int j = 0; j < currentSet.difficultyBeatmaps.Length; ++j)
                    {
                        var currentSimplifiedDiff = new SimplifiedDifficultyBeatmap();
                        currentSimplifiedSet.DifficultyBeatmaps[j] = currentSimplifiedDiff;
                        var currentDiff = currentSet.difficultyBeatmaps[j];

                        currentDiff.difficulty.BeatmapDifficultyFromSerializedName(out currentSimplifiedDiff.Difficulty);
                        currentSimplifiedDiff.NoteJumpMovementSpeed = currentDiff.noteJumpMovementSpeed;

                        BeatmapSaveData beatmapSaveData = await Task.Run(delegate ()
                        {
                            string filePath = Path.Combine(customLevel.customLevelPath, currentDiff.beatmapFilename);
                            if (File.Exists(filePath))
                                return BeatmapSaveData.DeserializeFromJSONString(File.ReadAllText(filePath));
                            else
                                return null;
                        }, token);
                        token.ThrowIfCancellationRequested();

                        // missing difficulty files
                        if (beatmapSaveData == null)
                            return null;

                        // count notes and bombs
                        currentSimplifiedDiff.NotesCount = 0;
                        currentSimplifiedDiff.BombsCount = 0;
                        foreach (var note in beatmapSaveData.notes)
                        {
                            if (note.type.IsBasicNote())
                                ++currentSimplifiedDiff.NotesCount;
                            else if (note.type == NoteType.Bomb)
                                ++currentSimplifiedDiff.BombsCount;
                        }

                        // count rotation events
                        currentSimplifiedDiff.SpawnRotationEventsCount = 0;
                        foreach (var mapEvent in beatmapSaveData.events)
                        {
                            if (mapEvent.type.IsRotationEvent())
                                ++currentSimplifiedDiff.SpawnRotationEventsCount;
                        }

                        currentSimplifiedDiff.ObstaclesCount = beatmapSaveData.obstacles.Count;

                    }
                }

                return simplifiedDifficultySets;
            }, token);

            // load audio for map length
            AudioClip audioClip = await customLevel.GetPreviewAudioClipAsync(token);

            // data validation
            if (audioClip == null || beatmapDetails.DifficultyBeatmapSets == null)
                return null;

            beatmapDetails.SongDuration = audioClip.length;

            return beatmapDetails;
        }
    }

    [Serializable]
    public class SimplifiedDifficultyBeatmapSet
    {
        public string CharacteristicName;
        public SimplifiedDifficultyBeatmap[] DifficultyBeatmaps;

        internal SimplifiedDifficultyBeatmapSet()
        { }

        [JsonConstructor]
        public SimplifiedDifficultyBeatmapSet(string characteristicName, SimplifiedDifficultyBeatmap[] difficultyBeatmaps)
        {
            CharacteristicName = characteristicName;
            DifficultyBeatmaps = difficultyBeatmaps;
        }
    }

    [Serializable]
    public class SimplifiedDifficultyBeatmap
    {
        public BeatmapDifficulty Difficulty;
        public float NoteJumpMovementSpeed;
        public int NotesCount;
        public int BombsCount;
        public int ObstaclesCount;
        public int SpawnRotationEventsCount;

        internal SimplifiedDifficultyBeatmap()
        { }

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

        public SimplifiedDifficultyBeatmap(BeatmapDifficulty difficulty, float noteJumpMovementSpeed, int notesCount, int bombsCount, int obstaclesCount, int spawnRotationEventsCount)
        {
            Difficulty = difficulty;
            NoteJumpMovementSpeed = noteJumpMovementSpeed;
            NotesCount = notesCount;
            BombsCount = bombsCount;
            ObstaclesCount = obstaclesCount;
            SpawnRotationEventsCount = spawnRotationEventsCount;
        }
    }
}
