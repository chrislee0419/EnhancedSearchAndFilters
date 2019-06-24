using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using EnhancedSearchAndFilters.UI;

namespace EnhancedSearchAndFilters.Filters
{
    class OtherFilter : IFilter
    {
        public string FilterName { get { return "Misc."; } }
        public FilterStatus Status
        {
            get
            {
                if (_oneSaberAppliedValue || _lightshowAppliedValue)
                {
                    if (_oneSaberAppliedValue != _oneSaberStagingValue ||
                        _lightshowAppliedValue != _lightshowStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_oneSaberStagingValue || _lightshowStagingValue)
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
                return _oneSaberAppliedValue || _lightshowAppliedValue;
            }
            set
            {
                if (value)
                {
                    _oneSaberAppliedValue = _oneSaberStagingValue;
                    _lightshowAppliedValue = _lightshowStagingValue;
                }
                else
                {
                    _oneSaberAppliedValue = false;
                    _lightshowAppliedValue = false;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[3];

        public event Action SettingChanged;

        private BoolViewController _oneSaberViewController;
        private BoolViewController _lightshowViewController;

        private bool _oneSaberStagingValue = false;
        private bool _lightshowStagingValue = false;
        private bool _oneSaberAppliedValue = false;
        private bool _lightshowAppliedValue = false;

        private bool _isInitialized = false;

        public void Init()
        {
            if (_isInitialized)
                return;

            SubMenu submenu = new SubMenu((Transform)null);

            // title text
            var text = BeatSaberUI.CreateText(null, "Miscellaneous Filters", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = rt.anchorMin;

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // one saber view controller
            _oneSaberViewController = submenu.AddBool("Has One Saber", "Filters out songs that don't have at least one saber map");
            _oneSaberViewController.GetValue += () => _oneSaberStagingValue;
            _oneSaberViewController.SetValue += delegate (bool value)
            {
                _oneSaberStagingValue = value;
                SettingChanged?.Invoke();
            };
            _oneSaberViewController.Init();
            _oneSaberViewController.applyImmediately = true;

            Utilities.CreateHorizontalDivider(_oneSaberViewController.transform);

            Controls[1] = new FilterControl(_oneSaberViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f));

            // lightshow view controller
            _lightshowViewController = submenu.AddBool("Has Lightshow", "Filters out songs that don't have a lightshow map");
            _lightshowViewController.GetValue += () => _lightshowStagingValue;
            _lightshowViewController.SetValue += delegate (bool value)
            {
                _lightshowStagingValue = value;
                SettingChanged?.Invoke();
            };
            _lightshowViewController.Init();
            _lightshowViewController.applyImmediately = true;

            Controls[2] = new FilterControl(_lightshowViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -20f));

            _isInitialized = true;
        }

        private void RefreshUI()
        {
            _oneSaberViewController.applyImmediately = false;
            _lightshowViewController.applyImmediately = false;

            _oneSaberViewController.Init();
            _lightshowViewController.Init();

            _oneSaberViewController.applyImmediately = true;
            _lightshowViewController.applyImmediately = true;
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = false;
            _lightshowStagingValue = false;

            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            _oneSaberStagingValue = _oneSaberAppliedValue;
            _lightshowStagingValue = _lightshowAppliedValue;

            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || (!_oneSaberAppliedValue && !_lightshowAppliedValue))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                BeatmapDetails beatmap = detailsList[i];

                if (_lightshowAppliedValue &&
                    !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.DifficultyBeatmaps.Any(diff => diff.NotesCount == 0)))
                    detailsList.RemoveAt(i);
                else if (_oneSaberAppliedValue && !beatmap.DifficultyBeatmapSets.Any(diffSet => diffSet.CharacteristicName == "LEVEL_ONE_SABER"))
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }
    }
}
