using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedSearchAndFilters.Filters
{
    public interface IFilter
    {
        string FilterName { get; }
        FilterStatus Status { get; }
        bool ApplyFilter { get; set; }
        FilterControl[] Controls { get; }

        event Action SettingChanged;

        void Init();

        void SetDefaultValues();
        void ResetValues();

        void FilterSongList(ref List<BeatmapDetails> detailsList);
    }

    public enum FilterStatus
    {
        NotAppliedAndDefault,
        NotAppliedAndChanged,
        Applied,
        AppliedAndChanged
    }

    public class FilterControl
    {
        private Vector2 _anchorMin;
        private Vector2 _anchorMax;
        private Vector2 _pivot;
        private Vector2 _sizeDelta;
        private Vector2 _anchoredPosition;

        private GameObject _control;
        private Action OnEnable;

        public bool HasBeenInitialized { get; private set; }

        /// <summary>
        /// Creates a new FilterControl object, which contains a GameObject that controls some or many UI elements for a filter.
        /// </summary>
        /// <param name="control">GameObject containing one or more filter UI element(s).</param>
        /// <param name="anchorMin"></param>
        /// <param name="anchorMax"></param>
        /// <param name="pivot"></param>
        /// <param name="sizeDelta"></param>
        /// <param name="anchoredPosition">Anchored position of the control on the parent transform set by Init().</param>
        /// <param name="onEnable">A function that is called every time the filter is displayed. Used to update the state of the control if it is reset when it is disabled.</param>
        public FilterControl(GameObject control, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, Action onEnable=null)
        {
            _control = control;
            _control.SetActive(false);

            _anchorMin = anchorMin;
            _anchorMax = anchorMax;
            _pivot = pivot;
            _sizeDelta = sizeDelta;
            _anchoredPosition = anchoredPosition;

            OnEnable = onEnable;
        }

        /// <summary>
        /// Initializes the filter control elements. Attaches the elements to a parent transform.
        /// </summary>
        /// <param name="transform">The transform that will become the parent to the control element.</param>
        public void Init(Transform transform)
        {
            RectTransform rt = _control.transform as RectTransform;
            rt.SetParent(transform, false);
            rt.anchorMin = _anchorMin;
            rt.anchorMax = _anchorMax;
            rt.pivot = _pivot;
            rt.sizeDelta = _sizeDelta;
            rt.anchoredPosition = _anchoredPosition;

            HasBeenInitialized = true;
        }

        /// <summary>
        /// Set the filter control elements to be displayed.
        /// </summary>
        public void EnableControl()
        {
            if (HasBeenInitialized)
            {
                _control.SetActive(true);
                OnEnable?.Invoke();
            }
        }

        /// <summary>
        /// Hides the filter control elements.
        /// </summary>
        public void DisableControl()
        {
            _control.SetActive(false);
        }
    }
}
