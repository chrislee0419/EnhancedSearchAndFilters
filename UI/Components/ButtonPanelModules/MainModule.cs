using System;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;

namespace EnhancedSearchAndFilters.UI.Components.ButtonPanelModules
{
    [RequireComponent(typeof(RectTransform))]
	internal class MainModule : MonoBehaviour
    {
        public event Action SearchButtonPressed;
        public event Action FilterButtonPressed;
        public event Action ClearFilterButtonPressed;

        public RectTransform RectTransform { get; private set; }

        private bool _areFiltersApplied = false;

#pragma warning disable CS0649
        [UIValue("hide-search")]
        private bool _disableSearchButton = false;
        [UIValue("hide-filters")]
        private bool _disableFilterButtons = false;

        [UIComponent("filter-button")]
        private Button _filterButton;
        [UIComponent("clear-filter-button")]
        private Button _clearFilterButton;
#pragma warning restore CS0649

        [UIValue("filter-button-default-text")]
        private const string FilterButtonDefaultText = "<color=#FFFFCC>Filter</color>";
        [UIValue("clear-filter-button-default-text")]
        private const string ClearFilterButtonDefaultText = "<color=#FFFFCC>Clear Filters</color>";
        private const string FilterButtonHighlightedText = "<color=#444400>Filter</color>";
        private const string FilterButtonAppliedText = "<color=#DDFFDD>Filter (Applied)</color>";
        private const string FilterButtonHighlightedAppliedText = "<color=#004400>Filter (Applied)</color>";
        private const string ClearFilterButtonHighlightedText = "<color=#444400>Clear Filters</color>";
        private const string ClearFilterButtonAppliedText = "<color=#FFDDDD>Clear Filters</color>";
        private const string ClearFilterButtonHighlightedAppliedText = "<color=#440000>Clear Filters</color>";

        private void Awake()
        {
            RectTransform = this.GetComponent<RectTransform>();

            _disableSearchButton = PluginConfig.DisableSearch;
            _disableFilterButtons = PluginConfig.DisableFilters;
        }

        private void Start()
        {
            Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.ButtonPanelModules.MainModuleView.bsml", this.gameObject, this);

            // add ability to check pointer enter/exit events to filter/clear buttons to change colour
            if (_filterButton != null)
            {
                _filterButton.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _filterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonHighlightedAppliedText : FilterButtonHighlightedText);
                handler.PointerExited += () => _filterButton.SetButtonText(_areFiltersApplied ? FilterButtonAppliedText : FilterButtonDefaultText);
            }
            if (_clearFilterButton != null)
            {
                _clearFilterButton.gameObject.AddComponent<EnterExitEventHandler>();
                var handler = _clearFilterButton.gameObject.GetComponent<EnterExitEventHandler>();

                handler.PointerEntered += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText);
                handler.PointerExited += () => _clearFilterButton.SetButtonText(_areFiltersApplied ? ClearFilterButtonAppliedText : ClearFilterButtonDefaultText);
            }
        }

        public void SetFilterStatus(bool filterApplied)
        {
            _areFiltersApplied = filterApplied;

            if (_filterButton != null)
                _filterButton.SetButtonText(filterApplied ? FilterButtonAppliedText : FilterButtonDefaultText);

            if (_clearFilterButton != null)
            {
                if (_clearFilterButton.GetComponent<EnterExitEventHandler>().IsPointedAt)
                    _clearFilterButton.SetButtonText(filterApplied ? ClearFilterButtonHighlightedAppliedText : ClearFilterButtonHighlightedText);
                else
                    _clearFilterButton.SetButtonText(filterApplied ? ClearFilterButtonAppliedText : ClearFilterButtonDefaultText);
            }
        }

        [UIAction("search-button-clicked")]
        private void OnSearchButtonClicked()
        {
            Logger.log.Debug("Search button presssed");
            SearchButtonPressed?.Invoke();
        }

        [UIAction("filter-button-clicked")]
        private void OnFilterButtonClicked()
        {
            Logger.log.Debug("Filter button pressed");
            FilterButtonPressed?.Invoke();
        }

        [UIAction("clear-filter-button-clicked")]
        private void OnClearFilterButtonClicked()
        {
            Logger.log.Debug("Clear Filter button pressed");
            ClearFilterButtonPressed?.Invoke();
            SetFilterStatus(false);
        }
    }
}
