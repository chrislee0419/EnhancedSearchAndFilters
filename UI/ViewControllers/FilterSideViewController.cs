using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using EnhancedSearchAndFilters.Filters;
using Image = UnityEngine.UI.Image;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class FilterSideViewController : BSMLResourceViewController
    {
        public override string ResourceName => "EnhancedSearchAndFilters.UI.Views.FilterSideView.bsml";

        public event Action<IFilter> FilterSelected;
        public event Action ClearButtonPressed;
        public event Action DefaultButtonPressed;
        [UIValue("filter-cell-list")]
        private List<object> _filterCellList = new List<object>();

#pragma warning disable CS0649
        [UIComponent("filter-list-data")]
        private CustomCellListTableData _tableData;

        [UIComponent("clear-button")]
        private Button _clearButton;
        [UIComponent("default-button")]
        private Button _defaultButton;
#pragma warning restore CS0649

        [UIValue("list-cell-template")]
        private static readonly string ListCellTemplate =
            BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.FilterListCellTemplate.bsml");
        [UIValue("legend-text")]
        private const string LegendText =
            "<b><i>Filter Color Legend</i></b>\n" +
            "<color=#FF5555>Red</color> -  Not applied\n" +
            "<color=#FFFF55>Yellow</color> -  Not applied, but has changes\n" +
            "<color=#55FF55>Green</color> -  Applied\n" +
            "<color=#55AAFF>Blue</color> -  Applied, but has changes";

        private class FilterTableCell : INotifiableHost
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public IFilter AssociatedFilter { get; private set; }

#pragma warning disable CS0649
            [UIComponent("status-image")]
            private Image _statusImg;
            [UIComponent("hovered-image")]
            private Image _hoveredImg;
            [UIComponent("selected-image")]
            private Image _selectedImg;
#pragma warning restore CS0649

            [UIValue("text")]
            private string _text;
            public string Text
            {
                get => _text;
                set
                {
                    if (_text == value)
                        return;
                    _text = value;
                    NotifyPropertyChanged();
                }
            }

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
                if (AssociatedFilter == null || _text == null || _statusImg == null)
                {
                    Logger.log.Warn("Unable to refresh filter TableView cell content");
                    return;
                }

                if (AssociatedFilter.IsAvailable)
                {
                    string statusText = "";
                    if (AssociatedFilter.Status == FilterStatus.Applied)
                        statusText = " <size=70%><color=#CCFFCC>[A]</color></size>";
                    else if (AssociatedFilter.Status == FilterStatus.AppliedAndChanged || AssociatedFilter.Status == FilterStatus.NotAppliedAndChanged)
                        statusText = " <size=70%><color=#FFFFCC>(*)</color></size>";

                    Text = AssociatedFilter.Name + statusText;
                }
                else
                {
                    Text = $"<color=#FF8888><i>{AssociatedFilter.Name}</i></color>";
                }

                if (AssociatedFilter.Status == FilterStatus.NotAppliedAndDefault)
                    _statusImg.color = DefaultFilterColor;
                else if (AssociatedFilter.Status == FilterStatus.NotAppliedAndChanged)
                    _statusImg.color = PendingFilterColor;
                else if (AssociatedFilter.Status == FilterStatus.AppliedAndChanged)
                    _statusImg.color = AppliedPendingFilterColor;
                else
                    _statusImg.color = AppliedFilterColor;
            }

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error Invoking PropertyChanged: {ex.Message}");
                    Logger.log?.Error(ex);
                }
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);
            this.name = "FilterSideViewController";

            SetButtonInteractivity(false, false);
        }

        public void SetFilterList(List<IFilter> filterList)
        {
            _filterCellList.Clear();

            foreach (var filter in filterList)
                _filterCellList.Add(new FilterTableCell(filter));
        }

        public void SetButtonInteractivity(bool clearInteractable, bool defaultInteractable)
        {
            _clearButton.interactable = clearInteractable;
            _defaultButton.interactable = defaultInteractable;
        }

        public void RefreshFilterList()
        {
            foreach (var cell in _filterCellList)
                (cell as FilterTableCell).RefreshCellContent();
            _tableData.tableView.ReloadData();

        }

        [UIAction("filter-cell-selected")]
        private void FilterCellSelected(TableView tableView, object selectedCell)
        {
            FilterSelected?.Invoke((selectedCell as FilterTableCell).AssociatedFilter);
        }

        [UIAction("clear-button-clicked")]
        private void OnClearButtonClicked()
        {
            ClearButtonPressed?.Invoke();
        }

        [UIAction("default-button-clicked")]
        private void OnDefaultButtonClicked()
        {
            DefaultButtonPressed?.Invoke();
        }
    }
}
