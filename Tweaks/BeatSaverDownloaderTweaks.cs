using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SongListUI = EnhancedSearchAndFilters.UI.SongListUI;
using System;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class BeatSaverDownloaderTweaks
    {
        private static Button _sortButton;
        private static Button _defaultSortButton;
        private static Button _newestSortButton;
        private static Button _authorSortButton;
        private static Button _randomButton;

        public static bool ModLoaded { get; set; } = false;
        public static bool Initialized { get; private set; } = false;

        /// <summary>
        /// Adapts some BeatSaverDownloader buttons to this mod.
        /// </summary>
        /// <returns>true, if buttons were modified successfully; false, otherwise</returns>
        /// <param name="newButtonSize"></param>
        public static bool Init(Vector2 newButtonSize)
        {
            if (!ModLoaded)
                return false;

            Logger.log.Info("BeatSaverDownloader mod found. Attempting to replace Search button behaviour.");

            // acquire all UI elements first, before modifying
            List<Button> buttonList;
            Button searchButton;
            try
            {
                RectTransform viewControllersContainer = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "ViewControllers");
                var parent = viewControllersContainer.GetComponentInChildren<LevelPackLevelsViewController>(true);

                buttonList = parent.transform.GetComponentsInChildren<Button>(true).Where(x => x.name == "CustomUIButton").ToList();

                searchButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Search") == true);
                _sortButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Sort By") == true);
                _defaultSortButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Default") == true);
                _newestSortButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Newest") == true);
                _authorSortButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Song Author") == true);
                //_difficultySortButton = buttonList.First(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text.Contains("Difficulty") == true);
                _randomButton = buttonList.First(x =>
                    x.GetComponentInChildren<TextMeshProUGUI>(true)?.text == null &&
                    x.transform.parent == _sortButton.transform.parent);
            }
            catch (InvalidOperationException)
            {
                Logger.log.Debug("Unable to find the buttons created by BeatSaverDownloader mod.");
                return false;
            }

            // modify BeatSaverDownloader's Search button to create our FlowCoordinator
            searchButton.onClick = new Button.ButtonClickedEvent();
            searchButton.onClick.AddListener(SongListUI.Instance.SearchButtonPressed);

            // move BeatSaverDownloader's Sort and Random buttons to make room for our "Filter" and "Clear Filter" buttons
            DismissableNavigationController parentViewController = SongListUI.Instance.ButtonParentViewController;
            searchButton.transform.SetParent(parentViewController.transform, false);
            _sortButton.transform.SetParent(parentViewController.transform, false);
            _randomButton.transform.SetParent(parentViewController.transform, false);
            (searchButton.transform as RectTransform).sizeDelta = newButtonSize;
            (_sortButton.transform as RectTransform).sizeDelta = newButtonSize;
            (searchButton.transform as RectTransform).anchoredPosition = new Vector2(-52f, 36.5f);
            (_sortButton.transform as RectTransform).anchoredPosition = new Vector2(14f, 36.5f);
            (_randomButton.transform as RectTransform).anchoredPosition = new Vector2(36f, 36.5f);
            _sortButton.onClick.RemoveAllListeners();
            _sortButton.onClick.AddListener(() =>
            {
                SongListUI.Instance.ToggleButtonsActive(false);
                SetTopButtons(true);
            });
            _defaultSortButton.onClick.AddListener(() =>
            {
                SongListUI.Instance.ToggleButtonsActive(true);
                SongListUI.Instance.UnapplyFilters();
            });
            _newestSortButton.onClick.AddListener(() =>
            {
                SongListUI.Instance.ToggleButtonsActive(true);
                SongListUI.Instance.UnapplyFilters();
            });
            _authorSortButton.onClick.AddListener(() =>
            {
                SongListUI.Instance.ToggleButtonsActive(true);
                SongListUI.Instance.UnapplyFilters();
            });

            SongListUI.Instance.SearchButton = searchButton;

            Initialized = true;
            Logger.log.Info("Modified BeatSaverDownloader's search, sort, and random buttons");
            return true;
        }

        public static void SetTopButtons(bool showSortButtons)
        {
            if (!ModLoaded || !Initialized)
                return;
            _sortButton.gameObject.SetActive(!showSortButtons);
            _randomButton.gameObject.SetActive(!showSortButtons);
            _defaultSortButton.gameObject.SetActive(showSortButtons);
            _newestSortButton.gameObject.SetActive(showSortButtons);
            _authorSortButton.gameObject.SetActive(showSortButtons);
        }

        public static void HideTopButtons()
        {
            if (!ModLoaded || !Initialized)
                return;
            _sortButton.gameObject.SetActive(false);
            _randomButton.gameObject.SetActive(false);
            _defaultSortButton.gameObject.SetActive(false);
            _newestSortButton.gameObject.SetActive(false);
            _authorSortButton.gameObject.SetActive(false);
        }
    }
}
