using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EnhancedSearchAndFilters.SongData
{
    internal static class SongSortModule
    {
        public static bool Reversed { get; private set; } = false;

        private static SortMode _currentSortMode;
        public static SortMode CurrentSortMode
        {
            get => _currentSortMode;
            set
            {
                if (value == _currentSortMode)
                {
                    Reversed = !Reversed;
                }
                else
                {
                    _currentSortMode = value;
                    Reversed = false;
                }
            }
        }

        public static IPreviewBeatmapLevel[] SortSongs(IPreviewBeatmapLevel[] unsortedLevels)
        {
            switch (_currentSortMode)
            {
                case SortMode.Default:
                    return SortByDefault(unsortedLevels);
                case SortMode.Newest:
                    return SortByNewest(unsortedLevels);
                case SortMode.PlayCount:
                    return SortByPlayCount(unsortedLevels);
                default:
                    return unsortedLevels;
            }
        }

        private static IPreviewBeatmapLevel[] SortByDefault(IPreviewBeatmapLevel[] unsortedLevels)
        {
            if (Reversed)
                return unsortedLevels.Reverse().ToArray();
            else
                return unsortedLevels;
        }

        private static IPreviewBeatmapLevel[] SortByNewest(IPreviewBeatmapLevel[] unsortedLevels)
        {
            var directoriesWithCreationTime = unsortedLevels
                .Select(delegate (IPreviewBeatmapLevel level)
                {
                    if (level is CustomPreviewBeatmapLevel customLevel)
                        return Directory.GetParent(customLevel.customLevelPath).FullName;
                    else
                        return null;
                })
                .Distinct()
                .Where(dirName => dirName != null)
                .Select(dir => new DirectoryInfo(dir))
                .SelectMany(dir => dir.GetDirectories())
                .ToDictionary(x => x.FullName, x => x.CreationTime.Ticks);

            Func<IPreviewBeatmapLevel, long> getCreationTime = delegate (IPreviewBeatmapLevel level)
            {
                if (level is CustomPreviewBeatmapLevel customLevel)
                {
                    if (directoriesWithCreationTime.TryGetValue(Path.GetFullPath(customLevel.customLevelPath), out long creationTime))
                        return creationTime;
                }

                return DateTime.MinValue.Ticks;
            };

            if (Reversed)
                return unsortedLevels.OrderBy(getCreationTime).ToArray();
            else
                return unsortedLevels.OrderByDescending(getCreationTime).ToArray();
        }

        private static IPreviewBeatmapLevel[] SortByPlayCount(IPreviewBeatmapLevel[] unsortedLevels)
        {
            if (PlayerDataHelper.Instance == null)
                return unsortedLevels;

            var levelsWithPlays = unsortedLevels.AsParallel().Select(level => new Tuple<IPreviewBeatmapLevel, int>(level, PlayerDataHelper.Instance.GetPlayCountForLevel(level.levelID)));

            if (Reversed)
                return levelsWithPlays.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();
            else
                return levelsWithPlays.OrderByDescending(x => x.Item2).Select(x => x.Item1).ToArray();
        }

        public static void ResetSortMode()
        {
            _currentSortMode = SortMode.Default;
            Reversed = false;
        }
    }

    internal enum SortMode
    {
        Default,
        Newest,
        PlayCount,
    }
}
