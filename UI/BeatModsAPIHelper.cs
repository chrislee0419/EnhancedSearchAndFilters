using System;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using SemVerVersion = SemVer.Version;

namespace EnhancedSearchAndFilters.UI
{
    internal class BeatModsAPIHelper : PersistentSingleton<BeatModsAPIHelper>
    {
        private SemVerVersion _latestVersion = null;
        private DateTime _lastRequest = default;

        private const string ModsListAPIURL = "https://beatmods.com/api/v1/mod?name=EnhancedSearchAndFilters&status=approved";

        public void GetLatestReleaseVersion(Action<bool, SemVerVersion> onFinish)
        {
            if (onFinish == null)
                return;

            TimeSpan diff = DateTime.Now - _lastRequest;
            if (_latestVersion != null && diff.Hours < 1)
                onFinish.Invoke(true, _latestVersion);
            else
                StartCoroutine(_GetLatestReleaseVersion(onFinish));
        }

        private IEnumerator _GetLatestReleaseVersion(Action<bool, SemVerVersion> onFinish)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(ModsListAPIURL))
            {
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    try
                    {
                        JArray content = JArray.Parse(request.downloadHandler.text);
                        _latestVersion = content
                            .Children<JObject>()
                            .Select(x => new SemVerVersion(x["version"].ToString()))
                            .Max();

                        _lastRequest = DateTime.Now;

                        try
                        {
                            onFinish.Invoke(true, _latestVersion);
                        }
                        catch (Exception e)
                        {
                            Logger.log.Error($"Exception thrown by delegate in GetLatestReleaseVersion ({e.Message})");
                            Logger.log.Debug(e);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.log.Error($"Unable to retrieve latest version number from BeatMods API ({e.Message})");
                        Logger.log.Debug(e);

                        onFinish.Invoke(false, null);
                    }
                }
                else
                {
                    Logger.log.Error($"Unable to retrieve latest version number from BeatMods API (response code = {request.responseCode})");

                    onFinish.Invoke(false, null);
                }
            }
        }
    }
}
