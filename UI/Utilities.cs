using System.Linq;
using UnityEngine;

namespace EnhancedSearchAndFilters.UI
{
    internal static class Utilities
    {
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
    }
}
