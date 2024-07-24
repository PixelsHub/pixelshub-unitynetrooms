using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(RectTransform))]
    public class PointerInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static event Action<PointerEventData> OnPointerPressed;
        public static event Action<PointerEventData> OnPointerReleased;
        public static event Action<PointerEventData> OnClicked;
        public static event Action<PointerEventData> OnDragBegan;
        public static event Action<PointerEventData> OnDragged;
        public static event Action<PointerEventData> OnDragEnded;
        public static event Action<PointerEventData> OnHoverBegan;
        public static event Action<PointerEventData> OnHoverEnded;

        public static Vector2 MousePosition => MousePositionAction.ReadValue<Vector2>();

        public static InputAction MousePositionAction { get; private set; }

        public static int DraggingPointersCount{ get; private set; }

        public static int HoveringPointersCount { get; private set; }
        
        [SerializeField]
        private string mousePositionActionName = "Point";

        public void OnPointerClick(PointerEventData eventData)
        {
            if(DraggingPointersCount > 0)
                return;

            OnClicked?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerPressed?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(DraggingPointersCount > 0)
                return;

            Debug.Assert(HoveringPointersCount > 0);

            OnPointerReleased?.Invoke(eventData);
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

            OnHoverBegan?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoveringPointersCount--;

            OnHoverEnded?.Invoke(eventData);

            Debug.Assert(HoveringPointersCount >= 0);
        }

        private void Start()
        {
            MousePositionAction = InputSystem.actions.FindAction(mousePositionActionName);

            if(MousePositionAction == null)
            {
                Debug.LogError($"Could not find Input Action {MousePositionAction}.");
            }
        }
    }
}