using System.Collections.Generic;
using UnityEngine;

namespace EnhancedSearchAndFilters.Filters
{
    public interface IFilter
    {
        string FilterName { get; }
        FilterStatus Status { get; }
        bool ApplyFilter { get; set; }

        FilterControl[] GetControls();

        void SetDefaultValues();
        void ResetValues();

        void FilterSongList(ref List<IPreviewBeatmapLevel> levels);
    }

    public class FilterControl
    {
        private Vector2 _anchorMin;
        private Vector2 _anchorMax;
        private Vector2 _pivot;
        private Vector2 _sizeDelta;
        private Vector2 _anchoredPosition;

        private GameObject _control;

        private bool _hadBeenInitialized;

        public FilterControl(GameObject control, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            _control = control;
            _control.SetActive(false);

            _anchorMin = anchorMin;
            _anchorMax = anchorMax;
            _pivot = pivot;
            _sizeDelta = sizeDelta;
            _anchoredPosition = anchoredPosition;
        }

        public void Init(Transform transform)
        {
            _control.transform.SetParent(transform, false);

            RectTransform rt = _control.transform as RectTransform;
            rt.anchorMin = _anchorMin;
            rt.anchorMax = _anchorMax;
            rt.pivot = _pivot;
            rt.sizeDelta = _sizeDelta;
            rt.anchoredPosition = _anchoredPosition;

            Logger.log.Info($"control - {rt.name}: rect={(rt as RectTransform).rect}, localPos={rt.localPosition}, parent={rt.parent?.name}");
            //Logger.log.Info($"control - {rt.name}: anchorMin={(rt as RectTransform).anchorMin}, anchorMax={(rt as RectTransform).anchorMax}");
            //Logger.log.Info($"control - {rt.name}: pivot={(rt as RectTransform).pivot}, anchoredPos={(rt as RectTransform).anchoredPosition}");
            for (int i = 0; i < _control.transform.childCount; ++i)
            {
                var t = _control.transform.GetChild(i);
                if (t.name != "Value")
                    continue;
                Logger.log.Info($"{t.name}: rect={(t as RectTransform).rect}, localPos={t.localPosition}, parent={t.parent?.name}");
                Logger.log.Info($"{t.name}: anchorMin={(t as RectTransform).anchorMin}, anchorMax={(t as RectTransform).anchorMax}");
                Logger.log.Info($"{t.name}: pivot={(t as RectTransform).pivot}, anchoredPos={(t as RectTransform).anchoredPosition}");
                Logger.log.Info($"{t.name}: offsetMin={(t as RectTransform).offsetMin}, offsetMax={(t as RectTransform).offsetMax}");
                Logger.log.Info($"{t.name}: localRotation={(t as RectTransform).localRotation}, localScale={(t as RectTransform).localScale}");

                for (int j = 0; j < t.childCount; ++j)
                {
                    var t2 = t.GetChild(j);
                    Logger.log.Info($"{t2.name}: rect={(t2 as RectTransform).rect}, localPos={t2.localPosition}, parent={t2.parent?.name}");
                    Logger.log.Info($"{t2.name}: anchorMin={(t2 as RectTransform).anchorMin}, anchorMax={(t2 as RectTransform).anchorMax}");
                    Logger.log.Info($"{t2.name}: pivot={(t2 as RectTransform).pivot}, anchoredPos={(t2 as RectTransform).anchoredPosition}");
                    Logger.log.Info($"{t2.name}: offsetMin={(t2 as RectTransform).offsetMin}, offsetMax={(t2 as RectTransform).offsetMax}");
                    Logger.log.Info($"{t2.name}: localRotation={(t2 as RectTransform).localRotation}, localScale={(t2 as RectTransform).localScale}");
                }
            }

            _hadBeenInitialized = true;
        }

        public void EnableControl()
        {
            if (_hadBeenInitialized)
                _control.SetActive(true);
        }

        public void DisableControl()
        {
            _control.SetActive(false);
        }
    }

    public enum FilterStatus
    {
        NotApplied,
        Changed,
        Applied
    }
}
