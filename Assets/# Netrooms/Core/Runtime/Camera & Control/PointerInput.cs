using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelsHub.Netrooms
{
    public interface IPointerInputReceiver
    {
        public void Click(PointerEventData eventData);

        public void BeginDrag(PointerEventData eventData);

        public void Drag(PointerEventData eventData);

        public void EndDrag(PointerEventData eventData);
    }

    [RequireComponent(typeof(RectTransform))]
    public class PointerInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int DraggingPointersCount{ get; private set; }

        public int HoveringPointersCount { get; private set; }

        public IPointerInputReceiver inputReceiver;

        public void OnPointerClick(PointerEventData eventData)
        {
            inputReceiver.Click(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            inputReceiver.Drag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            DraggingPointersCount++;

            inputReceiver.BeginDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DraggingPointersCount--;

            inputReceiver.EndDrag(eventData);
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