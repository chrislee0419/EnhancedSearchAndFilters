using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnhancedSearchAndFilters.UI.Components
{
    public class ClickEventHandler : MonoBehaviour, IPointerClickHandler
    {
        public event Action PointerClicked;

        public void OnPointerClick(PointerEventData pointerEventData) => PointerClicked?.Invoke();
    }
}
