using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.UI.Components;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class FilterSideViewController : HotReloadableViewController
    //internal class FilterSideViewController : BSMLResourceViewController
    {
        public override string ResourceName => "EnhancedSearchAndFilters.UI.Views.FilterSideView.bsml";
        public override string ContentFilePath => "E:\\Projects\\EnhancedSearchAndFilters\\UI\\Views\\FilterSideView.bsml";

        public event Action<IFilter> FilterSelected;
        public event Action ClearButtonPressed;
        public event Action DefaultButtonPressed;

        private bool _applyQuickFilterButtonInteractibility = false;
        [UIValue("apply-quick-filter-button-interactivity")]
        public bool ApplyQuickFilterButtonInteractability
        {
            get => _applyQuickFilterButtonInteractibility;
            set
            {
                if (_applyQuickFilterButtonInteractibility == value)
                    return;

                _applyQuickFilterButtonInteractibility = value;
                NotifyPropertyChanged();
            }
        }
        private bool _deleteQuickFilterButtonInteractivity = false;
        [UIValue("delete-quick-filter-button-interactivity")]
        public bool DeleteQuickFilterButtonInteractivity
        {
            get => _deleteQuickFilterButtonInteractivity;
            set
            {
                if (_deleteQuickFilterButtonInteractivity == value)
                    return;

                _deleteQuickFilterButtonInteractivity = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("filter-cell-list")]
        private List<object> _filterCellList = new List<object>();

#pragma warning disable CS0649
        [UIComponent("clear-button")]
        private Button _clearButton;
        [UIComponent("default-button")]
        private Button _defaultButton;
        [UIComponent("filter-list")]
        private CustomCellListTableData _tableData;

        [UIObject("quick-filter-dropdown-container")]
        private GameObject _quickFilterDropdownContainer;

        [UIComponent("quick-filter-dropdown-text")]
        private TextMeshProUGUI _quickFilterDropdownText;
        [UIComponent("quick-filter-list")]
        private CustomListTableData _quickFilterTableData;
#pragma warning restore CS0649

        [UIParams]
        private BSMLParserParams _parserParams;

        [UIValue("legend-text")]
        private const string LegendText =
            "<b>Filter Color Legend</b>\n\n" +
            "<color=#FF5555>Red</color>  -  Not applied\n" +
            "<color=#FFFF55>Yellow</color>  -  Not applied, but has changes\n" +
            "<color=#55FF55>Green</color>  -  Applied\n" +
            "<color=#55AAFF>Blue</color>  -  Applied, but has changes";
        [UIValue("save-quick-filter-button-text")]
        private const string SaveQuickFilterButtonText = "Save Settings\nTo Quick Filter";

        private class FilterTableCell
        {
            public IFilter AssociatedFilter { get; private set; }

#pragma warning disable CS0649
            [UIComponent("status-image")]
            private RawImage _statusImg;
            [UIComponent("hovered-image")]
            private RawImage _hoveredImg;
            [UIComponent("selected-image")]
            private RawImage _selectedImg;

            [UIComponent("text")]
            private TextMeshProUGUI _text;
#pragma warning restore CS0649

            private static readonly Color DefaultFilterColor = new Color(1f, 0.2f, 0.2f);
            private static readonly Color PendingFilterColor = new Color(1f, 1f, 0f);
            private static readonly Color AppliedFilterColor = new Color(0.2f, 1f, 0.2f);
            private static readonly Color AppliedPendingFilterColor = new Color(0.2f, 0.5f, 1f);

            public FilterTableCell(IFilter filter)
            {
                AssociatedFilter = filter;
            }

            public void RefreshCellContent()
            {
                if (AssociatedFilter == null || _statusImg == null)
                {
                    Logger.log.Warn("Unable to refresh filter TableView cell content");
                    return;
                }

                // raw image setup (will probably need to update this in the future, since the bsml image tag
                // is likely to change in the future)
                _statusImg.texture = Texture2D.whiteTexture;
                _hoveredImg.texture = Texture2D.whiteTexture;
                _selectedImg.texture = Texture2D.whiteTexture;

                _hoveredImg.color = new Color(0.1f, 0.6f, 1.0f);
                _selectedImg.color = new Color(0f, 0.3f, 0.7f);

                if (AssociatedFilter.IsAvailable)
                {
                    string statusText = "";
                    if (AssociatedFilter.Status == FilterStatus.Applied)
                        statusText = " <size=70%><color=#CCFFCC>[A]</color></size>";
                    else if (AssociatedFilter.Status == FilterStatus.AppliedAndChanged || AssociatedFilter.Status == FilterStatus.NotAppliedAndChanged)
                        statusText = " <size=70%><color=#FFFFCC>[*]</color></size>";

                    _text.text = AssociatedFilter.Name + statusText;
                }
                else
                {
                    _text.text = $"<color=#FF8888><i>{AssociatedFilter.Name}</i></color>";
                }

                if (AssociatedFilter.Status == FilterStatus.NotApplied)
                    _statusImg.color = DefaultFilterColor;
                else if (AssociatedFilter.Status == FilterStatus.NotAppliedAndChanged)
                    _statusImg.color = PendingFilterColor;
                else if (AssociatedFilter.Status == FilterStatus.AppliedAndChanged)
                    _statusImg.color = AppliedPendingFilterColor;
                else
                    _statusImg.color = AppliedFilterColor;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            if (firstActivation)
            {
                this.name = "FilterSideViewController";

                _tableData.gameObject.GetComponentInChildren<ScrollRect>().vertical = false;

                var eventHandler = _quickFilterDropdownContainer.AddComponent<EnterExitEventHandler>();
                eventHandler.PointerEntered += delegate ()
                {
                    Logger.log.Warn("pointer entered quick filter dropdown container");
                    // TODO: do something with the background and text
                };
                eventHandler.PointerExited += delegate ()
                {
                    Logger.log.Warn("pointer exited quick filter dropdown container");
                    // TODO: do somoething with the background and text
                };
            }

            SetFilterList();

            SetButtonInteractivity(false, false);
            SetFilterListVisibility(false);
        }

        public void SetFilterList()
        {
            _filterCellList.Clear();

            foreach (var filter in FilterList.ActiveFilters)
                _filterCellList.Add(new FilterTableCell(filter));

            if (_tableData != null)
            {
                _tableData.data = _filterCellList;
                _tableData.tableView.ReloadData();
            }
        }

        public void SelectCell(int index)
        {
            if (_tableData == null || index < 0 || index >= _filterCellList.Count)
                return;

            _tableData.tableView.SelectCellWithIdx(index);
        }

        public void SetButtonInteractivity(bool clearInteractable, bool defaultInteractable)
        {
            _clearButton.interactable = clearInteractable;
            _defaultButton.interactable = defaultInteractable;
        }

        public void SetFilterListVisibility(bool visible)
        {
            if (_tableData == null)
                return;

            _tableData.gameObject.SetActive(visible);
        }

        public void RefreshFilterList()
        {
            foreach (var cell in _filterCellList)
                (cell as FilterTableCell).RefreshCellContent();

        }

        [UIAction("filter-cell-selected")]
        private void FilterCellSelected(TableView tableView, object selectedCell)
        {
            FilterSelected?.Invoke((selectedCell as FilterTableCell).AssociatedFilter);
        }

        [UIAction("clear-button-clicked")]
        private void OnClearButtonClicked()
        {
            _clearButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            ClearButtonPressed?.Invoke();
        }

        [UIAction("default-button-clicked")]
        private void OnDefaultButtonClicked()
        {
            _defaultButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            DefaultButtonPressed?.Invoke();
        }

        [UIAction("quick-filter-dropdown-clicked")]
        private void OnQuickFilterDropdownButtonClicked()
        {
            Logger.log.Warn("quick filter dropdown clicked");
            // TODO

            _parserParams.EmitEvent("show-modal");
        }

        [UIAction("save-quick-filter-button-clicked")]
        private void OnSaveQuickFilterButtonClicked()
        {
            Logger.log.Warn("save quick filter clicked");
            // TODO
        }
    }
}
