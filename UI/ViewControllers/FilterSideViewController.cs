using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using EnhancedSearchAndFilters.Filters;
using Image = UnityEngine.UI.Image;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class FilterSideViewController : BSMLResourceViewController
    {
        public override string ResourceName => "EnhancedSearchAndFilters.UI.Views.FilterSideView.bsml";

        public event Action<IFilter> FilterSelected;
        public event Action ClearButtonPressed;
        public event Action DefaultButtonPressed;

#pragma warning disable CS0649
        [UIValue("filter-cell-list")]
        private List<object> _filterCellList;

        [UIComponent("clear-button")]
        private Button _clearButton;
        [UIComponent("default-button")]
        private Button _defaultButton;
#pragma warning restore CS0649

        [UIValue("cell-template")]
        private const string ListCellTemplate =
            "<bg anchor-min-x='0' anchor-min-y='0' anchor-max-x='1' anchor-max-y='1' size-delta-x='0' size-delta-y='0'>\n" +
            "<image id='status-image' anchor-min-x='0' anchor-min-y='0' anchor-max-x='0' anchor-max-y='1' size-delta-x='3' size-delta-y='0' anchored-pos-x='1.5' anchored-pos-y='0' />\n" +
            "<text id='text' font-align='Left' font-size='4.5' anchor-min-x='0' anchor-min-y='0' anchor-max-x='1' anchor-max-y='1' size-delta-x='-5' size-delta-y='0' anchored-pos-x='1' anchored-pos-y='0' />\n" +
            "</bg>";
        [UIValue("legend-text")]
        private const string LegendText =
            "<b><i>Filter Color Legend<i><b>\n" +
            "<color=#FF5555>Red</color> -  Not applied\n" +
            "<color=#FFFF55>Yellow</color> -  Not applied, but has changes\n" +
            "<color=#55FF55>Green</color> -  Applied\n" +
            "<color=#55AAFF>Blue</color> -  Applied, but has changes";

        private class FilterTableCell
        {
            public IFilter AssociatedFilter { get; private set; }

#pragma warning disable CS0649
            [UIComponent("text")]
            private TextMeshProUGUI _text;
            [UIComponent("status-image")]
            private Image _statusImg;
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

                    _text.text = AssociatedFilter.Name + statusText;
                }
                else
                {
                    _text.text = $"<color=#FF8888><i>{AssociatedFilter.Name}</i></color>";
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
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            SetButtonInteractivity(false, false);
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
