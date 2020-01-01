using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnhancedSearchAndFilters.UI.Components
{
    public class EnterExitEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action PointerEntered;
        public event Action PointerExited;

        public bool IsPointedAt { get; private set; } = false;

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            IsPointedAt = true;
            PointerEntered?.Invoke();
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            IsPointedAt = false;
            PointerExited?.Invoke();
        }
    }
}
