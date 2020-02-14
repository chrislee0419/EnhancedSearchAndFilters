using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.UI.Components;
using Image = UnityEngine.UI.Image;
using UIUtilities = EnhancedSearchAndFilters.UI.Utilities;

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
        public event Action<QuickFilter> QuickFilterApplied;

        private bool _clearButtonInteractable = false;
        [UIValue("clear-button-interactable")]
        public bool ClearButtonInteractable
        {
            get => _clearButtonInteractable;
            set
            {
                if (_clearButtonInteractable == value)
                    return;

                _clearButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _defaultButtonInteractable = false;
        [UIValue("default-button-interactable")]
        public bool DefaultButtonInteractable
        {
            get => _defaultButtonInteractable;
            set
            {
                if (_defaultButtonInteractable == value)
                    return;

                _defaultButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _filterListActive = false;
        [UIValue("filter-list-active")]
        public bool FilterListActive
        {
            get => _filterListActive;
            set
            {
                if (_filterListActive == value)
                    return;

                _filterListActive = value;
                NotifyPropertyChanged();
            }
        }
        private bool _quickFilterSectionActive = false;
        [UIValue("quick-filter-section-active")]
        public bool QuickFilterSectionActive
        {
            get => _quickFilterSectionActive;
            set
            {
                if (_quickFilterSectionActive == value)
                    return;

                _quickFilterSectionActive = value;
                NotifyPropertyChanged();
            }
        }
        private string _quickFilterDropdownTextValue = QuickFilterDropdownEmptyText;
        [UIValue("quick-filter-dropdown-text-value")]
        public string QuickFilterDropdownTextValue
        {
            get => _quickFilterDropdownTextValue;
            set
            {
                if (_quickFilterDropdownTextValue == value)
                    return;

                _quickFilterDropdownTextValue = value;
                NotifyPropertyChanged();
            }
        }
        private string _deleteQuickFilterModalTextValue = "";
        [UIValue("delete-quick-filter-modal-text-value")]
        public string DeleteQuickFilterModalTextValue
        {
            get => _deleteQuickFilterModalTextValue;
            set
            {
                if (_deleteQuickFilterModalTextValue == value)
                    return;

                _deleteQuickFilterModalTextValue = value;
                NotifyPropertyChanged();
            }
        }
        private string _saveQuickFilterModalWarningText = "";
        [UIValue("save-quick-filter-modal-warning-text")]
        public string SaveQuickFilterModalWarningText
        {
            get => _saveQuickFilterModalWarningText;
            set
            {
                if (_saveQuickFilterModalWarningText == value)
                    return;

                _saveQuickFilterModalWarningText = value;
                NotifyPropertyChanged();
            }
        }
        private bool _applyQuickFilterButtonInteractable = false;
        [UIValue("apply-quick-filter-button-interactable")]
        public bool ApplyQuickFilterButtonInteractable
        {
            get => _applyQuickFilterButtonInteractable;
            set
            {
                if (_applyQuickFilterButtonInteractable == value)
                    return;

                _applyQuickFilterButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _deleteQuickFilterButtonInteractable = false;
        [UIValue("delete-quick-filter-button-interactable")]
        public bool DeleteQuickFilterButtonInteractable
        {
            get => _deleteQuickFilterButtonInteractable;
            set
            {
                if (_deleteQuickFilterButtonInteractable == value)
                    return;

                _deleteQuickFilterButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _saveQuickFilterButtonInteractable = false;
        [UIValue("save-quick-filter-button-interactable")]
        public bool SaveQuickFilterButtonInteractable
        {
            get => _saveQuickFilterButtonInteractable;
            set
            {
                if (_saveQuickFilterButtonInteractable == value)
                    return;

                _saveQuickFilterButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _modalSaveQuickFilterButtonInteractable = false;
        [UIValue("modal-save-quick-filter-button-interactable")]
        public bool ModalSaveQuickFilterButtonInteractable
        {
            get => _modalSaveQuickFilterButtonInteractable;
            set
            {
                if (_modalSaveQuickFilterButtonInteractable == value)
                    return;

                _modalSaveQuickFilterButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        private QuickFilter _selectedQuickFilter = null;
        private QuickFilter SelectedQuickFilter
        {
            get => _selectedQuickFilter;
            set
            {
                _selectedQuickFilter = value;

                if (value == null)
                {
                    QuickFilterDropdownTextValue = QuickFilterDropdownEmptyText;
                    ApplyQuickFilterButtonInteractable = false;
                    DeleteQuickFilterButtonInteractable = false;
                }
                else
                {
                    var textWidth = _quickFilterDropdownText.GetPreferredValues(value.Name).x;
                    string name;

                    if (textWidth > 28f)
                        name = $"<size={(2800f / textWidth).ToString("N0")}%>" + value.Name + "</size>";
                    else
                        name = value.Name;

                    QuickFilterDropdownTextValue = name;
                    ApplyQuickFilterButtonInteractable = true;
                    DeleteQuickFilterButtonInteractable = true;
                }
            }
        }

        [UIValue("filter-cell-list")]
        private List<object> _filterCellList = new List<object>();
        [UIValue("quick-filter-name")]
        private string _quickFilterName = "";

#pragma warning disable CS0649
        [UIComponent("clear-button")]
        private Button _clearButton;
        [UIComponent("default-button")]
        private Button _defaultButton;
        [UIComponent("filter-list")]
        private CustomCellListTableData _filterListTableData;

        [UIObject("quick-filter-dropdown-container")]
        private GameObject _quickFilterDropdownContainer;

        [UIComponent("quick-filter-list")]
        private CustomListTableData _quickFilterTableData;
        [UIComponent("quick-filter-dropdown-text")]
        private TextMeshProUGUI _quickFilterDropdownText;
        [UIComponent("dropdown-chevron")]
        private RawImage _dropdownChevron;
        [UIComponent("quick-filter-name-setting")]
        private StringSetting _quickFilterNameSetting;
        [UIComponent("delete-quick-filter-modal-text")]
        private TextMeshProUGUI _deleteQuickFilterModalText;

        [UIParams]
        private BSMLParserParams _parserParams;
#pragma warning restore CS0649

        private Image _quickFilterDropdownImage;

        [UIValue("legend-text")]
        private const string LegendText =
            "<b>Filter Color Legend</b>\n\n" +
            "<color=#FF5555>Red</color>  -  Not applied\n" +
            "<color=#FFFF55>Yellow</color>  -  Not applied, but has changes\n" +
            "<color=#55FF55>Green</color>  -  Applied\n" +
            "<color=#55AAFF>Blue</color>  -  Applied, but has changes";

        [UIValue("save-quick-filter-button-text")]
        private const string SaveQuickFilterButtonText = "Save Settings\nTo Quick Filter";
        private const string QuickFilterDropdownEmptyText = "<color=#FF9999>None Available</color>";

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

                _filterListTableData.gameObject.GetComponentInChildren<ScrollRect>().vertical = false;
                _dropdownChevron.color = UIUtilities.LightBlueElementColour;

                _quickFilterDropdownImage = _quickFilterDropdownContainer.GetComponentsInChildren<Image>().First();

                var enterExitEventHandler = _quickFilterDropdownContainer.AddComponent<EnterExitEventHandler>();
                var clickEventHandler = _quickFilterDropdownContainer.AddComponent<ClickEventHandler>();
                enterExitEventHandler.PointerEntered += () => _quickFilterDropdownImage.color = UIUtilities.LightBlueHighlightedColour;
                enterExitEventHandler.PointerExited += () => _quickFilterDropdownImage.color = UIUtilities.RoundRectDefaultColour;
                clickEventHandler.PointerClicked += OnQuickFilterDropdownClicked;
            }

            SetFilterList();

            ClearButtonInteractable = false;
            DefaultButtonInteractable = false;
            FilterListActive = false;
            SaveQuickFilterButtonInteractable = QuickFiltersManager.HasSlotsAvailable;
        }

        /// <summary>
        /// Set the global list of filters to the filter list TableView.
        /// </summary>
        public void SetFilterList()
        {
            _filterCellList.Clear();

            foreach (var filter in FilterList.ActiveFilters)
                _filterCellList.Add(new FilterTableCell(filter));

            if (_filterListTableData != null)
            {
                _filterListTableData.data = _filterCellList;
                _filterListTableData.tableView.ReloadData();
            }
        }

        /// <summary>
        /// Select a filter on the TableView by index.
        /// </summary>
        /// <param name="index">Index of filter.</param>
        public void SelectFilterCell(int index)
        {
            if (_filterListTableData == null || index < 0 || index >= _filterCellList.Count)
                return;

            _filterListTableData.tableView.SelectCellWithIdx(index);
        }

        /// <summary>
        /// Refresh filter status image on every cell in the TableView.
        /// </summary>
        public void RefreshFilterListCellContent()
        {
            foreach (var cell in _filterCellList)
                (cell as FilterTableCell).RefreshCellContent();
        }

        public void HideModals()
        {
            if (_parserParams == null)
                return;

            // hide string setting keyboard modal
            if (_quickFilterNameSetting != null)
                _quickFilterNameSetting.modalKeyboard.modalView.Hide(true);

            _parserParams.EmitEvent("hide-save-quick-filter-modal,hide-quick-filter-list-modal,hide-delete-quick-filter-modal");
        }

        /// <summary>
        /// Refresh quick filters TableView in selection modal.
        /// </summary>
        private void RefreshQuickFiltersList()
        {
            if (_quickFilterTableData == null)
                return;
            else if (_quickFilterTableData.data == null)
                _quickFilterTableData.data = new List<CustomListTableData.CustomCellInfo>(QuickFiltersManager.NumberOfSlots);

            _quickFilterTableData.data.Clear();

            foreach (var quickFilter in QuickFiltersManager.QuickFiltersList)
                _quickFilterTableData.data.Add(new CustomListTableData.CustomCellInfo(quickFilter.Name));

            _quickFilterTableData.tableView.ReloadData();
        }

        /// <summary>
        /// Checks for nulls and sets the selected quick filter if necessary.
        /// </summary>
        public void CheckSelectedQuickFilter()
        {
            if (QuickFiltersManager.QuickFiltersList.Count > 0)
            {
                if (_selectedQuickFilter == null || !QuickFiltersManager.QuickFiltersList.Contains(_selectedQuickFilter))
                    SelectedQuickFilter = QuickFiltersManager.QuickFiltersList.First();
            }
            else
            {
                SelectedQuickFilter = null;
            }
        }

        /// <summary>
        /// Set warning text on quick filter creation modal depending on whether filters are applied/changed.
        /// </summary>
        private void SetDefaultSaveQuickFilterWarningText()
        {
            if (!FilterList.AnyApplied)
                SaveQuickFilterModalWarningText = "No filters are currently applied!";
            else if (FilterList.AnyChanged)
                SaveQuickFilterModalWarningText = "There are unapplied changes to your filter settings!";
            else
                SaveQuickFilterModalWarningText = "";
        }

        #region BSML Actions
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
        private void OnQuickFilterDropdownClicked()
        {
            RefreshQuickFiltersList();
            _parserParams.EmitEvent("show-quick-filter-list-modal");
        }

        [UIAction("quick-filter-list-cell-selected")]
        private void OnQuickFilterListCellSelected(TableView tableView, int index)
        {
            if (index < 0 || index >= QuickFiltersManager.QuickFiltersList.Count)
            {
                Logger.log.Warn("Invalid quick filter selected (list was outdated?)");
                return;
            }

            SelectedQuickFilter = QuickFiltersManager.QuickFiltersList[index];
            _parserParams.EmitEvent("hide-quick-filter-list-modal");

            Logger.log.Debug($"Quick filter '{SelectedQuickFilter.Name}' selected from list");
        }

        [UIAction("apply-quick-filter-button-clicked")]
        private void OnApplyQuickFilterButtonClicked()
        {
            if (SelectedQuickFilter != null)
                QuickFilterApplied?.Invoke(SelectedQuickFilter);
        }

        [UIAction("delete-quick-filter-button-clicked")]
        private void OnDeleteQuickFilterButtonClicked()
        {
            // this should never happen
            if (SelectedQuickFilter == null)
            {
                CheckSelectedQuickFilter();
                return;
            }
            _parserParams.EmitEvent("show-delete-quick-filter-modal");

            // NOTE: text has to be set after the modal is requested to be shown
            // for whatever reason, the first time the modal is shown, the calculated text size is a tenth of what it should be
            // probably has to do with how the elements can be resized to from initially provided preferred size values vs
            // calculated preferred size values after being set to active for the first time
            var textWidth = _deleteQuickFilterModalText.GetPreferredValues(SelectedQuickFilter.Name).x;
            string name;

            if (textWidth > 30f)
                name = $"<size={(3000f / textWidth).ToString("N0")}%>" + SelectedQuickFilter.Name + "</size>";
            else
                name = SelectedQuickFilter.Name;

            DeleteQuickFilterModalTextValue = $"Are you sure you want to delete the \"<color=#FFFFCC>{name}</color>\" quick filter?";
        }

        [UIAction("modal-delete-quick-filter-button-clicked")]
        private void OnModalDeleteQuickFilterButtonClicked()
        {
            if (SelectedQuickFilter == null)
            {
                // this should never happen
                Logger.log.Warn("Unable to delete empty quick filter");
                return;
            }

            QuickFiltersManager.DeleteQuickFilter(SelectedQuickFilter);
            CheckSelectedQuickFilter();

            SaveQuickFilterButtonInteractable = true;

            _parserParams.EmitEvent("hide-delete-quick-filter-modal");
        }

        [UIAction("save-quick-filter-button-clicked")]
        private void OnSaveQuickFilterButtonClicked()
        {
            _quickFilterName = "";
            SetDefaultSaveQuickFilterWarningText();
            ModalSaveQuickFilterButtonInteractable = false;

            _parserParams.EmitEvent("get-quick-filter-name");
            _parserParams.EmitEvent("show-save-quick-filter-modal");
        }

        [UIAction("modal-save-quick-filter-button-clicked")]
        private void OnModalSaveQuickFilterButtonClicked()
        {
            QuickFiltersManager.SaveCurrentSettingsToQuickFilter(_quickFilterName);
            CheckSelectedQuickFilter();

            SaveQuickFilterButtonInteractable = QuickFiltersManager.HasSlotsAvailable;

            _parserParams.EmitEvent("hide-save-quick-filter-modal");
        }

        [UIAction("quick-filter-name-changed")]
        private void OnQuickFilterNameChanged(string value)
        {
            // validate name
            if (value.Length > QuickFilter.MaxNameLength)
            {
                SaveQuickFilterModalWarningText = $"A quick filter must have a name under {QuickFilter.MaxNameLength} characters!";
                ModalSaveQuickFilterButtonInteractable = false;
            }
            else if (value.Length == 0)
            {
                SaveQuickFilterModalWarningText = "A quick filter cannot have an empty name!";
                ModalSaveQuickFilterButtonInteractable = false;
            }
            else
            {
                SetDefaultSaveQuickFilterWarningText();
                ModalSaveQuickFilterButtonInteractable = true;
            }
        }

        [UIAction("quick-filter-name-formatter")]
        private string QuickFilterNameFormatter(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "<size=1%>.</size> New Quick Filter <size=1%>.</size>";

            if (s.Length > 12)
            {
                s = s.Substring(0, 10) + "<color=#FFFFBB><size=70%> [...]</size></color>";
            }
            else
            {
                // hacky way of adding padding
                string spaces = $"<space={(12 - s.Length) + 1}px>";
                s = "<size=1%>.</size>" + spaces + s + spaces + "<size=1%>.</size>";
            }

            return s;
        }
        #endregion
    }
}
