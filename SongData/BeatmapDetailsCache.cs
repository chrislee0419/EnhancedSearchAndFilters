using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EnhancedSearchAndFilters.SongData
{
    internal class BeatmapDetailsCache
    {
        public int Version { get; }
        public List<BeatmapDetails> Cache { get; }

        private const int CURRENT_CACHE_VERSION = 2;

        private BeatmapDetailsCache(List<BeatmapDetails> beatmapDetails)
        {
            Version = CURRENT_CACHE_VERSION;
            Cache = beatmapDetails;
        }

        [JsonConstructor]
        public BeatmapDetailsCache(int version, BeatmapDetails[] cache)
        {
            Version = version;
            Cache = cache.ToList();
        }

        public static async Task<List<BeatmapDetails>> GetBeatmapDetailsFromCacheAsync(string path)
        {
            Task<List<BeatmapDetails>> t = Task.Run(delegate ()
            {
                try
                {
                    var cache = JsonConvert.DeserializeObject<BeatmapDetailsCache>(File.ReadAllText(path));

                    if (cache.Version < CURRENT_CACHE_VERSION)
                    {
                        Logger.log.Warn("EnhancedSearchAndFilters details cache is outdated. Forcing the cache to be rebuilt.");
                        return new List<BeatmapDetails>();
                    }

                    Logger.log.Info("Successfully loaded details cache from storage");
                    return cache.Cache;
                }
                catch (FileNotFoundException)
                {
                    Logger.log.Info($"Cache file could not be found in the path: '{path}'");
                }
                catch (JsonSerializationException)
                {
                    Logger.log.Warn("Unable to deserialize cache file. Could be an older version of the cache file (will be replaced after the in-memory cache is rebuilt).");
                }
                catch (Exception e)
                {
                    Logger.log.Warn(e);
                }
                return new List<BeatmapDetails>();
            });

            return await t.ConfigureAwait(false);
        }

        public static void SaveBeatmapDetailsToCache(string path, List<BeatmapDetails> beatmapDetailsList)
        {
            var cache = new BeatmapDetailsCache(beatmapDetailsList);
            File.WriteAllText(path, JsonConvert.SerializeObject(cache));
        }
    }
}
