using System;
using UnityEngine;
using HMUI;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SongDetailsViewController : ViewController
    {
        public Action<IPreviewBeatmapLevel> SelectButtonPressed;
        public Action CompactKeyboardButtonPressed;

        private IPreviewBeatmapLevel _level;
        private SongDetailsDisplay _songDetailsDisplay;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                this.name = "SongDetailsViewController";

                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                this.rectTransform.anchoredPosition = Vector2.zero;
                this.rectTransform.sizeDelta = new Vector2(80f, 0f);

                _songDetailsDisplay = new GameObject("SongDetailsDisplay").AddComponent<SongDetailsDisplay>();
                _songDetailsDisplay.transform.SetParent(this.transform, false);
                _songDetailsDisplay.rectTransform.anchorMin = Vector2.zero;
                _songDetailsDisplay.rectTransform.anchorMax = Vector2.one;
                _songDetailsDisplay.rectTransform.anchoredPosition = Vector2.zero;
                _songDetailsDisplay.rectTransform.sizeDelta = new Vector2(-6f, -8f);

                _songDetailsDisplay.SelectButtonPressed += () => SelectButtonPressed?.Invoke(_level);
                _songDetailsDisplay.KeyboardButtonPressed += () => CompactKeyboardButtonPressed?.Invoke();
            }
            else
            {
                _songDetailsDisplay.KeyboardButtonActive = PluginConfig.CompactSearchMode;
            }
        }

        public void SetContent(IPreviewBeatmapLevel level)
        {
            _level = level;
            _songDetailsDisplay.SetContent(level);
        }
    }
}
