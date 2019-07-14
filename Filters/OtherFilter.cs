using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SongCore;
using SongCore.Data;
using SongCore.Utilities;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using EnhancedSearchAndFilters.UI;

namespace EnhancedSearchAndFilters.Filters
{
    class OtherFilter : IFilter
    {
        public string FilterName { get { return "Other"; } }
        public bool IsAvailable { get { return true; } }
        public FilterStatus Status
        {
            get
            {
                if (ApplyFilter)
                {
                    if (_oneSaberAppliedValue != _oneSaberStagingValue ||
                        _lightshowAppliedValue != _lightshowStagingValue ||
                        _mappingExtensionsAppliedValue != _mappingExtensionsStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_oneSaberStagingValue || _lightshowStagingValue || _mappingExtensionsStagingValue != SongRequirement.Off)
                {
                    return FilterStatus.NotAppliedAndChanged;
                }
                else
                {
                    return FilterStatus.NotAppliedAndDefault;
                }
            }
        }
        public bool ApplyFilter
        {
            get
            {
                return _oneSaberAppliedValue || _lightshowAppliedValue || _mappingExtensionsAppliedValue != SongRequirement.Off;
            }
            set
            {
                if (value)
                {
                    _oneSaberAppliedValue = _oneSaberStagingValue;
                    _lightshowAppliedValue = _lightshowStagingValue;
                    _mappingExtensionsAppliedValue = _mappingExtensionsStagingValue;
                }
                else
                {
                    _oneSaberAppliedValue = false;
                    _lightshowAppliedValue = false;
                    _mappingExtensionsAppliedValue = SongRequirement.Off;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[4];

        public event Action SettingChanged;

        private BoolViewController _oneSaberViewController;
        private BoolViewController _lightshowViewController;
        private ListViewController _mappingExtensionsViewController;

        private bool _oneSaberStagingValue = false;
        private bool _lightshowStagingValue = false;
        private SongRequirement _mappingExtensionsStagingValue = SongRequirement.Off;
        private bool _oneSaberAppliedValue = false;
        private bool _lightshowAppliedValue = false;
        private SongRequirement _mappingExtensionsAppliedValue = SongRequirement.Off;

        private bool _isInitialized = false;

        public void Init()
        {
            if (_isInitialized)
                return;

            // title text
            var text = BeatSaberUI.CreateText(null, "Other Filters", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = rt.anchorMin;

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // one saber view controller
            _oneSaberViewController = Utilities.CreateBoolViewController("Has One Saber", "Filters out songs that don't have at least one saber map");
            _oneSaberViewController.GetValue += () => _oneSaberStagingValue;
            _oneSaberViewController.SetValue += delegate (bool value)
            {
                _oneSaberStagingValue = value;
                SettingChanged?.Invoke();
            };
            _oneSaberViewController.Init();
            _oneSaberViewController.applyImmediately = true;

            Utilities.CreateHorizontalDivider(_oneSaberViewController.transform);
            Utilities.MoveIncDecViewControllerElements(_oneSaberViewController);

            Controls[1] = new FilterControl(_oneSaberViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f));

            // lightshow view controller
            _lightshowViewController = Utilities.CreateBoolViewController("Has Lightshow", "Filters out songs that don't have a lightshow map");
            _lightshowViewController.GetValue += () => _lightshowStagingValue;
            _lightshowViewController.SetValue += delegate (bool value)
            {
                _lightshowStagingValue = value;
                SettingChanged?.Invoke();
            };
            _lightshowViewController.Init();
            _lightshowViewController.applyImmediately = true;

            Utilities.CreateHorizontalDivider(_lightshowViewController.transform);
            Utilities.MoveIncDecViewControllerElements(_lightshowViewController);

            Controls[2] = new FilterControl(_lightshowViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -20f));

            // mapping extensions view controller
            var values = Enumerable.Range(0, 3).Select(x => (float)x).ToArray();
            _mappingExtensionsViewController = Utilities.CreateListViewController("Requires Mapping Extensions", values, "Filters out songs that don't require the 'Mapping Extensions' mod");
            _mappingExtensionsViewController.GetTextForValue += delegate (float value)
            {
                if (value == (float)SongRequirement.Required)
                    return "<size=90%>Required</size>";
                else if (value == (float)SongRequirement.NotRequired)
                    return "<size=70%>Not Required</size>";
                else
                    return "OFF";
            };
            _mappingExtensionsViewController.GetValue += () => (float)_mappingExtensionsStagingValue;
            _mappingExtensionsViewController.SetValue += delegate (float value)
            {
                _mappingExtensionsStagingValue = (SongRequirement)value;
                SettingChanged?.Invoke();
            };
            _mappingExtensionsViewController.Init();
            _mappingExtensionsViewController.applyImmediately = true;

            Utilities.MoveIncDecViewControllerElements(_mappingExtensionsViewController);

            Controls[3] = new FilterControl(_mappingExtensionsViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -32f));

            _isInitialized = true;
        }

        private void RefreshUI()
        {
            _oneSaberViewController.applyImmediately = false;
            _lightshowViewController.applyImmediately = false;
            _mappingExtensionsViewController.applyImmediately = false;

            _oneSaberViewController.Init();
            _lightshowViewController.Init();
            _mappingExtensionsViewController.Init();

            _oneSaberViewController.applyImmediately = true;
            _lightshowViewController.applyImmediately = true;
            _mappingExtensionsViewController.applyImmediately = true;
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = false;
            _lightshowStagingValue = false;
            _mappingExtensionsStagingValue = SongRequirement.Off;

            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = _oneSaberAppliedValue;
            _lightshowStagingValue = _lightshowAppliedValue;
            _mappingExtensionsStagingValue = _mappingExtensionsAppliedValue;

            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || (!_oneSaberAppliedValue && !_lightshowAppliedValue && _mappingExtensionsAppliedValue == SongRequirement.Off))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails beatmap = detailsList[i];
                List<CustomPreviewBeatmapLevel> customLevels = null;
                if (_mappingExtensionsAppliedValue != SongRequirement.Off)
                    customLevels = Loader.CustomLevels.Values.ToList();

                if (_lightshowAppliedValue &&
                    !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.DifficultyBeatmaps.Any(diff => diff.NotesCount == 0)))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_oneSaberAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "LEVEL_ONE_SABER"))
                {
                    detailsList.RemoveAt(i);
                }
                else if (_mappingExtensionsAppliedValue != SongRequirement.Off && !beatmap.IsOST)
                {
                    // remove songs that somehow aren't OST, but also aren't custom levels handled by SongCore
                    CustomPreviewBeatmapLevel customLevel = customLevels.FirstOrDefault(x => x.levelID == beatmap.LevelID);
                    if (customLevel == null)
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    ExtraSongData songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(customLevel), customLevel.customLevelPath);
                    if (songData == null)
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    bool required = songData._difficulties.Any(x => x.additionalDifficultyData._requirements.Any(y => y == "Mapping Extensions"));
                    if ((_mappingExtensionsAppliedValue == SongRequirement.Required && !required) ||
                        (_mappingExtensionsAppliedValue == SongRequirement.NotRequired && required))
                    {
                        detailsList.RemoveAt(i);
                        continue;
                    }

                    // passes check, requires mapping extensions
                    ++i;
                }
                else
                {
                    ++i;
                }
            }
        }
    }

    internal enum SongRequirement
    {
        Off = 0,
        Required = 1,
        NotRequired = 2
    }
}
