using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.UI.Components.ButtonPanelModules
{
    [RequireComponent(typeof(RectTransform))]
    internal class SortModeModule : MonoBehaviour
    {
        public event Action SortButtonPressed;

        public RectTransform RectTransform { get; private set; }

#pragma warning disable CS0649
        [UIComponent("default-sort-button")]
        private Button _defaultSortButton;
        [UIComponent("newest-sort-button")]
        private Button _newestSortButton;
        [UIComponent("play-count-sort-button")]
        private Button _playCountSortButton;
#pragma warning restore CS0649

        private Image _defaultSortButtonStrokeImage;
        private Image _newestSortButtonStrokeImage;
        private Image _playCountSortButtonStrokeImage;

        private static readonly Color DefaultSortButtonColor = Color.white;
        private static readonly Color SelectedSortButtonColor = new Color(0.7f, 1f, 0.6f);
        private static readonly Color SelectedReversedSortButtonColor = new Color(0.7f, 0.6f, 1f);

        private void Awake()
        {
            RectTransform = this.GetComponent<RectTransform>();
        }

        private void Start()
        {
            Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.ButtonPanelModules.SortModeView.bsml", this.gameObject, this);

            _defaultSortButtonStrokeImage = _defaultSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");
            _newestSortButtonStrokeImage = _newestSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");
            _playCountSortButtonStrokeImage = _playCountSortButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke");

            _defaultSortButtonStrokeImage.color = SelectedSortButtonColor;
        }

        [UIAction("default-sort-button-clicked")]
        private void OnDefaultSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.Default;
            UpdateSortButtons();
            Logger.log.Debug("Default sort button pressed");

            SortButtonPressed?.Invoke();
        }

        [UIAction("newest-sort-button-clicked")]
        private void OnNewestSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.Newest;
            UpdateSortButtons();
            Logger.log.Debug("Newest sort button pressed");

            SortButtonPressed?.Invoke();
        }

        [UIAction("play-count-sort-button-clicked")]
        private void OnPlayCountSortButtonClicked()
        {
            SongSortModule.CurrentSortMode = SortMode.PlayCount;
            UpdateSortButtons();
            Logger.log.Debug("Play Count sort button pressed");

            SortButtonPressed?.Invoke();
        }

        public void UpdateSortButtons()
        {
            if (_defaultSortButton == null || _newestSortButton == null || _playCountSortButton == null)
                return;

            switch (SongSortModule.CurrentSortMode)
            {
                case SortMode.Default:
                    _defaultSortButtonStrokeImage.color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    _newestSortButtonStrokeImage.color = DefaultSortButtonColor;
                    _playCountSortButtonStrokeImage.color = DefaultSortButtonColor;
                    break;

                case SortMode.Newest:
                    _defaultSortButtonStrokeImage.color = DefaultSortButtonColor;
                    _newestSortButtonStrokeImage.color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    _playCountSortButtonStrokeImage.color = DefaultSortButtonColor;
                    break;

                case SortMode.PlayCount:
                    _defaultSortButtonStrokeImage.color = DefaultSortButtonColor;
                    _newestSortButtonStrokeImage.color = DefaultSortButtonColor;
                    _playCountSortButtonStrokeImage.color = SongSortModule.Reversed ? SelectedReversedSortButtonColor : SelectedSortButtonColor;
                    break;
            }
        }
    }
}
