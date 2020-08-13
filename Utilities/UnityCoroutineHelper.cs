using System;
using System.Collections;
using UnityEngine;

namespace EnhancedSearchAndFilters.Utilities
{
    internal class UnityCoroutineHelper : PersistentSingleton<UnityCoroutineHelper>
    {
        private static WaitForEndOfFrame _wait = new WaitForEndOfFrame();
        /// <summary>
        /// Invoke an action after a short wait.
        /// </summary>
        /// <param name="action">Action to invoke after the wait.</param>
        /// <param name="framesToWait">The number of frames to wait.</param>
        /// <param name="waitForEndOfFrame">True to wait for the end of the frame. False to wait for the next frame</param>
        /// <returns>A <see cref="Coroutine"/> representing the wait.</returns>
        public static Coroutine StartDelayedAction(Action action, int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            if (action == null)
                return null;
            else
                return instance.StartCoroutine(DelayedActionCoroutine(action, framesToWait, waitForEndOfFrame));
        }

        public static IEnumerator DelayedActionCoroutine(Action action, int framesToWait, bool waitForEndOfFrame)
        {
            WaitForEndOfFrame wait = waitForEndOfFrame ? _wait : null;
            while (framesToWait-- > 0)
                yield return wait;

            action.Invoke();
        }

        public static Coroutine Start(IEnumerator coroutine)
        {
            if (coroutine == null)
                return null;
            else
                return instance.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine) => instance.StopCoroutine(coroutine);
    }
}
