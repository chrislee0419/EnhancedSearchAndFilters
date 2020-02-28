using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.Attributes;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace EnhancedSearchAndFilters.UI.Components
{
    internal class FilterTutorialPages : MonoBehaviour, INotifiableHost
    {
        public event Action CloseButtonPressed;
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _previousButtonActive = false;
        [UIValue("previous-button-active")]
        public bool PreviousButtonActive
        {
            get => _previousButtonActive;
            set
            {
                if (_previousButtonActive == value)
                    return;

                _previousButtonActive = value;
                NotifyPropertyChanged();
            }
        }
        private bool _nextButtonActive = true;
        [UIValue("next-button-active")]
        public bool NextButtonActive
        {
            get => _nextButtonActive;
            set
            {
                if (_nextButtonActive == value)
                    return;

                _nextButtonActive = value;
                NotifyPropertyChanged();
            }
        }
        private bool _page1Active = true;
        [UIValue("filter-page-active")]
        public bool Page1Active
        {
            get => _page1Active;
            set
            {
                if (_page1Active == value)
                    return;

                _page1Active = value;
                _indicator1.color = value ? SelectedPageIndicatorColor : DefaultPageIndicatorColor;
                NotifyPropertyChanged();
            }
        }
        private bool _page2Active = false;
        [UIValue("filter-loading-page-active")]
        public bool Page2Active
        {
            get => _page2Active;
            set
            {
                if (_page2Active == value)
                    return;

                _page2Active = value;
                _indicator2.color = value ? SelectedPageIndicatorColor : DefaultPageIndicatorColor;
                NotifyPropertyChanged();
            }
        }
        private bool _page3Active = false;
        [UIValue("disable-filter-page-active")]
        public bool Page3Active
        {
            get => _page3Active;
            set
            {
                if (_page3Active == value)
                    return;

                _page3Active = value;
                _indicator3.color = value ? SelectedPageIndicatorColor : DefaultPageIndicatorColor;
                NotifyPropertyChanged();
            }
        }
        private bool _page4Active = false;
        [UIValue("quick-filter-page-active")]
        public bool Page4Active
        {
            get => _page4Active;
            set
            {
                if (_page4Active == value)
                    return;

                _page4Active = value;
                _indicator4.color = value ? SelectedPageIndicatorColor : DefaultPageIndicatorColor;
                NotifyPropertyChanged();
            }
        }
        private bool _page5Active = false;
        [UIValue("quick-filter-page-2-active")]
        public bool Page5Active
        {
            get => _page5Active;
            set
            {
                if (_page5Active == value)
                    return;

                _page5Active = value;
                _indicator5.color = value ? SelectedPageIndicatorColor : DefaultPageIndicatorColor;
                NotifyPropertyChanged();
            }
        }

        private int _currentPage = 1;
        private int CurrentPage
        {
            get => _currentPage;
            set
            {
                value = value < 1 ? 1 : value;
                _currentPage = value > NumberOfPages ? NumberOfPages : value;

                Page1Active = _currentPage == 1;
                Page2Active = _currentPage == 2;
                Page3Active = _currentPage == 3;
                Page4Active = _currentPage == 4;
                Page5Active = _currentPage == 5;

                PreviousButtonActive = _currentPage > 1;
                NextButtonActive = _currentPage < NumberOfPages;
            }
        }

#pragma warning disable CS0649
        [UIComponent("indicator-1")]
        private RawImage _indicator1;
        [UIComponent("indicator-2")]
        private RawImage _indicator2;
        [UIComponent("indicator-3")]
        private RawImage _indicator3;
        [UIComponent("indicator-4")]
        private RawImage _indicator4;
        [UIComponent("indicator-5")]
        private RawImage _indicator5;

        [UIObject("filter-list-example-image")]
        private GameObject _filterListExampleImageGO;
        [UIObject("quick-filter-example-image")]
        private GameObject _quickFilterExampleImageGO;
        [UIObject("saving-quick-filter-example-image")]
        private GameObject _savingQuickFilterExampleImageGO;
#pragma warning restore CS0649

        private bool _isInitialized = false;

        private static readonly Color DefaultPageIndicatorColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color SelectedPageIndicatorColor = new Color(1f, 1f, 1f, 0.5f);

        private static readonly Vector2 OutlineEffectDistance = new Vector2(0.3f, 0.3f);

        private const int NumberOfPages = 5;

        private void Start()
        {
            BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "EnhancedSearchAndFilters.UI.Views.Filters.FilterTutorialView.bsml"), this.gameObject, this);

            _currentPage = 1;
            _indicator1.color = SelectedPageIndicatorColor;
            _indicator2.color = DefaultPageIndicatorColor;
            _indicator3.color = DefaultPageIndicatorColor;
            _indicator4.color = DefaultPageIndicatorColor;
            _indicator5.color = DefaultPageIndicatorColor;

            // add an outline to all images
            var outline = _filterListExampleImageGO.AddComponent<Outline>();
            outline.effectDistance = OutlineEffectDistance;
            outline.effectColor = Color.black;
            outline = _quickFilterExampleImageGO.AddComponent<Outline>();
            outline.effectDistance = OutlineEffectDistance;
            outline.effectColor = Color.black;
            outline = _savingQuickFilterExampleImageGO.AddComponent<Outline>();
            outline.effectDistance = OutlineEffectDistance;
            outline.effectColor = Color.black;

            _isInitialized = true;
        }

        /// <summary>
        /// Show the first tutorial page.
        /// </summary>
        public void ResetPages()
        {
            if (_isInitialized)
                CurrentPage = 1;
        }

        #region BSML Actions
        [UIAction("close-button-clicked")]
        private void OnCloseButtonClicked() => CloseButtonPressed?.Invoke();

        [UIAction("previous-button-clicked")]
        private void OnPreviousButtonClicked() => CurrentPage -= 1;

        [UIAction("next-button-clicked")]
        private void OnNextButtonClicked() => CurrentPage += 1;
        #endregion

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception e)
            {
                Logger.log.Error($"Error invoking PropertyChanged: {e.Message}");
                Logger.log.Error(e);
            }
        }
    }
}
