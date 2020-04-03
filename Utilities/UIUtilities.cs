using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SongCore;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.Utilities
{
    internal static class UIUtilities
    {
        /// <summary>
        /// Default colour of RoundRectPanel image.
        /// </summary>
        public static readonly Color RoundRectDefaultColour = new Color(0f, 0f, 0f, 0.25f);
        /// <summary>
        /// Colour used by most highlighted elements.
        /// </summary>
        public static readonly Color LightBlueHighlightedColour = new Color(0.025f, 0.415f, 0.670f);
        /// <summary>
        /// Colour used in text and the chevron in dropdown components.
        /// </summary>
        public static readonly Color LightBlueElementColour = new Color(0f, 0.75f, 1f);

        public static Sprite DefaultCoverImage => Loader.defaultCoverImage;

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
        private static Material _noGlowMaterial;

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
        public static void ScaleRectTransform(RectTransform rt, float scalingFactor = 0.75f)
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
        }
    }
}
