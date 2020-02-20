using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Screen = HMUI.Screen;
using EnhancedSearchAndFilters.Filters;
using EnhancedSearchAndFilters.UI.Components;
using EnhancedSearchAndFilters.UI.Components.ButtonPanelModules;

namespace EnhancedSearchAndFilters.UI
{
    internal class ButtonPanel : PersistentSingleton<ButtonPanel>
    {
        public event Action SearchButtonPressed;
        public event Action FilterButtonPressed;
        public event Action ClearFilterButtonPressed;
        public event Action SortButtonPressed;
        public event Action<QuickFilter> ApplyQuickFilterPressed;

        public bool Initialized { get; private set; } = false;

        private GameObject _container;
        private bool _inRevealAnimation = false;
        private IEnumerator _expandAnimation;
        private IEnumerator _contractAnimation;
        private float _expandedXSize;
        private EnterExitEventHandler _hoverEventHandler;

        private MainModule _mainModule;
        private SortModeModule _sortModeModule;
        private QuickFilterModule _quickFilterModule;

        private const float DefaultYScale = 0.02f;
        private const float HiddenYScale = 0f;

        private const float DefaultXSize = 28f;
        private static readonly WaitForSeconds CollapseAnimationDelay = new WaitForSeconds(1.2f);

        public void Setup(bool forceReinit = false)
        {
            if (Initialized)
            {
                if (!forceReinit)
                    return;

                if (_container != null)
                {
                    DestroyImmediate(_container);
                    _container = null;
                    _mainModule = null;
                    _sortModeModule = null;
                }
            }

            var topScreen = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "TopScreen");

            _container = Instantiate(topScreen, topScreen.transform.parent, true);
            _container.name = "EnhancedSearchAndFiltersButtonPanel";
            _container.AddComponent<RectMask2D>();

            // always render this screen in front of the title view controller's screen
            var canvas = _container.GetComponent<Canvas>();
            canvas.sortingOrder += 1;

            // expand screen to reveal sort mode buttons
            _hoverEventHandler = _container.AddComponent<EnterExitEventHandler>();
            _hoverEventHandler.PointerEntered += delegate ()
            {
                if (_inRevealAnimation)
                {
                    return;
                }
                else if (_contractAnimation != null)
                {
                    StopCoroutine(_contractAnimation);
                    _contractAnimation = null;
                }

                _expandAnimation = ExpandAnimationCoroutine();
                StartCoroutine(_expandAnimation);
            };
            _hoverEventHandler.PointerExited += delegate ()
            {
                if (_inRevealAnimation)
                    return;

                bool immediate = false;
                if (_expandAnimation != null)
                {
                    StopCoroutine(_expandAnimation);
                    _expandAnimation = null;
                    immediate = true;
                }
                else if (_contractAnimation != null)
                {
                    StopCoroutine(_contractAnimation);
                    _contractAnimation = null;
                }

                _contractAnimation = ContractAnimationCoroutine(immediate);
                StartCoroutine(_contractAnimation);
            };

            Destroy(_container.GetComponentInChildren<SetMainCameraToCanvas>(true));
            Destroy(_container.transform.Find("TitleViewController").gameObject);
            Destroy(_container.GetComponentInChildren<Screen>(true));
            Destroy(_container.GetComponentInChildren<HorizontalLayoutGroup>(true));

            // position the screen
            var rt = _container.transform as RectTransform;
            rt.sizeDelta = new Vector2(DefaultXSize, 30f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(1.6f, 2.44f);
            rt.localRotation = Quaternion.Euler(345f, 0f, 0f);
            rt.localPosition += new Vector3(0f, 0f, -0.001f);

            // initialize modules
            // - unexpanded container is of size 28w x 30h
            // - usable area minus default padding (3) for each module is 22w x 24h
            Vector2 anchorMin = new Vector2(1f, 0f);
            Vector2 anchorMax = Vector2.one;
            Vector2 sizeDelta = new Vector2(28f, 0f);
            Vector2 anchoredPos = new Vector2(-14f, 0f);
            _expandedXSize = 0f;

            if (!PluginConfig.DisableSearch || !PluginConfig.DisableFilters)
            {
                _mainModule = new GameObject("MainModule").AddComponent<MainModule>();
                _mainModule.SearchButtonPressed += () => SearchButtonPressed?.Invoke();
                _mainModule.FilterButtonPressed += () => FilterButtonPressed?.Invoke();
                _mainModule.ClearFilterButtonPressed += () => ClearFilterButtonPressed?.Invoke();

                _mainModule.RectTransform.SetParent(_container.transform, false);
                _mainModule.RectTransform.anchorMin = anchorMin;
                _mainModule.RectTransform.anchorMax = anchorMax;
                _mainModule.RectTransform.sizeDelta = sizeDelta;
                _mainModule.RectTransform.anchoredPosition = anchoredPos;
                anchoredPos -= sizeDelta;
                _expandedXSize += sizeDelta.x;
            }

            _sortModeModule = new GameObject("SortModeModule").AddComponent<SortModeModule>();
            _sortModeModule.SortButtonPressed += () => SortButtonPressed?.Invoke();

            _sortModeModule.RectTransform.SetParent(_container.transform, false);
            _sortModeModule.RectTransform.anchorMin = anchorMin;
            _sortModeModule.RectTransform.anchorMax = anchorMax;
            _sortModeModule.RectTransform.sizeDelta = sizeDelta;
            _sortModeModule.RectTransform.anchoredPosition = anchoredPos;
            anchoredPos -= sizeDelta;
            _expandedXSize += sizeDelta.x;

            if (!PluginConfig.DisableFilters)
            {
                _quickFilterModule = new GameObject("QuickFilterModule").AddComponent<QuickFilterModule>();
                _quickFilterModule.ApplyQuickFilterPressed += (quickFilter) => ApplyQuickFilterPressed?.Invoke(quickFilter);

                _quickFilterModule.RectTransform.SetParent(_container.transform, false);
                _quickFilterModule.RectTransform.anchorMin = anchorMin;
                _quickFilterModule.RectTransform.anchorMax = anchorMax;
                _quickFilterModule.RectTransform.sizeDelta = sizeDelta;
                _quickFilterModule.RectTransform.anchoredPosition = anchoredPos;
                anchoredPos -= sizeDelta;
                _expandedXSize += sizeDelta.x;
            }

            Initialized = true;

            HidePanel(true);
        }

        public void DisablePanel()
        {
            if (!Initialized)
                return;

            if (_container != null)
            {
                DestroyImmediate(_container);
                _container = null;
            }

            Initialized = false;
        }

        public void ShowPanel(bool immediately = false)
        {
            if (!Initialized || _container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = DefaultYScale;
                this._container.transform.localScale = localScale;

                // reset size delta as well
                var rt = (this._container.transform as RectTransform);
                var sizeDelta = rt.sizeDelta;
                sizeDelta.x = DefaultXSize;
                rt.sizeDelta = sizeDelta;

                _container.SetActive(true);
                return;
            }

            _container.SetActive(true);

            _inRevealAnimation = true;
            StopAllCoroutines();
            StartCoroutine(RevealAnimationCoroutine(DefaultYScale));
        }

        public void HidePanel(bool immediately = false)
        {
            if (!Initialized || !_container.activeSelf)
                return;

            if (immediately)
            {
                Vector3 localScale = this._container.transform.localScale;
                localScale.y = HiddenYScale;
                this._container.transform.localScale = localScale;

                // reset size delta as well
                var rt = (this._container.transform as RectTransform);
                var sizeDelta = rt.sizeDelta;
                sizeDelta.x = DefaultXSize;
                rt.sizeDelta = sizeDelta;

                _container.SetActive(false);

                return;
            }

            _inRevealAnimation = true;
            StopAllCoroutines();
            StartCoroutine(RevealAnimationCoroutine(HiddenYScale, true));
        }

        private IEnumerator RevealAnimationCoroutine(float destAnimationValue, bool disableOnFinish = false)
        {
            yield return null;
            yield return null;

            Vector3 localScale = this._container.transform.localScale;
            while (Mathf.Abs(localScale.y - destAnimationValue) > 0.0001f)
            {
                float num = (localScale.y > destAnimationValue) ? 30f : 16f;
                localScale.y = Mathf.Lerp(localScale.y, destAnimationValue, Time.deltaTime * num);
                this._container.transform.localScale = localScale;

                yield return null;
            }

            localScale.y = destAnimationValue;
            this._container.transform.localScale = localScale;

            // reset size delta as well
            var rt = (this._container.transform as RectTransform);
            var sizeDelta = rt.sizeDelta;
            sizeDelta.x = DefaultXSize;
            rt.sizeDelta = sizeDelta;

            _container.SetActive(!disableOnFinish);
            _inRevealAnimation = false;
        }

        private IEnumerator ExpandAnimationCoroutine()
        {
            RectTransform rt = this._container.transform as RectTransform;
            Vector3 sizeDelta = rt.sizeDelta;

            while (Mathf.Abs(sizeDelta.x - _expandedXSize) > 0.0001f)
            {
                sizeDelta.x = Mathf.Lerp(sizeDelta.x, _expandedXSize, Time.deltaTime * 45);
                rt.sizeDelta = sizeDelta;

                yield return null;
            }

            sizeDelta.x = _expandedXSize;
            rt.sizeDelta = sizeDelta;

            _expandAnimation = null;
        }

        private IEnumerator ContractAnimationCoroutine(bool immediate = false)
        {
            RectTransform rt = this._container.transform as RectTransform;
            Vector3 sizeDelta = rt.sizeDelta;

            if (!immediate)
            {
                yield return CollapseAnimationDelay;
                if (_hoverEventHandler.IsPointedAt)
                    yield break;
            }

            while (Mathf.Abs(sizeDelta.x - DefaultXSize) > 0.0001f)
            {
                sizeDelta.x = Mathf.Lerp(sizeDelta.x, DefaultXSize, Time.deltaTime * 30);
                rt.sizeDelta = sizeDelta;

                yield return null;
            }

            sizeDelta.x = DefaultXSize;
            rt.sizeDelta = sizeDelta;

            _contractAnimation = null;
        }

        public void SetFilterStatus(bool filterApplied)
        {
            if (Initialized && _mainModule != null)
                _mainModule.SetFilterStatus(filterApplied);
        }

        public void UpdateSortButtons()
        {
            if (Initialized && _sortModeModule != null)
                _sortModeModule.UpdateSortButtons();
        }
    }
}
