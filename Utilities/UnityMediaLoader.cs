using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

namespace EnhancedSearchAndFilters.Utilities
{
    internal static class UnityMediaLoader
    {
        public static IEnumerator<string> LoadTextCoroutine(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                yield break;

            using (UnityWebRequest request = UnityWebRequest.Get(FileHelpers.GetEscapedURLForFilePath(filePath)))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    yield return null;

                if (request.isNetworkError || request.isHttpError)
                    yield break;
                else
                    yield return request.downloadHandler.text;
            }
        }

        public static IEnumerator<AudioClip> LoadAudioClipCoroutine(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                yield break;

            string fileExtension = Path.GetExtension(filePath).ToLower();
            AudioType audioType;

            switch (fileExtension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".egg":
                    goto case ".ogg";
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                default:
                    yield break;
            }

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(FileHelpers.GetEscapedURLForFilePath(filePath), audioType))
            {
                ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    yield return null;

                if (request.isNetworkError || request.isHttpError)
                    yield break;
                else
                    yield return DownloadHandlerAudioClip.GetContent(request);
            }
        }
    }
}
