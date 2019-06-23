using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.Filters;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class FilterViewController : VRUIViewController
    {
        public Action BackButtonPressed;
        public Action FiltersUnapplied;
        public Action<IPreviewBeatmapLevel[]> LevelsModified;

        public bool IsFilterApplied { get; private set; } = false;

        private FilterListViewController _listViewController;
        private RectTransform _settingsRectTransform;

        private Button _backButton;
        private Button _clearButton;
        private Button _resetButton;
        private Button _applyButton;
        private Button _unapplyButton;
        private Button _colorHint;

        private GameObject _loadingSpinner;
        private TextMeshProUGUI _loadingText;
        private TextMeshProUGUI _infoText;
        private int _coroutinesActive = 0;

        private IPreviewBeatmapLevel[] _levels;
        private Dictionary<BeatmapDetails, IPreviewBeatmapLevel> _beatmapDetails;

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
                BeatSaberUI.AddHintText(rt, "Puts all settings back to the default");

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
                BeatSaberUI.AddHintText(rt, "Revert to previously applied settings");

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
                BeatSaberUI.AddHintText(rt, "Applies the current settings to the filter");

                _unapplyButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", HandleUnapplyButtonPressed, "Unapply");
                _unapplyButton.ToggleWordWrapping(false);
                _unapplyButton.SetButtonTextSize(4f);
                _unapplyButton.interactable = false;
                rt = _unapplyButton.transform as RectTransform;
                rt.anchorMax = new Vector2(1f, 0f);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(20f, 7.5f);
                rt.anchoredPosition = new Vector2(-5f, 2f);
                BeatSaberUI.AddHintText(rt, "Unapplies the filter (Keeps all settings as-is for re-application)");
                _unapplyButton.gameObject.SetActive(false);

                _colorHint = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton", null, "?");
                _colorHint.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content").padding = new RectOffset(1, 1, 0, 0);
                _colorHint.SetButtonTextSize(4f);
                _colorHint.interactable = false;
                rt = _colorHint.transform as RectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(6f, 6f);
                rt.anchoredPosition = new Vector2(3f, -5f);
                BeatSaberUI.AddHintText(rt, "Filter Color Legend\n" + 
                    "<color=#FF5555>Red</color> - Not applied\n" +
                    "<color=#FFFF55>Yellow</color> - Not applied, but has changes\n" +
                    "<color=#55FF55>Green</color> - Applied\n" +
                    "<color=#55AAFF>Blue</color> - Applied, but has changes");
                _colorHint.gameObject.SetActive(false);

                _listViewController = new GameObject("FilterListViewController").AddComponent<FilterListViewController>();
                rt = _listViewController.rectTransform;
                rt.SetParent(this.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(50f, 60f);
                rt.anchoredPosition = new Vector2(5f, -5f);

                _listViewController.FilterSelected += PresentFilterMenu;

                // container RectTransform that all controls will be placed in
                // 90f wide, 60f tall
                var containerImage = new GameObject("FilterSettingsContainer").AddComponent<Image>();
                containerImage.sprite = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                containerImage.color = Color.black;
                _settingsRectTransform = containerImage.gameObject.transform as RectTransform;
                _settingsRectTransform.SetParent(this.transform, false);
                _settingsRectTransform.anchorMin = Vector2.one;
                _settingsRectTransform.anchorMax = Vector2.one;
                _settingsRectTransform.pivot = Vector2.one;
                _settingsRectTransform.sizeDelta = new Vector2(90f, 60f);
                _settingsRectTransform.anchoredPosition = new Vector2(-5f, -5f);

                // TODO: add filters to the filter list
                _listViewController.FilterList.Add(new DifficultyFilter());
                _listViewController.FilterList.Add(new DurationFilter());
                _listViewController.FilterList.Add(new NJSFilter());

                foreach (var filter in _listViewController.FilterList)
                    filter.SettingChanged += FilterSettingChanged;

                _listViewController.FilterList[0].Init();
                foreach (var control in _listViewController.FilterList[0].Controls)
                {
                    control.Init(_settingsRectTransform);
                }

                // divider sprite
                Image divider = new GameObject("WhiteDivider").AddComponent<Image>();
                divider.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                divider.color = Color.white;
                rt = divider.rectTransform;
                rt.SetParent(_settingsRectTransform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-8f, 0f);

                // loading screen
                _loadingSpinner = BeatSaberUI.CreateLoadingSpinner(this.transform);

                _loadingText = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(0f, 12f), new Vector2(80f, 50f));
                _loadingText.alignment = TextAlignmentOptions.Center;
                _loadingText.fontSize = 5f;

                // info text
                _infoText = BeatSaberUI.CreateText(this.rectTransform, "", new Vector2(28f, 2f), new Vector2(36f, 8f));
                _infoText.rectTransform.anchorMin = Vector2.zero;
                _infoText.rectTransform.anchorMax = Vector2.zero;
                _infoText.rectTransform.pivot = Vector2.zero;
                _infoText.alignment = TextAlignmentOptions.Left;
                _infoText.fontSize = 3.5f;
            }

            _loadingSpinner.gameObject.SetActive(true);
            if (PluginConfig.ShowFirstTimeLoadingText && _levels.Length > 0 && _levels[0] is CustomPreviewBeatmapLevel)
            {
                _loadingText.text = "<color=#FF5555>Loading custom beatmap details for the first time...</color>\n\n" +
                    "This first load may take several minutes, depending on the number of custom songs you have\n(it usually takes about 10 to 15 seconds for every 100 songs).\n\n" +
                    "<color=#CCFFCC>You may back out of this screen and have the loading occur in the background</color>,\nhowever, loading will pause when playing a level.";
                (_loadingSpinner.transform as RectTransform).anchoredPosition = new Vector2(0f, -20f);
            }
            else
            {
                _loadingText.text = "Loading beatmap details...";
                (_loadingSpinner.transform as RectTransform).anchoredPosition = Vector2.zero;
            }

            _loadingText.gameObject.SetActive(true);
            _infoText.text = GetLoadingProgressString(0, _levels.Length);
            _infoText.gameObject.SetActive(true);
            _settingsRectTransform.gameObject.SetActive(false);
            // _listViewController deactivated by DidDeactivate

            // load all songs so we have access to all the song details we need to filter
            BeatmapDetailsLoader.Instance.LoadBeatmaps(_levels,
                delegate (int songsLoaded)
                {
                    // on update, show updated progress text
                    _infoText.text = GetLoadingProgressString(songsLoaded, _levels.Length);
                },
                delegate (BeatmapDetails[] levels)
                {
                    // on finish
                    PluginConfig.ShowFirstTimeLoadingText = false;

                    _loadingSpinner.SetActive(false);
                    _loadingText.gameObject.SetActive(false);
                    _infoText.gameObject.SetActive(false);

                    _listViewController.__Activate(ActivationType.NotAddedToHierarchy);
                    _listViewController.SetUserInteraction(true);
                    _listViewController.RefreshTable(true);
                    _colorHint.gameObject.SetActive(true);

                    _settingsRectTransform.gameObject.SetActive(true);
                    foreach (var control in _listViewController.CurrentFilter.Controls)
                        control.EnableControl();

                    _beatmapDetails = levels.Zip(_levels, (details, beatmap) => new { details, beatmap }).ToDictionary((item) => item.details, (item) => item.beatmap);
                });
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            if (_listViewController.isActivated)
            {
                _listViewController.__Deactivate(DeactivationType.RemovedFromHierarchy, true);
                _listViewController.SetUserInteraction(false);
            }
        }

        /// <summary>
        /// Presents this view controller and sets the song list to filter. 
        /// This must be used instead of invoking the private PresentViewController to ensure the list of levels is provided.
        /// </summary>
        /// <param name="parentFlowCoordinator">The flow coordinator that will present this view controller.</param>
        /// <param name="levels">The list of levels that will be filtered.</param>
        public void Activate(FlowCoordinator parentFlowCoordinator, IPreviewBeatmapLevel[] levels)
        {
            _levels = levels;
            parentFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { this, null, false });
        }

        public void UnapplyFilters(bool sendEvent = true)
        {
            _resetButton.interactable = false;
            _applyButton.interactable = true;
            _unapplyButton.interactable = false;

            _applyButton.gameObject.SetActive(true);
            _unapplyButton.gameObject.SetActive(false);

            foreach (var filter in _listViewController.FilterList)
                filter.ApplyFilter = false;

            IsFilterApplied = false;

            _listViewController.RefreshTable();

            if (sendEvent)
                FiltersUnapplied?.Invoke();
        }

        private string GetLoadingProgressString(int loadedCount, int total)
        {
            return $"Loaded {loadedCount} out of {total} beatmaps...";
        }

        private void HandleBackButtonPressed()
        {
            BeatmapDetailsLoader.Instance.CancelLoading();

            BackButtonPressed?.Invoke();
        }

        private void HandleClearButtonPressed()
        {
            if (BeatmapDetailsLoader.Instance.IsLoading)
                return;

            _resetButton.interactable = true;

            bool hasChanged = false;
            foreach (var filter in _listViewController.FilterList)
            {
                filter.SetDefaultValues();

                if (filter.Status == FilterStatus.AppliedAndChanged)
                    hasChanged = true;
            }

            _applyButton.gameObject.SetActive(true);
            _unapplyButton.gameObject.SetActive(false);

            // if nothing needs to be changed, it implies that everything is default
            _applyButton.interactable = hasChanged;
            _unapplyButton.interactable = false;

            _listViewController.RefreshTable();
        }

        private void HandleResetButtonPressed()
        {
            if (BeatmapDetailsLoader.Instance.IsLoading)
                return;

            _resetButton.interactable = false;

            bool isAppliedNoChanges = false;
            foreach (var filter in _listViewController.FilterList)
            {
                filter.ResetValues();

                if (filter.Status == FilterStatus.Applied)
                    isAppliedNoChanges = true;
            }

            if (isAppliedNoChanges)
            {
                _applyButton.gameObject.SetActive(false);
                _unapplyButton.gameObject.SetActive(true);

                _applyButton.interactable = false;
                _unapplyButton.interactable = true;
            }
            else
            {
                // if nothing is applied after resetting, it implies that everything is default
                _applyButton.gameObject.SetActive(true);
                _unapplyButton.gameObject.SetActive(false);

                _applyButton.interactable = false;
                _unapplyButton.interactable = false;
            }

            _listViewController.RefreshTable();
        }

        private void HandleApplyButtonPressed()
        {
            if (BeatmapDetailsLoader.Instance.IsLoading)
                return;

            _resetButton.interactable = false;

            Logger.log.Debug($"Using filter, starting with {_beatmapDetails.Count} songs");
            List<BeatmapDetails> filteredLevels = new List<BeatmapDetails>(_beatmapDetails.Keys);
            bool hasApplied = false;
            foreach (var filter in _listViewController.FilterList)
            {
                filter.ApplyFilter = true;

                if (filter.Status == FilterStatus.Applied)
                {
                    filter.FilterSongList(ref filteredLevels);
                    hasApplied = true;
                }
            }
            Logger.log.Debug($"Filter completed, {filteredLevels.Count} songs left");

            if (hasApplied)
            {
                _applyButton.interactable = false;
                _unapplyButton.interactable = true;

                _applyButton.gameObject.SetActive(false);
                _unapplyButton.gameObject.SetActive(true);

                IsFilterApplied = true;

                _infoText.text = $"{filteredLevels.Count} out of {_beatmapDetails.Count} songs found";
                _infoText.gameObject.SetActive(true);
                StartCoroutine(HideInfoText());

                LevelsModified?.Invoke(_beatmapDetails.Where(x => filteredLevels.Contains(x.Key)).Select(x => x.Value).ToArray());
            }
            else
            {
                // defaults were applied
                _applyButton.interactable = false;
                _unapplyButton.interactable = false;

                _applyButton.gameObject.SetActive(true);
                _unapplyButton.gameObject.SetActive(false);

                IsFilterApplied = false;

                FiltersUnapplied?.Invoke();
            }

            _listViewController.RefreshTable();
        }

        private void HandleUnapplyButtonPressed()
        {
            if (BeatmapDetailsLoader.Instance.IsLoading)
                return;

            UnapplyFilters();
        }

        private void FilterSettingChanged()
        {
            bool hasChanged = false;
            bool hasAppliedNoChanges = false;
            foreach (var filter in _listViewController.FilterList)
            {
                if (filter.Status == FilterStatus.NotAppliedAndChanged || filter.Status == FilterStatus.AppliedAndChanged)
                    hasChanged = true;
                else if (filter.Status == FilterStatus.Applied)
                    hasAppliedNoChanges = true;
            }

            if (hasChanged)
            {
                _applyButton.gameObject.SetActive(true);
                _unapplyButton.gameObject.SetActive(false);

                _resetButton.interactable = true;
                _applyButton.interactable = true;
                _unapplyButton.interactable = false;
            }
            else if (hasAppliedNoChanges)
            {
                _applyButton.gameObject.SetActive(false);
                _unapplyButton.gameObject.SetActive(true);

                _resetButton.interactable = false;
                _applyButton.interactable = false;
                _unapplyButton.interactable = true;
            }
            else
            {
                // everything is default
                _applyButton.gameObject.SetActive(true);
                _unapplyButton.gameObject.SetActive(false);

                _resetButton.interactable = false;
                _applyButton.interactable = false;
                _unapplyButton.interactable = false;
            }

            _listViewController.RefreshTable();
        }

        private void PresentFilterMenu(IFilter oldFilter, IFilter newFilter)
        {
            foreach (var control in oldFilter.Controls)
            {
                control.DisableControl();
            }
            newFilter.Init();
            foreach (var control in newFilter.Controls)
            {
                if (!control.HasBeenInitialized)
                    control.Init(_settingsRectTransform);
                control.EnableControl();
            }
        }

        private IEnumerator HideInfoText()
        {
            ++_coroutinesActive;
            yield return new WaitForSeconds(10f);

            if ((--_coroutinesActive) == 0 && _infoText.text.Contains("songs found"))
                _infoText.gameObject.SetActive(false);
        }
    }
}
