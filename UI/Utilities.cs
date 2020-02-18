using System.Linq;
using System.Reflection;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.UI
{
    internal static class Utilities
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
    }
}
