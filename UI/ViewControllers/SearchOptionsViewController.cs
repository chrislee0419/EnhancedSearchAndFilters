using System;
using VRUI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using System.Linq;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SearchOptionsViewController : VRUIViewController
    {
        SubMenu _submenu;
        ListViewController _maxResultsShownSetting;
        BoolViewController _stripSymbolsSetting;
        BoolViewController _splitQuerySetting;
        ListViewController _songFieldsSetting;
        BoolViewController _compactModeSetting;

        private int _maxResultsShownStagingValue;
        private bool _stripSymbolsStagingValue;
        private bool _splitQueryStagingValue;
        private SearchableSongFields _songFieldsStagingValue;
        private bool _compactModeStagingValue;

        public Action SearchOptionsChanged;

        private const float DefaultElementYPosition = 0.84f;
        private const float DefaultElementHeight = 0.12f;

        private float _currentYPosition = DefaultElementYPosition;

        private Button _defaultButton;
        private Button _resetButton;
        private Button _applyButton;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _maxResultsShownStagingValue = PluginConfig.MaxSearchResults;
            _stripSymbolsStagingValue = PluginConfig.StripSymbols;
            _splitQueryStagingValue = PluginConfig.SplitQueryByWords;
            _songFieldsStagingValue = PluginConfig.SongFieldsToSearch;
            _compactModeStagingValue = PluginConfig.CompactSearchMode;

            if (firstActivation)
            {
                var headerRectTransform = Instantiate(Resources.FindObjectsOfTypeAll<RectTransform>()
                    .First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), this.rectTransform);
                headerRectTransform.gameObject.SetActive(true);
                TextMeshProUGUI titleText = headerRectTransform.GetComponentInChildren<TextMeshProUGUI>();
                titleText.SetText("Options");

                _submenu = new SubMenu(this.transform);

                // have to use the ListViewController because IntViewController doesn't seem to work outside of the settings menu
                float[] maxResultsShownValues =
                    Enumerable.Range(PluginConfig.MaxSearchResultsMinValue, PluginConfig.MaxSearchResultsMaxValue + 1)
                    .Select((x) => (float)x).ToArray();
                _maxResultsShownSetting = _submenu.AddList("Maximum # of Results Shown", maxResultsShownValues,
                    "The maximum number of songs found before a search result is shown.\n" +
                    "<color=#11FF11>A lower number is less distracting and only displays results when most irrelevant songs are removed.</color>\n" +
                    "<color=#FFFF11>You can force a search result to be shown using the button on the center screen.</color>");
                _maxResultsShownSetting.GetTextForValue += x => ((int)x).ToString();
                _maxResultsShownSetting.GetValue += () => _maxResultsShownStagingValue;
                _maxResultsShownSetting.SetValue += delegate (float value)
                {
                    _maxResultsShownStagingValue = (int)value;
                    _resetButton.interactable = true;
                    _applyButton.interactable = true;
                };
                SetSettingElementPosition(_maxResultsShownSetting.transform as RectTransform);
                _maxResultsShownSetting.Init();
                _maxResultsShownSetting.applyImmediately = true;        // applyImmediately is after Init(), otherwise it calls SetValue once

                _splitQuerySetting = _submenu.AddBool("Search Each Word Individually",
                    "Split up the search query into words and searches the song details for those words individually. " +
                    "A song will only appear in the results if it contains all the words typed.\n" +
                    "<color=#11FF11>'ON' - For when you know some words or names in the song details, but not the specific order.</color>\n" +
                    "<color=#11FF11>'OFF' - Useful if you want to search for a particular phrase.</color>");
                _splitQuerySetting.GetValue += () => _splitQueryStagingValue;
                _splitQuerySetting.SetValue += delegate (bool value)
                {
                    _splitQueryStagingValue = value;
                    _resetButton.interactable = true;
                    _applyButton.interactable = true;
                };
                SetSettingElementPosition(_splitQuerySetting.transform as RectTransform);
                _splitQuerySetting.Init();
                _splitQuerySetting.applyImmediately = true;

                float[] songFieldsValues = new float[3]
                {
                    (float)SearchableSongFields.All, (float)SearchableSongFields.TitleAndAuthor, (float)SearchableSongFields.TitleOnly
                };
                _songFieldsSetting = _submenu.AddList("Song Fields to Search", songFieldsValues,
                    "A query will only search in these particular details of a song.\n" +
                    "<color=#11FF11>Can get relevant results quicker if you never search for song artist or beatmap creator.</color>\n" +
                    "Options - 'All', 'Title and Author', 'Title Only'");
                _songFieldsSetting.GetTextForValue += delegate (float value)
                {
                    switch (value)
                    {
                        case (float)SearchableSongFields.All:
                            return "All";
                        case (float)SearchableSongFields.TitleAndAuthor:
                            return "<size=80%>Title and Author</size>";
                        //case (float)SearchableSongFields.TitleOnly:
                        default:
                            return "Title Only";
                    }
                };
                _songFieldsSetting.GetValue += () => (float)_songFieldsStagingValue;
                _songFieldsSetting.SetValue += delegate (float value)
                {
                    _songFieldsStagingValue = (SearchableSongFields)value;
                    _resetButton.interactable = true;
                    _applyButton.interactable = true;
                };
                SetSettingElementPosition(_songFieldsSetting.transform as RectTransform);
                _songFieldsSetting.Init();
                _songFieldsSetting.applyImmediately = true;

                _stripSymbolsSetting = _submenu.AddBool("Strip Symbols from Song Details",
                    "Removes symbols from song title, subtitle, artist, etc. fields when performing search.\n" +
                    "<color=#11FF11>Can be useful when searching for song remixes and titles with apostrophes, quotations, or hyphens.</color>");
                _stripSymbolsSetting.GetValue += () => _stripSymbolsStagingValue;
                _stripSymbolsSetting.SetValue += delegate (bool value)
                {
                    _stripSymbolsStagingValue = value;
                    _resetButton.interactable = true;
                    _applyButton.interactable = true;
                };
                SetSettingElementPosition(_stripSymbolsSetting.transform as RectTransform);
                _stripSymbolsSetting.Init();
                _stripSymbolsSetting.applyImmediately = true;

                _compactModeSetting = _submenu.AddBool("Use Compact Mode",
                    "Removes the keyboard on the right screen, replacing it with a smaller keyboard on the center screen.");
                _compactModeSetting.GetValue += () => _compactModeStagingValue;
                _compactModeSetting.SetValue += delegate (bool value)
                {
                    _compactModeStagingValue = value;
                    _resetButton.interactable = true;
                    _applyButton.interactable = true;
                };
                SetSettingElementPosition(_compactModeSetting.transform as RectTransform);
                _compactModeSetting.Init();
                _compactModeSetting.applyImmediately = true;

                _defaultButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton",
                    new Vector2(-37f, -32f), new Vector2(36f, 10f),
                    DefaultButtonPressed, "Use Defaults");
                _defaultButton.ToggleWordWrapping(false);
                _resetButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton",
                    new Vector2(16f, -32f), new Vector2(24f, 9f),
                    ResetButtonPressed, "Reset");
                _resetButton.ToggleWordWrapping(false);
                _applyButton = BeatSaberUI.CreateUIButton(this.rectTransform, "CancelButton",
                    new Vector2(43f, -32f), new Vector2(24f, 9f),
                    ApplyButtonPressed, "Apply");
                _applyButton.ToggleWordWrapping(false);
            }
            else
            {
                // force show current value in config
                RefreshUI();
            }

            _resetButton.interactable = false;
            _applyButton.interactable = false;
        }

        private void SetSettingElementPosition(RectTransform r, bool reset=false)
        {
            if (reset)
                _currentYPosition = DefaultElementYPosition;

            r.anchorMax = new Vector2(0.95f, _currentYPosition);
            r.anchorMin = new Vector2(0.05f, (_currentYPosition -= DefaultElementHeight));
            r.pivot = Vector2.zero;
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Force the current staging values to be displayed
        /// </summary>
        private void RefreshUI()
        {
            _maxResultsShownSetting.Init();
            _stripSymbolsSetting.Init();
            _splitQuerySetting.Init();
            _songFieldsSetting.Init();
            _compactModeSetting.Init();
        }

        private void DefaultButtonPressed()
        {
            _maxResultsShownStagingValue = PluginConfig.MaxSearchResultsDefaultValue;
            _stripSymbolsStagingValue = PluginConfig.StripSymbolsDefaultValue;
            _splitQueryStagingValue = PluginConfig.SplitQueryByWordsDefaultValue;
            _songFieldsStagingValue = PluginConfig.SongFieldsToSearchDefaultValue;
            _compactModeStagingValue = PluginConfig.CompactSearchModeDefaultValue;

            RefreshUI();

            _resetButton.interactable = true;
            _applyButton.interactable = true;
        }

        private void ResetButtonPressed()
        {
            _maxResultsShownStagingValue = PluginConfig.MaxSearchResults;
            _stripSymbolsStagingValue = PluginConfig.StripSymbols;
            _splitQueryStagingValue = PluginConfig.SplitQueryByWords;
            _songFieldsStagingValue = PluginConfig.SongFieldsToSearch;
            _compactModeStagingValue = PluginConfig.CompactSearchMode;

            RefreshUI();

            _resetButton.interactable = false;
            _applyButton.interactable = false;
        }
        private void ApplyButtonPressed()
        {
            PluginConfig.MaxSearchResults = _maxResultsShownStagingValue;
            PluginConfig.StripSymbols = _stripSymbolsStagingValue;
            PluginConfig.SplitQueryByWords = _splitQueryStagingValue;
            PluginConfig.SongFieldsToSearch = _songFieldsStagingValue;
            PluginConfig.CompactSearchMode = _compactModeStagingValue;

            _resetButton.interactable = false;
            _applyButton.interactable = false;

            SearchOptionsChanged?.Invoke();
        }
    }
}
