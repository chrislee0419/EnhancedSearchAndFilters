using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class FilterSettingsViewController : VRUIViewController
    {
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                Image img = new GameObject("Background").AddComponent<Image>();
                img.transform.SetParent(this.transform, false);
                img.rectTransform.anchorMin = Vector2.zero;
                img.rectTransform.anchorMax = Vector2.one;
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                img.rectTransform.sizeDelta = Vector2.zero;
                img.rectTransform.anchoredPosition = Vector2.zero;
                img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                img.color = new Color(0f, 0f, 0f, 0.3f);
            }
        }
    }
}
