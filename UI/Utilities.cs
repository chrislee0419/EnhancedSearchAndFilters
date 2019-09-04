using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CustomUI.Settings;
using CustomUI.Utilities;
using Object = UnityEngine.Object;

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
                    _noGlowMaterial = new Material(UIUtilities.NoGlowMaterial);
                    _noGlowMaterial.color = new Color(1f, 1f, 1f, 1f);

                }
                return _noGlowMaterial;
            }
        }
        private static Material _noGlowMaterial;

        private static SubMenu _submenu = new SubMenu((Transform)null);

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

        /// <summary>
        /// Creates a white, horizontal divider with the default width delta of -4f.
        /// </summary>
        /// <param name="parent">The transform to parent the divider to.</param>
        /// <param name="placeBottomEdge">true (default): place the divider on the bottom edge, false: place the divider on the top edge</param>
        /// <returns>An Image object representing the divider.</returns>
        public static Image CreateHorizontalDivider(Transform parent, bool placeBottomEdge = true)
        {
            return CreateHorizontalDivider(parent, -4f, placeBottomEdge);
        }

        /// <summary>
        /// Creates a white, horizontal divider.
        /// </summary>
        /// <param name="parent">The transform to parent the divider to.</param>
        /// <param name="widthDelta">The difference in width of the divider, compared to the parent.</param>
        /// <param name="placeBottomEdge">true (default): place the divider on the bottom edge, false: place the divider on the top edge.</param>
        /// <returns>An Image object representing the divider.</returns>
        public static Image CreateHorizontalDivider(Transform parent, float widthDelta, bool placeBottomEdge = true)
        {
            var divider = new GameObject("Divider").AddComponent<Image>();
            divider.color = new Color(1f, 1f, 1f, 0.15f);
            divider.material = NoGlowMaterial;

            var rt = divider.rectTransform;
            rt.SetParent(parent);
            if (placeBottomEdge)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(1f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = Vector2.one;
            }
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(widthDelta, 0.1f);
            rt.anchoredPosition = Vector2.zero;

            return divider;
        }

        /// <summary>
        /// Create a BoolViewController outside of the settings menu. 
        /// NOTE: CustomUI.Settings.SubMenu:AddHooks() can make a call to ApplySettings.
        /// </summary>
        /// <param name="name">The text to be displayed for this setting.</param>
        /// <param name="hintText">The text to be displayed when this settings is hovered over.</param>
        /// <returns>A new BoolViewController.</returns>
        public static BoolViewController CreateBoolViewController(string name, string hintText = "")
        {
            BoolViewController viewController = _submenu.AddBool(name, hintText);

            // remove this view controller from the global list of CustomSettings, otherwise it'll try to call Init() when we don't need it to
            SubMenu.needsInit.Remove(viewController);

            return viewController;
        }

        /// <summary>
        /// Create a ListViewController outside of the settings menu. 
        /// NOTE: CustomUI.Settings.SubMenu:AddHooks() can make a call to ApplySettings.
        /// </summary>
        /// <param name="name">The text to be displayed for this setting.</param>
        /// <param name="values">A list of values representing an option in this setting.</param>
        /// <param name="hintText">The text to be displayed when this settings is hovered over.</param>
        /// <returns>A new ListViewController.</returns>
        public static ListViewController CreateListViewController(string name, float[] values, string hintText = "")
        {
            ListViewController viewController = _submenu.AddList(name, values, hintText);

            // remove this view controller from the global list of CustomSettings, otherwise it'll try to call Init() when we don't need it to
            SubMenu.needsInit.Remove(viewController);

            return viewController;
        }

        /// <summary>
        /// Moves the decrement button, value, increment button, and toggle (if it exists) of a ListViewController outside of the "Value" transform.
        /// </summary>
        /// <param name="controller">A ListViewController that represents a filter's control's UI element.</param>
        public static void MoveListViewControllerElements(ListViewController controller)
        {
            MoveIncDecViewControllerElements(controller);

            // for min/max ListViewControllers that have an enable toggle
            if (controller.transform.Find("Value/MinValueToggle") != null || controller.transform.Find("Value/MaxValueToggle") != null)
            {
                var toggle = controller.transform.Find("Value/MinValueToggle") as RectTransform ?? controller.transform.Find("Value/MaxValueToggle") as RectTransform;
                toggle.SetParent(controller.transform);
                toggle.anchorMin = new Vector2(1f, 0.5f);
                toggle.anchorMax = new Vector2(1f, 0.5f);
                toggle.pivot = new Vector2(1f, 0.5f);
                toggle.sizeDelta = new Vector2(8f, 8f);
                toggle.anchoredPosition = new Vector2(-34f, 0f);
            }
        }

        /// <summary>
        /// Moves the decrement button, value, and increment button of an IncDecSettingsController outside of the "Value" transform. 
        /// This method is here for consistency of UI elements and because the "Value" transform has some forced horizontal layout that messes up child RectTransform positioning.
        /// </summary>
        /// <param name="controller">An IncDecSettingsController that represents a filter's control's UI element.</param>
        public static void MoveIncDecViewControllerElements(IncDecSettingsController controller)
        {
            var incButton = controller.transform.Find("Value/IncButton") as RectTransform;
            incButton.SetParent(controller.transform);
            incButton.anchorMin = new Vector2(1f, 0.5f);
            incButton.anchorMax = new Vector2(1f, 0.5f);
            incButton.pivot = new Vector2(1f, 0.5f);
            incButton.sizeDelta = new Vector2(8f, 8f);
            incButton.anchoredPosition = Vector2.zero;

            var text = controller.transform.Find("Value/ValueText") as RectTransform;
            text.SetParent(controller.transform);
            text.anchorMin = new Vector2(1f, 0.5f);
            text.anchorMax = new Vector2(1f, 0.5f);
            text.pivot = new Vector2(1f, 0.5f);
            text.sizeDelta = new Vector2(16f, 8f);
            text.anchoredPosition = new Vector2(-8f, 0f);

            var decButton = controller.transform.Find("Value/DecButton") as RectTransform;
            decButton.SetParent(controller.transform);
            decButton.anchorMin = new Vector2(1f, 0.5f);
            decButton.anchorMax = new Vector2(1f, 0.5f);
            decButton.pivot = new Vector2(1f, 0.5f);
            decButton.sizeDelta = new Vector2(8f, 8f);
            decButton.anchoredPosition = new Vector2(-24f, 0f);
        }
    }
}
