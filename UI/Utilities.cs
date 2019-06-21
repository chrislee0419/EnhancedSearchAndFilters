using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CustomUI.Utilities;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.UI
{
    internal static class Utilities
    {
        /// <summary>
        /// Gets the prefab that should be passed to CreateToggleFromPrefab. 
        /// Should be used like this: 'CreateToggleFromPrefab(prefab.toggle, transform)'. 
        /// You should manually destroy this prefab after creating all your toggles.
        /// </summary>
        /// <returns>A GameplayModifierToggle that contains the toggle prefab.</returns>
        public static GameplayModifierToggle GetTogglePrefab()
        {
            var togglePrefab = Object.Instantiate(Resources.FindObjectsOfTypeAll<GameplayModifierToggle>().Last());
            Object.Destroy(togglePrefab.GetPrivateField<HoverHint>("_hoverHint"));

            return togglePrefab;
        }

        /// <summary>
        /// Create a toggle UI element. The prefab can be obtained through GetTogglePrefab().
        /// </summary>
        /// <param name="prefab">An existing toggle to instantiate from.</param>
        /// <param name="parent">Transform to parent the newly created toggle.</param>
        /// <returns>The newly created toggle.</returns>
        public static Toggle CreateToggleFromPrefab(Toggle prefab, Transform parent, float toggleSize = 6f)
        {
            var toggle = Object.Instantiate(prefab, parent, false);

            Object.Destroy(toggle.transform.Find("Icon").gameObject);
            Object.Destroy(toggle.transform.Find("Text").gameObject);

            Navigation nav = toggle.navigation;
            nav.mode = Navigation.Mode.None;
            toggle.navigation = nav;

            toggle.onValueChanged.RemoveAllListeners();

            var rt = toggle.transform.Find("BG") as RectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(toggleSize, toggleSize);
            rt.anchoredPosition = Vector2.zero;

            return toggle;
        }
    }
}
