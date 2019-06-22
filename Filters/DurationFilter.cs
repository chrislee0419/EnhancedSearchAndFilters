﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using EnhancedSearchAndFilters.UI;
using Object = UnityEngine.Object;

namespace EnhancedSearchAndFilters.Filters
{
    class DurationFilter : IFilter
    {
        public string FilterName { get { return "Song Length"; } }
        public FilterStatus Status
        {
            get
            {
                if (_minEnabledAppliedValue || _maxEnabledAppliedValue)
                {
                    if (_minAppliedValue != _minStagingValue ||
                        _maxAppliedValue != _maxStagingValue)
                        return FilterStatus.AppliedAndChanged;
                    else
                        return FilterStatus.Applied;
                }
                else if (_minEnabledStagingValue || _maxEnabledStagingValue)
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
                return _minEnabledAppliedValue || _maxEnabledAppliedValue;
            }
            set
            {
                if (value)
                {
                    _minEnabledAppliedValue = _minEnabledStagingValue;
                    _maxEnabledAppliedValue = _maxEnabledStagingValue;
                    _minAppliedValue = _minStagingValue;
                    _maxAppliedValue = _maxStagingValue;
                }
                else
                {
                    _minEnabledAppliedValue = false;
                    _maxEnabledAppliedValue = false;
                    _minAppliedValue = DefaultMaxValue;
                    _maxAppliedValue = _maxStagingValue;
                }
            }
        }
        public FilterControl[] Controls { get; private set; } = new FilterControl[3];

        public event Action SettingChanged;

        ListViewController _minViewController;
        ListViewController _maxViewController;

        private bool _isInitialized = false;

        private bool _minEnabledStagingValue = false;
        private bool _maxEnabledStagingValue = false;
        private float _minStagingValue = DefaultMinValue;
        private float _maxStagingValue = DefaultMaxValue;
        private bool _minEnabledAppliedValue = false;
        private bool _maxEnabledAppliedValue = false;
        private float _minAppliedValue = DefaultMinValue;
        private float _maxAppliedValue = DefaultMaxValue;

        private const float DefaultMinValue = 60f;
        private const float DefaultMaxValue = 120f;
        private const float MinValue = 0f;
        private const float MaxValue = 1800f;       // 30 minutes
        private const float IncrementValue = 15f;

        public void Init()
        {
            if (_isInitialized)
                return;

            SubMenu submenu = new SubMenu((Transform)null);

            var togglePrefab = Utilities.GetTogglePrefab();

            // title text
            var text = BeatSaberUI.CreateText(null, "Keep Songs Between Some Length", Vector2.zero, Vector2.zero);
            text.fontSize = 5.5f;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            Controls[0] = new FilterControl(text.gameObject, new Vector2(0f, 0.95f), new Vector2(0f, 0.95f), new Vector2(0f, 1f), new Vector2(50f, 6f), Vector2.zero);

            // min view controller
            float[] values = Enumerable.Range(0, (int)((MaxValue - MinValue) / IncrementValue) + 1).Select(x => x * IncrementValue).ToArray();
            _minViewController = submenu.AddList("Minimum Duration", values, "Filters out songs that are shorter than this value");
            _minViewController.GetTextForValue += x => ConvertFloatToTimeString(x);
            _minViewController.GetValue += () => _minStagingValue;
            _minViewController.SetValue += delegate (float value)
            {
                if (_maxEnabledStagingValue && value > _maxStagingValue)
                {
                    _minStagingValue = _maxStagingValue;
                    RefreshUI();
                    return;
                }

                _minStagingValue = value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _minViewController.Init();
            _minViewController.applyImmediately = true;

            var minToggle = Utilities.CreateToggleFromPrefab(togglePrefab.toggle, _minViewController.transform.Find("Value"));
            minToggle.name = "MinValueToggle";
            minToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _minEnabledStagingValue = value;

                if (value && _maxEnabledStagingValue && _minStagingValue > _maxStagingValue)
                    _minStagingValue = _maxStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            MoveViewControllerElements(_minViewController);

            var divider = new GameObject("Divider").AddComponent<Image>();
            divider.color = new Color(1f, 1f, 1f, 0.15f);

            rt = divider.rectTransform;
            rt.SetParent(_minViewController.transform);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-4f, 0.1f);
            rt.anchoredPosition = Vector2.zero;

            Controls[1] = new FilterControl(_minViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -8f),
                delegate ()
                {
                    // disabling buttons needs to be done after the view controller is enabled to override the interactable assignments of ListSettingsController:OnEnable()
                    _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _minEnabledStagingValue && _minStagingValue > MinValue;
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _minEnabledStagingValue && (!_maxEnabledStagingValue || _minStagingValue < _maxStagingValue) && _minStagingValue < MaxValue;
                });

            // max view controller
            _maxViewController = submenu.AddList("Maximum Duration", values, "Filters out songs that are longer than this value");
            _maxViewController.GetTextForValue += x => ConvertFloatToTimeString(x);
            _maxViewController.GetValue += () => _maxStagingValue;
            _maxViewController.SetValue += delegate (float value)
            {
                if (_minEnabledStagingValue && value < _minStagingValue)
                {
                    _maxStagingValue = _minStagingValue;
                    RefreshUI();
                    return;
                }

                _maxStagingValue = value;

                RefreshUI(false);

                SettingChanged?.Invoke();
            };
            _maxViewController.Init();
            _maxViewController.applyImmediately = true;

            var maxToggle = Utilities.CreateToggleFromPrefab(togglePrefab.toggle, _maxViewController.transform.Find("Value"));
            maxToggle.name = "MaxValueToggle";
            maxToggle.onValueChanged.AddListener(delegate (bool value)
            {
                _maxEnabledStagingValue = value;

                if (value && _minEnabledStagingValue && _maxStagingValue < _minStagingValue)
                    _maxStagingValue = _minStagingValue;

                RefreshUI(true, true);

                SettingChanged?.Invoke();
            });

            MoveViewControllerElements(_maxViewController);

            //divider = new GameObject("Divider").AddComponent<Image>();
            //divider.color = new Color(1f, 1f, 1f, 0.15f);

            //rt = divider.rectTransform;
            //rt.SetParent(_maxViewController.transform);
            //rt.anchorMin = Vector2.zero;
            //rt.anchorMax = new Vector2(1f, 0f);
            //rt.pivot = new Vector2(0.5f, 0.5f);
            //rt.sizeDelta = new Vector2(-4f, 0.1f);
            //rt.anchoredPosition = Vector2.zero;

            Controls[2] = new FilterControl(_maxViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(0f, -20f),
                delegate ()
                {
                    // disabling buttons needs to be done after the view controller is enabled to override the interactable assignments of ListSettingsController:OnEnable()
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _maxEnabledStagingValue && (!_minEnabledStagingValue || _maxStagingValue > _minStagingValue) && _maxStagingValue > MinValue;
                    _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _maxEnabledStagingValue  && _maxStagingValue < MaxValue;
                });

            Object.Destroy(togglePrefab);

            _isInitialized = true;
        }

        private string ConvertFloatToTimeString(float duration)
        {
            return TimeSpan.FromSeconds(duration).ToString("m':'ss");
        }

        /// <summary>
        /// Used to move the buttons and text outside of the "Value" transform for a ListViewController. 
        /// This is done because the "Value" transform has some forced horizontal layout that messes up child RectTransform positioning.
        /// </summary>
        /// <param name="controller"></param>
        private void MoveViewControllerElements(ListViewController controller)
        {
            var incButton = controller.transform.Find("Value/IncButton") as RectTransform;
            incButton.SetParent(controller.transform);
            incButton.anchorMin = new Vector2(1f, 0.5f);
            incButton.anchorMax = new Vector2(1f, 0.5f);
            incButton.pivot = new Vector2(1f, 0.5f);
            incButton.sizeDelta = new Vector2(8f, 8f);
            incButton.anchoredPosition = Vector2.zero;

            var text = controller.transform.Find("Value/ValueText") as RectTransform;
            text.SetParent(controller.transform);
            text.anchorMin = new Vector2(1f, 0.5f);
            text.anchorMax = new Vector2(1f, 0.5f);
            text.pivot = new Vector2(1f, 0.5f);
            text.sizeDelta = new Vector2(16f, 8f);
            text.anchoredPosition = new Vector2(-8f, 0f);

            var decButton = controller.transform.Find("Value/DecButton") as RectTransform;
            decButton.SetParent(controller.transform);
            decButton.anchorMin = new Vector2(1f, 0.5f);
            decButton.anchorMax = new Vector2(1f, 0.5f);
            decButton.pivot = new Vector2(1f, 0.5f);
            decButton.sizeDelta = new Vector2(8f, 8f);
            decButton.anchoredPosition = new Vector2(-24f, 0f);

            var toggle = controller.transform.Find("Value/MinValueToggle") as RectTransform ?? controller.transform.Find("Value/MaxValueToggle") as RectTransform;
            toggle.SetParent(controller.transform);
            toggle.anchorMin = new Vector2(1f, 0.5f);
            toggle.anchorMax = new Vector2(1f, 0.5f);
            toggle.pivot = new Vector2(1f, 0.5f);
            toggle.sizeDelta = new Vector2(8f, 8f);
            toggle.anchoredPosition = new Vector2(-34f, 0f);
        }

        private void RefreshUI(bool init = true, bool refreshValue = false)
        {
            if (init)
            {
                if (!refreshValue)
                {
                    // disable applyImmediately temporarily, otherwise Init() triggers a call to SetValue, which also calls RefreshUI()
                    _minViewController.applyImmediately = false;
                    _maxViewController.applyImmediately = false;
                }

                _minViewController.Init();
                _maxViewController.Init();

                if (!refreshValue)
                {
                    _minViewController.applyImmediately = true;
                    _maxViewController.applyImmediately = true;
                }
            }

            // reset button state
            _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _minEnabledStagingValue && _minStagingValue > MinValue;
            _minViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _minEnabledStagingValue && (!_maxEnabledStagingValue || _minStagingValue < _maxStagingValue) && _minStagingValue < MaxValue;
            _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "DecButton").interactable = _maxEnabledStagingValue && (!_minEnabledStagingValue || _maxStagingValue > _minStagingValue) && _maxStagingValue > MinValue;
            _maxViewController.GetComponentsInChildren<Button>().First(x => x.name == "IncButton").interactable = _maxEnabledStagingValue && _maxStagingValue < MaxValue;
        }

        public void SetDefaultValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = false;
            _maxEnabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = false;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = false;
            RefreshUI();
        }

        public void ResetValues()
        {
            if (!_isInitialized)
                return;

            _minEnabledStagingValue = _minEnabledAppliedValue;
            _maxEnabledStagingValue = _maxEnabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            _minViewController.GetComponentInChildren<Toggle>().isOn = _minEnabledAppliedValue;
            _maxViewController.GetComponentInChildren<Toggle>().isOn = _maxEnabledAppliedValue;
            RefreshUI();
        }

        public void FilterSongList(ref List<BeatmapDetails> detailsList)
        {
            if (!_isInitialized || (!_minEnabledAppliedValue && !_maxEnabledAppliedValue))
                return;

            for (int i = 0; i < detailsList.Count;)
            {
                if ((_minEnabledAppliedValue && detailsList[i].SongDuration < _minAppliedValue) ||
                    (_maxEnabledAppliedValue && detailsList[i].SongDuration > _maxAppliedValue))
                    detailsList.RemoveAt(i);
                else
                    ++i;
            }
        }
    }
}