using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SearchOptionsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "EnhancedSearchAndFilters.UI.Views.SearchOptionsView.bsml";

        public event Action SearchOptionsApplied;

        [UIValue("max-results-increment-value")]
        public int MaxResultsShownIncrementValue { get => PluginConfig.MaxSearchResultsValueIncrement; }
        [UIValue("max-results-min-value")]
        public int MaxResultsShownMinValue { get => PluginConfig.MaxSearchResultsMinValue; }
        [UIValue("max-results-max-value")]
        public int MaxResultsShownMaxValue { get => PluginConfig.MaxSearchResultsUnlimitedValue; }
        [UIValue("song-fields-options")]
        public static List<object> SongFieldsOptions { get => Enum.GetValues(typeof(SearchableSongFields)).Cast<SearchableSongFields>().Select(x => (object)x).ToList(); }

        private int _maxResultsShownStagingValue = PluginConfig.MaxSearchResults;
        [UIValue("max-results-value")]
        public int MaxResultsShownStagingValue
        {
            get => _maxResultsShownStagingValue;
            set
            {
                if (_maxResultsShownStagingValue == value)
                    return;
                _maxResultsShownStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

        private bool _splitQueryStagingValue = PluginConfig.SplitQueryByWords;
        [UIValue("split-query-value")]
        public bool SplitQueryStagingValue
        {
            get => _splitQueryStagingValue;
            set
            {
                if (_splitQueryStagingValue == value)
                    return;
                _splitQueryStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

        private SearchableSongFields _songFieldsStagingValue = PluginConfig.SongFieldsToSearch;
        [UIValue("song-fields-value")]
        public SearchableSongFields SongFieldsStagingValue
        {
            get => _songFieldsStagingValue;
            set
            {
                if (_songFieldsStagingValue == value)
                    return;
                _songFieldsStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

        private bool _stripSymbolsStagingValue = PluginConfig.StripSymbols;
        [UIValue("strip-symbols-value")]
        public bool StripSymbolsStagingValue
        {
            get => _stripSymbolsStagingValue;
            set
            {
                if (_stripSymbolsStagingValue == value)
                    return;
                _stripSymbolsStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

        private bool _compactModeStagingValue = PluginConfig.CompactSearchMode;
        [UIValue("compact-mode-value")]
        public bool CompactModeStagingValue
        {
            get => _compactModeStagingValue;
            set
            {
                if (_compactModeStagingValue == value)
                    return;
                _compactModeStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

        private bool _twoHandedTypingStagingValue = PluginConfig.TwoHandedTyping;
        [UIValue("two-handed-typing-value")]
        public bool TwoHandedTypingStagingValue
        {
            get => _twoHandedTypingStagingValue;
            set
            {
                if (_twoHandedTypingStagingValue == value)
                    return;
                _twoHandedTypingStagingValue = value;

                _resetButton.interactable = true;
                _applyButton.interactable = true;
            }
        }

#pragma warning disable CS0649
        [UIComponent("reset-button")]
        private Button _resetButton;
        [UIComponent("apply-button")]
        private Button _applyButton;

        [UIParams()]
        private BSMLParserParams _parserParams;
#pragma warning restore CS0649

        [UIValue("max-results-hover-hint")]
        private const string MaxResultsShownHoverHintText =
            "The maximum number of songs found before a search result is shown.\n" +
            "<color=#11FF11>A lower number is less distracting and only displays results when most irrelevant songs are removed.</color>\n" +
            "<color=#FFFF11>You can force a search result to be shown using the button on the center screen.</color>";
        [UIValue("split-query-hover-hint")]
        public const string SplitQueryHoverHintText =
            "Split up the search query into words and searches the song details for those words individually. " +
            "A song will only appear in the results if it contains all the words typed.\n" +
            "<color=#11FF11>'ON' - For when you know some words or names in the song details, but not the specific order.</color>\n" +
            "<color=#11FF11>'OFF' - Useful if you want to search for a particular phrase.</color>";
        [UIValue("song-fields-hover-hint")]
        public const string SongFieldsHoverHintText =
            "A query will only search in these particular details of a song.\n" +
            "<color=#11FF11>Can get relevant results quicker if you never search for song artist or beatmap creator.</color>\n" +
            "Options - 'All', 'Title and Author', 'Title Only'";
        [UIValue("strip-symbols-hover-hint")]
        public const string StripSymbolsHoverHintText =
            "Remove symbols from song title, subtitle, artist, etc. fields when performing search.\n" +
            "<color=#11FF11>Can be useful when searching for song remixes and titles with apostrophes, quotations, or hyphens.</color>";
        [UIValue("compact-mode-hover-hint")]
        private const string CompactModeHoverHintText =
            "Remove the keyboard on the right screen, replacing it with a smaller keyboard on the center screen.";
        [UIValue("two-handed-typing-hover-hint")]
        private const string TwoHandedTypingHoverHintText =
            "Add a laser pointer to the off-hand for easier two handed typing.";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);
            this.name = "SearchOptionsViewController";

            _resetButton.interactable = false;
            _applyButton.interactable = false;
        }

        [UIAction("max-results-formatter")]
        private string MaxResultsFormatter(int value) => value == PluginConfig.MaxSearchResultsUnlimitedValue ? "Unlimited" : value.ToString();

        [UIAction("song-fields-formatter")]
        public static string SongFieldsFormatter(object value)
        {
            switch ((SearchableSongFields)value)
            {
                case SearchableSongFields.All:
                    return "All";
                case SearchableSongFields.TitleAndAuthor:
                    return "<size=80%>Title and Author</size>";
                case SearchableSongFields.TitleOnly:
                    return "Title Only";
                default:
                    return "ERROR!";
            }
        }

        [UIAction("default-button-clicked")]
        private void DefaultButtonClicked()
        {
            MaxResultsShownStagingValue = PluginConfig.MaxSearchResultsDefaultValue;
            StripSymbolsStagingValue = PluginConfig.StripSymbolsDefaultValue;
            SplitQueryStagingValue = PluginConfig.SplitQueryByWordsDefaultValue;
            SongFieldsStagingValue = PluginConfig.SongFieldsToSearchDefaultValue;
            CompactModeStagingValue = PluginConfig.CompactSearchModeDefaultValue;
            TwoHandedTypingStagingValue = PluginConfig.TwoHandedTypingDefaultValue;

            _resetButton.interactable = true;
            _applyButton.interactable = true;

            _parserParams.EmitEvent("refresh-values");
        }

        [UIAction("reset-button-clicked")]
        private void ResetButtonClicked()
        {
            MaxResultsShownStagingValue = PluginConfig.MaxSearchResults;
            StripSymbolsStagingValue = PluginConfig.StripSymbols;
            SplitQueryStagingValue = PluginConfig.SplitQueryByWords;
            SongFieldsStagingValue = PluginConfig.SongFieldsToSearch;
            CompactModeStagingValue = PluginConfig.CompactSearchMode;
            TwoHandedTypingStagingValue = PluginConfig.TwoHandedTyping;

            _resetButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;

            _resetButton.interactable = false;
            _applyButton.interactable = false;

            _parserParams.EmitEvent("refresh-values");
        }

        [UIAction("apply-button-clicked")]
        private void ApplyButtonClicked()
        {
            PluginConfig.MaxSearchResults = MaxResultsShownStagingValue;
            PluginConfig.StripSymbols = StripSymbolsStagingValue;
            PluginConfig.SplitQueryByWords = SplitQueryStagingValue;
            PluginConfig.SongFieldsToSearch = SongFieldsStagingValue;
            PluginConfig.CompactSearchMode = CompactModeStagingValue;
            PluginConfig.TwoHandedTyping = TwoHandedTypingStagingValue;

            _applyButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;

            _resetButton.interactable = false;
            _applyButton.interactable = false;

            SearchOptionsApplied?.Invoke();
        }
    }
}
