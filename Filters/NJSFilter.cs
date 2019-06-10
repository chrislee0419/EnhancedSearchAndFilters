using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomUI.Settings;
using SongLoaderPlugin.OverrideClasses;

namespace EnhancedSearchAndFilters.Filters
{
    class NJSFilter : IFilter
    {
        public string FilterName { get { return "Note Jump Speed (NJS)"; } }
        public FilterStatus Status { get; private set; } = FilterStatus.NotApplied;
        public bool ApplyFilter {
            get
            {
                return _isApplied;
            }
            set
            {
                _isApplied = value;

                if (value)
                {
                    _enabledAppliedValue = _enabledStagingValue;
                    _minAppliedValue = _minStagingValue;
                    _maxAppliedValue = _maxStagingValue;
                }

                UpdateStatus();
            }
        }

        private FilterControl[] _controls = new FilterControl[3];

        BoolViewController _enabledViewController;
        ListViewController _minViewController;
        ListViewController _maxViewController;

        private bool _enabledStagingValue = false;
        private int _minStagingValue = DefaultMinValue;
        private int _maxStagingValue = DefaultMaxValue;
        private bool _enabledAppliedValue = false;
        private int _minAppliedValue = DefaultMinValue;
        private int _maxAppliedValue = DefaultMaxValue;

        private Toggle[] _difficultyToggles = new Toggle[5];

        private const int DefaultMinValue = 10;
        private const int DefaultMaxValue = 20;

        private bool _isApplied = false;

        public NJSFilter()
        {
            SubMenu submenu = new SubMenu((Transform)null);

            // enabled setting
            _enabledViewController = submenu.AddBool("Enable This Filter");
            _enabledViewController.GetValue += () => _enabledStagingValue;
            _enabledViewController.SetValue += delegate (bool value)
            {
                _enabledStagingValue = value;

                UpdateStatus();
            };
            _enabledViewController.Init();

            var divider = new GameObject("Divider").AddComponent<Image>();
            divider.color = new Color(1f, 1f, 1f, 0.4f);

            var rt = divider.rectTransform;
            rt.SetParent(_enabledViewController.transform);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 0.2f);
            rt.anchoredPosition = new Vector2(0f, -4f);
            _controls[0] = new FilterControl(_enabledViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 10f), Vector2.zero);

            // minimum value setting
            float[] values = Enumerable.Range(1, 50).Select(x => (float)x).ToArray();
            _minViewController = submenu.AddList("Minimum", values);
            _minViewController.GetTextForValue += x => ((int)x).ToString();
            _minViewController.GetValue += () => _minStagingValue;
            _minViewController.SetValue += delegate (float value)
            {
                if (value > _maxStagingValue)
                {
                    // only way i know of refreshing the value in the value displayed
                    _minViewController.Init();
                    return;
                }

                _minStagingValue = (int)value;

                UpdateStatus();
            };
            _minViewController.Init();
            _minViewController.applyImmediately = true;

            rt = _minViewController.transform.Find("NameText") as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 10f);
            rt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Logger.log.Info($"{rt.name}: rect={rt.rect}, offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");

            //rt = _minViewController.transform.Find("Value") as RectTransform;
            //rt.anchorMin = Vector2.zero;
            //rt.anchorMax = new Vector2(1f, 0f);
            //rt.pivot = new Vector2(0.5f, 0f);
            //rt.anchoredPosition = Vector2.zero;
            //rt.sizeDelta = new Vector2(45f, 8f);
            //Logger.log.Info($"{rt.name}: rect={valurteRt.rect}, offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");

            rt = _minViewController.transform.Find("Value").Find("ValueText") as RectTransform;
            rt.SetParent(_minViewController.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            //rt.offsetMin = Vector2.zero;
            //rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 8f);
            Logger.log.Info($"Min{rt.name}: rect={rt.rect}");

            rt = _minViewController.transform.Find("Value").Find("DecButton") as RectTransform;
            rt.SetParent(_minViewController.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(8f, 8f);
            Logger.log.Info($"Min{rt.name}: rect={rt.rect}");

            rt = _minViewController.transform.Find("Value").Find("IncButton") as RectTransform;
            rt.SetParent(_minViewController.transform, false);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(8f, 8f);
            Logger.log.Info($"Min{rt.name}: rect={rt.rect}");

            _controls[1] = new FilterControl(_minViewController.gameObject, new Vector2(0f, 0.95f), new Vector2(0.5f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(0f, -15f));

            // maximum value setting
            _maxViewController = submenu.AddList("Maximum", values);
            _maxViewController.GetTextForValue += x => ((int)x).ToString();
            _maxViewController.GetValue += () => _maxStagingValue;
            _maxViewController.SetValue += delegate (float value)
            {
                if (value < _minStagingValue)
                {
                    // only way i know of refreshing the value in the value displayed
                    _maxViewController.Init();
                    return;
                }

                _maxStagingValue = (int)value;

                UpdateStatus();
            };
            _maxViewController.Init();
            _maxViewController.applyImmediately = true;

            rt = _maxViewController.transform.Find("NameText") as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 10f);
            rt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            rt = _maxViewController.transform.Find("Value") as RectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 8f);

            rt = rt.Find("ValueText") as RectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            Logger.log.Info($"Max{rt.name}: rect={rt.rect}, offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            //rt.sizeDelta = Vector2.zero;

            _controls[2] = new FilterControl(_maxViewController.gameObject, new Vector2(0.5f, 0.95f), new Vector2(1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(0f, -15f));

            //var child = enabled.transform.Find("Value") as RectTransform;
            //child.anchorMin = new Vector2(0.7f, 0f);
            //child.sizeDelta = Vector2.zero;

            //var child2 = child.Find("DecButton") as RectTransform;
            //child2.SetParent(child, false);
            //child2.anchorMin = Vector2.zero;
            //child2.anchorMax = Vector2.zero;
            //child2.pivot = Vector2.zero;
            //child2.anchoredPosition = Vector2.zero;
            //child2.sizeDelta = new Vector2(10f, 10f);

            //child2 = child.Find("ValueText") as RectTransform;
            //child2.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
            //child2.anchorMin = Vector2.zero;
            //child2.anchorMax = Vector2.one;
            //child2.pivot = new Vector2(0.5f, 0.5f);
            //child2.anchoredPosition = Vector2.zero;
            //child2.sizeDelta = new Vector2(20f, 0f);

            //child2 = child.Find("IncButton") as RectTransform;
            //child2.SetParent(child, false);
            //child2.anchorMin = new Vector2(1f, 0f);
            //child2.anchorMax = new Vector2(1f, 1f);
            //child2.pivot = new Vector2(1f, 0.5f);
            //child2.anchoredPosition = Vector2.zero;
            //child2.sizeDelta = new Vector2(10f, 0f);

        }

        public FilterControl[] GetControls()
        {
            return _controls;
        }

        public void SetDefaultValues()
        {
            _enabledStagingValue = false;
            _minStagingValue = DefaultMinValue;
            _maxStagingValue = DefaultMaxValue;

            UpdateStatus();
        }

        public void ResetValues()
        {
            _enabledStagingValue = _enabledAppliedValue;
            _minStagingValue = _minAppliedValue;
            _maxStagingValue = _maxAppliedValue;

            RefreshUI();
            UpdateStatus();
        }

        public void FilterSongList(ref List<IPreviewBeatmapLevel> levels)
        {
            // don't need to check _isApplied, that's done outside of this module
            if (!_enabledAppliedValue)
                return;

            for (int i = 0; i < levels.Count;)
            {
                IPreviewBeatmapLevel level = levels[i];

                if (!(level is CustomLevel))
                {
                    // don't remove beatmaps we can't filter
                    ++i;
                    continue;
                }

                CustomLevel customLevel = level as CustomLevel;

                //if (customLevel.beatmapLevelData.)
            }
        }

        private void RefreshUI()
        {
            _enabledViewController.Init();
            _minViewController.Init();
            _maxViewController.Init();
        }

        private void UpdateStatus()
        {
            if (_enabledAppliedValue && _isApplied)
            {
                if (_enabledStagingValue != _enabledAppliedValue ||
                    _minStagingValue != _minAppliedValue ||
                    _maxStagingValue != _maxAppliedValue)
                    Status = FilterStatus.Changed;
                else
                    Status = FilterStatus.Applied;
            }
            else if (_enabledStagingValue)
            {
                Status = FilterStatus.Changed;
            }
            else
            {
                Status = FilterStatus.NotApplied;
            }
        }
    }
}
