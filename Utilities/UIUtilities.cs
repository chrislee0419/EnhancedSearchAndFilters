using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SongCore;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;
using BSUtilsUtilities = BS_Utils.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.Utilities
{
    internal static class UIUtilities
    {
        /// <summary>
        /// Default colour of RoundRectPanel image.
        /// </summary>
        public static readonly Color RoundRectDefaultColour = new Color(0f, 0f, 0f, 0.25f);
        /// <summary>
        /// Darker colour for RoundRectPanel image.
        /// </summary>
        public static readonly Color RoundRectDarkColour = new Color(0f, 0f, 0f, 0.5f);
        /// <summary>
        /// Colour used by most highlighted elements.
        /// </summary>
        public static readonly Color LightBlueHighlightedColour = new Color(0.025f, 0.415f, 0.670f);
        /// <summary>
        /// Colour used in text and the chevron in dropdown components.
        /// </summary>
        public static readonly Color LightBlueElementColour = new Color(0f, 0.75f, 1f);

        public static Sprite DefaultCoverImage => Loader.defaultCoverImage;

        private static Material _noGlowMaterial;
        public static Material NoGlowMaterial
        {
            get
            {
                if (_noGlowMaterial == null)
                {
                    _noGlowMaterial = new Material(Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlow"));
                    _noGlowMaterial.color = new Color(1f, 1f, 1f, 1f);

                }
                return _noGlowMaterial;
            }
        }

        private static Sprite _roundRectPanelSprite;
        public static Sprite RoundRectPanelSprite
        {
            get
            {
                if (_roundRectPanelSprite == null)
                {
                    _roundRectPanelSprite = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Image>().First(x => x.name == "LevelInfo" && x.sprite?.name == "RoundRectPanel").sprite);
                    _roundRectPanelSprite.name = "ESAFRoundRectPanel";
                }
                return _roundRectPanelSprite;
            }
        }

        private static Sprite _crossSprite;
        public static Sprite CrossSprite
        {
            get
            {
                if (_crossSprite == null)
                    _crossSprite = BSUtilsUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.cross.png");
                return _crossSprite;
            }
        }

        private static Sprite _checkmarkSprite;
        public static Sprite CheckmarkSprite
        {
            get
            {
                if (_checkmarkSprite == null)
                    _checkmarkSprite = BSUtilsUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.checkmark.png");
                return _checkmarkSprite;
            }
        }

        private static Sprite _blankSprite;
        public static Sprite BlankSprite
        {
            get
            {
                if (_blankSprite == null)
                    _blankSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                return _blankSprite;
            }
        }

        private static GameObject _loadingSpinnerPrefab;

        public static GameObject CreateLoadingSpinner(Transform parent)
        {
            // copied from CustomUI, since BSML doesn't currently create loading spinners
            // https://github.com/williums/BeatSaber-CustomUI/blob/master/BeatSaber/BeatSaberUI.cs
            if (_loadingSpinnerPrefab == null)
                _loadingSpinnerPrefab = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "LoadingIndicator").First();

            var loadingSpinner = GameObject.Instantiate(_loadingSpinnerPrefab, parent, false);
            loadingSpinner.name = "LoadingSpinner";

            return loadingSpinner;
        }

        public static BSMLParserParams ParseBSML(string resource, GameObject parent, object host = null)
        {
            return BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), resource), parent, host);
        }

        /// <summary>
        /// Scales the size of a button. Useful for 'unstretching' small buttons. Defaults to scaling down the button by 0.75.
        /// </summary>
        /// <param name="btn">Button to scale.</param>
        /// <param name="scalingFactor">How much to scale the button by.</param>
        public static void ScaleButton(Button btn, float scalingFactor = 0.75f)
        {
            btn.GetComponentInChildren<TextMeshProUGUI>().fontSize *= 1f / scalingFactor;
            ScaleRectTransform(btn.transform as RectTransform, scalingFactor);
        }

        /// <summary>
        /// Scales the size of a RectTransform. Defaults to scaling down the RectTransform by 0.75. 
        /// (NOTE: keep in mind that all children will be scaled as well).
        /// </summary>
        /// <param name="rt">RectTransform to scale.</param>
        /// <param name="scalingFactor">How much to scale the RectTransform by.</param>
        /// <param name="scaleChildren">Scale children TextMeshProUGUI font sizes and RectTransform size deltas.</param>
        public static void ScaleRectTransform(RectTransform rt, float scalingFactor = 0.75f, bool scaleChildren = false)
        {
            rt.localScale *= scalingFactor;

            var layout = rt.GetComponent<LayoutElement>();
            float inverseScale = 1f / scalingFactor;
            if (layout != null)
            {
                layout.preferredWidth *= inverseScale;
                layout.preferredHeight *= inverseScale;
            }
            else
            {
                rt.sizeDelta *= inverseScale;
            }

            if (scaleChildren)
            {
                foreach (var childLayout in rt.GetComponentsInChildren<LayoutElement>())
                {
                    if (childLayout == layout)
                        continue;

                    childLayout.preferredWidth *= inverseScale;
                    childLayout.preferredHeight *= inverseScale;
                }

                foreach (var childRT in rt.GetComponentsInChildren<RectTransform>())
                {
                    if (childRT == rt || childRT.GetComponent<LayoutElement>() != null)
                        continue;

                    childRT.sizeDelta *= inverseScale;
                }

                foreach (var text in rt.GetComponentsInChildren<TextMeshProUGUI>())
                    text.fontSize *= inverseScale;
            }
        }

        private static WaitForEndOfFrame _wait = new WaitForEndOfFrame();
        /// <summary>
        /// Invoke an action after a short wait.
        /// </summary>
        /// <param name="action">Action to invoke after the wait.</param>
        /// <param name="framesToWait">The number of frames to wait.</param>
        /// <param name="waitForEndOfFrame">True to wait for the end of the frame. False to wait for the next frame</param>
        /// <returns>An IEnumerator to be used by <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.</returns>
        public static IEnumerator DelayedAction(Action action, int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            if (action == null)
                yield break;

            WaitForEndOfFrame wait = waitForEndOfFrame ? _wait : null;
            while (framesToWait-- > 0)
                yield return wait;

            action.Invoke();
        }
    }
}
