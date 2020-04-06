using System.IO;
using System.Collections.Generic;
using System.Threading;
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

            AudioType? audioType = GetAudioFileExtension(filePath);
            if (!audioType.HasValue)
                yield break;

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(FileHelpers.GetEscapedURLForFilePath(filePath), audioType.Value))
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

        /// <summary>
        /// Load an audio clip (OGG/WAV only). Should not be used in the main thread, as it blocks until the load operation is complete.
        /// </summary>
        /// <param name="filePath">Path to audio file.</param>
        /// <returns>AudioClip on successful load, otherwise null.</returns>
        public static AudioClip LoadAudioClip(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            AudioType? audioType = GetAudioFileExtension(filePath);
            if (!audioType.HasValue)
                return null;

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(FileHelpers.GetEscapedURLForFilePath(filePath), audioType.Value))
            {
                ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                SpinWait.SpinUntil(() => operation.isDone);

                if (request.isNetworkError || request.isHttpError)
                    return null;
                else
                    return DownloadHandlerAudioClip.GetContent(request);
            }
        }

        private static AudioType? GetAudioFileExtension(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();

            switch (fileExtension)
            {
                case ".wav":
                    return AudioType.WAV;
                case ".egg":
                    goto case ".ogg";
                case ".ogg":
                    return AudioType.OGGVORBIS;
                default:
                    return null;
            }
        }
    }
}
