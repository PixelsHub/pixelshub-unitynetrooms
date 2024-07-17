using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(RectTransform))]
    public class PointerInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static event Action<PointerEventData> OnClicked;
        public static event Action<PointerEventData> OnDragBegan;
        public static event Action<PointerEventData> OnDragged;
        public static event Action<PointerEventData> OnDragEnded;

        public static int DraggingPointersCount{ get; private set; }

        public static int HoveringPointersCount { get; private set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(DraggingPointersCount > 0)
                return;

            OnClicked?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragged?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            DraggingPointersCount++;

            OnDragBegan?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DraggingPointersCount--;

            OnDragEnded?.Invoke(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HoveringPointersCount++;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoveringPointersCount--;

            Debug.Assert(HoveringPointersCount >= 0);
        }
    }
}