using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using EnhancedSearchAndFilters.Search;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.UI.Components;
using EnhancedSearchAndFilters.UI.ViewControllers;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.Filters
{
    internal class SearchFilter : FilterBase
    {
        public override string Name => "Search";
        public override FilterStatus Status
        {
            get
            {
                if (IsFilterApplied)
                    return HasChanges ? FilterStatus.AppliedAndChanged : FilterStatus.Applied;
                else if (_queryStagingValue != _queryAppliedValue ||
                         (!string.IsNullOrEmpty(_queryStagingValue) &&
                          (_splitQueryStagingValue != _splitQueryAppliedValue ||
                           _songFieldsStagingValue != _songFieldsAppliedValue ||
                           _stripSymbolsStagingValue != _stripSymbolsAppliedValue)))
                    return FilterStatus.NotAppliedAndChanged;
                else
                    return FilterStatus.NotApplied;
            }
        }
        public override bool IsFilterApplied => !string.IsNullOrEmpty(_queryAppliedValue);
        public override bool HasChanges => _queryStagingValue != _queryAppliedValue ||
            _splitQueryStagingValue != _splitQueryAppliedValue ||
            _songFieldsStagingValue != _songFieldsAppliedValue ||
            _stripSymbolsStagingValue != _stripSymbolsAppliedValue;
        public override bool IsStagingDefaultValues => string.IsNullOrEmpty(_queryStagingValue) &&
            _splitQueryStagingValue == SplitQueryDefaultValue &&
            _songFieldsStagingValue == SongFieldsDefaultValue &&
            _stripSymbolsStagingValue == StripSymbolsDefaultValue;

        [UIValue("split-query-hover-hint")]
        public string SplitQueryHoverHintText => SearchOptionsViewController.SplitQueryHoverHintText;
        [UIValue("song-fields-hover-hint")]
        public string SongFieldsHoverHintText => SearchOptionsViewController.SongFieldsHoverHintText;
        [UIValue("strip-symbols-hover-hint")]
        public string StripSymbolsHoverHintText => SearchOptionsViewController.StripSymbolsHoverHintText;
        [UIValue("song-fields-options")]
        public List<object> SongFieldsOptions => SearchOptionsViewController.SongFieldsOptions;

        private string _queryStagingValue = "";
        public string QueryStagingValue
        {
            get => _queryStagingValue;
            set
            {
                if (value == _queryStagingValue)
                    return;

                _queryStagingValue = value ?? "";
                RefreshValues();
                InvokeSettingChanged();
            }
        }

        private bool _splitQueryStagingValue = SplitQueryDefaultValue;
        [UIValue("split-query-value")]
        public bool SplitQueryStagingValue
        {
            get => _splitQueryStagingValue;
            set
            {
                if (_splitQueryStagingValue == value)
                    return;

                _splitQueryStagingValue = value;
                RefreshValues();
                InvokeSettingChanged();
            }
        }
        private SearchableSongFields _songFieldsStagingValue = SongFieldsDefaultValue;
        [UIValue("song-fields-value")]
        public SearchableSongFields SongFieldsStagingValue
        {
            get => _songFieldsStagingValue;
            set
            {
                if (_songFieldsStagingValue == value)
                    return;

                _songFieldsStagingValue = value;
                RefreshValues();
                InvokeSettingChanged();
            }
        }
        private bool _stripSymbolsStagingValue = StripSymbolsDefaultValue;
        [UIValue("strip-symbols-value")]
        public bool StripSymbolsStagingValue
        {
            get => _stripSymbolsStagingValue;
            set
            {
                if (_stripSymbolsStagingValue == value)
                    return;

                _stripSymbolsStagingValue = value;
                RefreshValues();
                InvokeSettingChanged();
            }
        }

        private string _queryAppliedValue = "";
        private bool _splitQueryAppliedValue = SplitQueryDefaultValue;
        private SearchableSongFields _songFieldsAppliedValue = SongFieldsDefaultValue;
        private bool _stripSymbolsAppliedValue = StripSymbolsDefaultValue;

#pragma warning disable CS0649
        [UIObject("container")]
        private GameObject _container;
        [UIComponent("settings-button")]
        private Button _settingsButton;
        [UIObject("settings-view")]
        private GameObject _settingsView;
#pragma warning restore CS0649

        private bool _settingsViewActive = false;

        private GameObject _keyboardContainer;
        private SearchFilterKeyboard _keyboard;
        private TextMeshProUGUI _text;

        private BeatmapLevelsModel _beatmapLevelsModel;

        private const string CursorText = "<color=#00CCCC>|</color>";
        private const string PlaceholderText = "<color=#FFFFCC><i>Enter text to search</i></color>";

        private const bool SplitQueryDefaultValue = false;
        private const SearchableSongFields SongFieldsDefaultValue = SearchableSongFields.All;
        private const bool StripSymbolsDefaultValue = false;

        private const string SettingsButtonKeyboardText = "Keyboard";
        private const string SettingsButtonSettingsText = "Search\nSettings";

        public override void Init(GameObject viewContainer)
        {
            if (_viewGameObject != null)
                return;

            // settings
            _parserParams = UIUtilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.Filters.SearchFilterSettingsView.bsml", viewContainer, this);

            _container.name = "SearchFilterViewContainer";
            var rt = _container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            _viewGameObject = _container;

            _keyboardContainer = new GameObject("KeyboardContainer");
            var rt2 = _keyboardContainer.AddComponent<RectTransform>();
            rt2.SetParent(rt, false);
            rt2.anchorMin = Vector2.zero;
            rt2.anchorMax = Vector2.one;
            rt2.anchoredPosition = Vector2.zero;
            rt2.sizeDelta = Vector2.zero;

            // keyboard
            var keyboardGO = new GameObject("FilterKeyboard", typeof(SearchFilterKeyboard), typeof(RectTransform));
            rt = keyboardGO.GetComponent<RectTransform>();
            rt.SetParent(rt2, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(85f, 45f);
            rt.anchoredPosition = new Vector2(0f, 4f);

            _keyboard = keyboardGO.GetComponent<SearchFilterKeyboard>();
            _keyboard.ClearButtonPressed += () => QueryStagingValue = "";
            _keyboard.DeleteButtonPressed += delegate ()
            {
                if (_queryStagingValue != string.Empty)
                    QueryStagingValue = _queryStagingValue.Substring(0, _queryStagingValue.Length - 1);
            };
            _keyboard.TextButtonPressed += key => QueryStagingValue += key;

            // text
            _text = BeatSaberUI.CreateText(rt2, PlaceholderText, new Vector2(0f, 24f));
            _text.enableWordWrapping = false;
            _text.fontSize = 6f;
            _text.alignment = TextAlignmentOptions.Center;

            _settingsButton.SetButtonText(SettingsButtonSettingsText);
            _settingsView.SetActive(false);
        }

        public override void SetDefaultValuesToStaging()
        {
            _queryStagingValue = "";
            _splitQueryStagingValue = SplitQueryDefaultValue;
            _songFieldsStagingValue = SongFieldsDefaultValue;
            _stripSymbolsStagingValue = StripSymbolsDefaultValue;

            RefreshValues();
        }

        public override void SetAppliedValuesToStaging()
        {
            _queryStagingValue = _queryAppliedValue;
            _splitQueryStagingValue = _splitQueryAppliedValue;
            _songFieldsStagingValue = _songFieldsAppliedValue;
            _stripSymbolsStagingValue = _stripSymbolsAppliedValue;

            RefreshValues();
        }

        public override void ApplyStagingValues()
        {
            _queryAppliedValue = _queryStagingValue;
            _splitQueryAppliedValue = _splitQueryStagingValue;
            _songFieldsAppliedValue = _songFieldsStagingValue;
            _stripSymbolsAppliedValue = _stripSymbolsStagingValue;
        }

        public override void ApplyDefaultValues()
        {
            _queryAppliedValue = "";
            _splitQueryAppliedValue = SplitQueryDefaultValue;
            _songFieldsAppliedValue = SongFieldsDefaultValue;
            _stripSymbolsAppliedValue = StripSymbolsDefaultValue;
        }

        public override void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!IsFilterApplied || !IsAvailable)
                return;

            if (_beatmapLevelsModel == null)
            {
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
                if (_beatmapLevelsModel == null)
                {
                    Logger.log.Error("Unable to find BeatmapLevelsModel for search filter");
                    return;
                }
            }

            IEnumerable<string> levelIds = detailsList.Select(details => details.LevelID);
            IEnumerable<IPreviewBeatmapLevel> allLevels = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks
                .AsParallel()
                .SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels)
                .Where(level => levelIds.Any(lid => level.levelID.StartsWith(lid)))
                .AsEnumerable();
            List<IPreviewBeatmapLevel> filteredLevels = SearchBehaviour.Instance.StartInstantSearch(allLevels, _queryAppliedValue, _stripSymbolsAppliedValue, _splitQueryAppliedValue, _songFieldsAppliedValue);

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails details = detailsList[i];
                if (!filteredLevels.Any(level => level.levelID.StartsWith(details.LevelID)))
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }

        public override List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs()
        {
            return FilterSettingsKeyValuePair.CreateFilterSettingsList(
                "query", _queryAppliedValue,
                "splitQuery", _splitQueryAppliedValue,
                "songFields", _songFieldsAppliedValue,
                "stripSymbols", _stripSymbolsAppliedValue);
        }

        public override void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList)
        {
            SetDefaultValuesToStaging();

            foreach (var pair in settingsList)
            {
                if (pair.Key == "query")
                {
                    _queryStagingValue = pair.Value;
                }
                else if (bool.TryParse(pair.Value, out bool boolValue))
                {
                    if (pair.Key == "splitQuery")
                        _splitQueryStagingValue = boolValue;
                    else if (pair.Key == "stripSymbols")
                        _stripSymbolsStagingValue = boolValue;
                }
                else if (pair.Key == "songFields" && Enum.TryParse(pair.Value, out SearchableSongFields enumValue))
                {
                    _songFieldsStagingValue = enumValue;
                }
            }

            RefreshValues();
        }

        protected override void RefreshValues()
        {
            base.RefreshValues();

            if (_viewGameObject != null)
            {
                if (string.IsNullOrEmpty(QueryStagingValue))
                    _text.SetText(PlaceholderText);
                else
                    _text.SetText(QueryStagingValue.ToUpper().EscapeTextMeshProTags() + CursorText);
            }
        }

        [UIAction("settings-button-clicked")]
        private void SettingsButtonClicked()
        {
            _settingsViewActive = !_settingsViewActive;

            _settingsView.SetActive(_settingsViewActive);
            _keyboardContainer.SetActive(!_settingsViewActive);
            _settingsButton.SetButtonText(_settingsViewActive ? SettingsButtonKeyboardText : SettingsButtonSettingsText);
        }

        [UIAction("song-fields-formatter")]
        private string SongFieldsFormatter(object value) => SearchOptionsViewController.SongFieldsFormatter(value);
    }
}
