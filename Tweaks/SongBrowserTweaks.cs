using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using EnhancedSearchAndFilters.UI;

namespace EnhancedSearchAndFilters.Tweaks
{
    internal static class SongBrowserTweaks
    {
        public static bool ModLoaded { get; set; } = false;
        public static bool Initialized { get; private set; } = false;

        public static bool Init()
        {
            if (!ModLoaded)
                return false;

            Logger.log.Info("SongBrowser mod found. Attempting to replace Search button behaviour.");

            // acquire all the UI elements we need to change before modifying
            Button searchButton;
            Button cancelSortButton;
            try
            {
                var buttonParentViewController = SongListUI.Instance.ButtonParentViewController;

                searchButton = buttonParentViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "FilterSearchButton");
                // cancel sort button is found using x position (will need to be changed if button position changes)
                cancelSortButton = buttonParentViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "CustomUIButton" && (x.transform as RectTransform).anchoredPosition.x == 17.5f);
            }
            catch (InvalidOperationException)
            {
                Logger.log.Debug("Unable to find the buttons created by SongBrowser");
                return false;
            }

            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(SongListUI.Instance.SearchButtonPressed);

            cancelSortButton.onClick.AddListener(SongListUI.Instance.ClearButtonPressed);

            SongListUI.Instance.SearchButton = searchButton;
            SongListUI.Instance.ClearButton = cancelSortButton;

            Initialized = true;
            Logger.log.Info("Modified SongBrowser's search and clear sort buttons");
            return true;
        }
    }
}
