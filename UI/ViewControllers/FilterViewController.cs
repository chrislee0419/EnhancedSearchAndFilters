using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.Filters;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class FilterViewController : VRUIViewController
    {
        public Action BackButtonPressed;

        private FilterListViewController _listViewController;
        private FilterSettingsViewController _settingsViewController;

        private Button _backButton;
        private Button _clearButton;
        private Button _resetButton;
        private Button _applyButton;

        private HoverHint _applyHint;

        private List<IPreviewBeatmapLevel> _levels;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                // the rect transform initially is not the size of the entire window, so set that manually
                RectTransform rt = this.rectTransform;
                rt.sizeDelta = new Vector2(160f, 80f);

                _backButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", HandleBackButtonPressed, "Back");
                _backButton.ToggleWordWrapping(false);
                _backButton.SetButtonTextSize(4f);
                rt = _backButton.transform as RectTransform;
                rt.anchorMax = Vector2.zero;
                rt.anchorMin = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(20f, 7.5f);
                rt.anchoredPosition = new Vector2(5f, 2f);

                _clearButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", HandleClearButtonPressed, "Clear All");
                _clearButton.ToggleWordWrapping(false);
                _clearButton.SetButtonTextSize(3f);
                rt = _clearButton.transform as RectTransform;
                rt.anchorMax = new Vector2(1f, 0f);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(24f, 6.5f);
                rt.anchoredPosition = new Vector2(-60f, 2f);
                BeatSaberUI.AddHintText(rt, "Set all filters back to the default");

                _resetButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", HandleResetButtonPressed, "Reset");
                _resetButton.ToggleWordWrapping(false);
                _resetButton.SetButtonTextSize(3f);
                _resetButton.interactable = false;
                rt = _resetButton.transform as RectTransform;
                rt.anchorMax = new Vector2(1f, 0f);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(20f, 6.5f);
                rt.anchoredPosition = new Vector2(-35f, 2f);
                BeatSaberUI.AddHintText(rt, "Revert to previously applied filters");

                _applyButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", HandleApplyButtonPressed, "Apply");
                _applyButton.ToggleWordWrapping(false);
                _applyButton.SetButtonTextSize(4f);
                _applyButton.interactable = false;
                rt = _applyButton.transform as RectTransform;
                rt.anchorMax = new Vector2(1f, 0f);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(20f, 7.5f);
                rt.anchoredPosition = new Vector2(-5f, 2f);
                _applyHint = BeatSaberUI.AddHintText(rt, "");

                _listViewController = new GameObject("FilterListViewController").AddComponent<FilterListViewController>();
                rt = _listViewController.rectTransform;
                rt.SetParent(this.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(50f, 60f);
                rt.anchoredPosition = new Vector2(5f, -5f);

                _listViewController.FilterSelected += PresentFilterMenu;
                _listViewController.__Activate(ActivationType.NotAddedToHierarchy);
                _listViewController.SetUserInteraction(true);

                _settingsViewController = new GameObject("FilterSettingsViewController").AddComponent<FilterSettingsViewController>();
                rt = _settingsViewController.rectTransform;
                rt.SetParent(this.transform, false);
                rt.anchorMin = Vector2.one;
                rt.anchorMax = Vector2.one;
                rt.pivot = Vector2.one;
                rt.sizeDelta = new Vector2(90f, 60f);
                rt.anchoredPosition = new Vector2(-5f, -5f);

                _settingsViewController.__Activate(ActivationType.NotAddedToHierarchy);
                _settingsViewController.SetUserInteraction(true);

                // TODO: add filters to the filter list
                _listViewController.FilterList.Add(new NJSFilter());
                _listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                //_listViewController.FilterList.Add(new NJSFilter());
                _listViewController.RefreshTable();

                foreach (var filter in _listViewController.FilterList)
                {
                    foreach (var control in filter.GetControls())
                    {
                        control.Init(_settingsViewController.transform);
                    }
                }
                foreach (var control in _listViewController.FilterList[0].GetControls())
                    control.EnableControl();

                // divider sprite
                Image divider = new GameObject("WhiteDivider").AddComponent<Image>();
                //divider.gameObject.SetActive(true);
                divider.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                divider.color = Color.white;
                rt = divider.rectTransform;
                rt.SetParent(_listViewController.transform, false);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(2f, 0f);
            }
            else
            {
                _listViewController.__Activate(ActivationType.NotAddedToHierarchy);
                _listViewController.SetUserInteraction(true);
                _settingsViewController.__Activate(ActivationType.NotAddedToHierarchy);
                _settingsViewController.SetUserInteraction(true);
            }

            // set all filters to match their current setting to remove persistence from the last time this view controller was presented
            //HandleResetButtonPressed();

            // TODO: set apply/unapply button status

            // show the menu for the first item
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            _listViewController.__Deactivate(DeactivationType.RemovedFromHierarchy, true);
            _listViewController.SetUserInteraction(false);
            _settingsViewController.__Deactivate(DeactivationType.RemovedFromHierarchy, true);
            _settingsViewController.SetUserInteraction(false);

            // deactivate current controls
        }

        /// <summary>
        /// Presents this view controller and sets the song list to filter. 
        /// This must be used instead of invoking the private PresentViewController to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will present this view controller.</param>
        /// <param name="levels">The list of levels that will be filtered.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IPreviewBeatmapLevel[] levels)
        {
            _levels = new List<IPreviewBeatmapLevel>(levels);
            parentFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { this, null, false });
        }

        private void HandleBackButtonPressed()
        {
            BackButtonPressed?.Invoke();
        }

        private void HandleClearButtonPressed()
        {
            _resetButton.interactable = true;
            _applyButton.interactable = true;
        }

        private void HandleResetButtonPressed()
        {
            _resetButton.interactable = false;
            _applyButton.interactable = false;
        }

        private void HandleApplyButtonPressed()
        {
            _resetButton.interactable = false;
            _applyButton.interactable = false;
        }

        private void PresentFilterMenu(IFilter oldFilter, IFilter newFilter)
        {
            foreach (var control in oldFilter.GetControls())
            {
                control.DisableControl();
            }
            foreach (var control in newFilter.GetControls())
            {
                control.EnableControl();
            }
        }
    }
}
