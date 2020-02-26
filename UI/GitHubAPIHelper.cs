using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using SemVerVersion = SemVer.Version;

namespace EnhancedSearchAndFilters.UI
{
    internal class GitHubAPIHelper : PersistentSingleton<GitHubAPIHelper>
    {
        // NOTE: github does rate limiting at 60 requests per hour for unregistered users
        private SemVerVersion _latestVersion = null;
        private List<string> _openIssues = null;
        private DateTime _lastRequest = default;

        private const string LatestReleaseAPIURL = "https://api.github.com/repos/chrislee0419/EnhancedSearchAndFilters/releases/latest";
        private const string OpenIssuesAPIURL = "https://api.github.com/repos/chrislee0419/EnhancedSearchAndFilters/issues?state=open&labels=bug";

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

        public void GetOpenIssues(Action<bool, List<string>> onFinish)
        {
            if (onFinish == null)
                return;

            TimeSpan diff = DateTime.Now - _lastRequest;
            if (_openIssues != null && diff.Hours < 1)
                onFinish.Invoke(true, _openIssues);
            else
                StartCoroutine(_GetOpenIssues(onFinish));
        }

        private IEnumerator _GetLatestReleaseVersion(Action<bool, SemVerVersion> onFinish)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(LatestReleaseAPIURL))
            {
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    try
                    {
                        JObject content = JObject.Parse(request.downloadHandler.text);
                        _latestVersion = new SemVerVersion(content["name"].ToString());

                        onFinish.Invoke(true, _latestVersion);
                        _lastRequest = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        Logger.log.Error($"Unable to retrieve latest version number from GitHub API ({e.Message})");
                        Logger.log.Debug(e);

                        onFinish.Invoke(false, null);
                    }
                }
                else
                {
                    Logger.log.Error($"Unable to retrieve latest version number from GitHub API (response code = {request.responseCode})");

                    onFinish.Invoke(false, null);
                }
            }
        }

        private IEnumerator _GetOpenIssues(Action<bool, List<string>> onFinish)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(OpenIssuesAPIURL))
            {
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    try
                    {
                        JArray content = JArray.Parse(request.downloadHandler.text);

                        _openIssues = new List<string>(content.Count);
                        foreach (JObject issue in content)
                            _openIssues.Add(issue["title"].ToString());

                        onFinish.Invoke(true, _openIssues);
                        _lastRequest = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        _openIssues = null;

                        Logger.log.Error($"Unable to retrieve latest version number from GitHub API ({e.Message})");
                        Logger.log.Debug(e);

                        onFinish.Invoke(false, null);
                    }
                }
                else
                {
                    Logger.log.Error($"Unable to retrieve latest version number from GitHub API (response code = {request.responseCode})");
                    onFinish.Invoke(false, null);
                }
            }
        }
    }
}
