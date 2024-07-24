using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PixelsHub.Netrooms
{
    public class NonImmersiveInteractor : XRBaseInputInteractor
    {
        private Camera LocalCamera => LocalPlayerRig.Instance.Camera;

        private IXRInteractable target;
        private IXRHoverInteractable hoverTarget;
        private IXRSelectInteractable selectableTarget;

        [Header("Interaction Raycasting")]
        [SerializeField]
        private float sphereCastRadius = 0.02f;

        [SerializeField]
        private float raycastDistance = 20;

        [SerializeField]
        private LayerMask raycastMask = 1;

        [SerializeField]
        private int maximumSimulatenousHits = 8;

        private RaycastHit[] raycastHits;

        private Ray dragStartRay;
        private float dragDistance;
        private Vector3 dragOffset;

        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if(hoverTarget != null)
                targets.Add(hoverTarget);
            else if(selectableTarget != null) // In case target is selectable and not hoverable
                targets.Add(selectableTarget);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            raycastHits = new RaycastHit[maximumSimulatenousHits];

            PointerInput.OnDragBegan += HandleDragBegan;
            PointerInput.OnDragged += HandleDragged;
            PointerInput.OnDragEnded += HandleDragEnded;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PointerInput.OnDragBegan -= HandleDragBegan;
            PointerInput.OnDragged -= HandleDragged;
            PointerInput.OnDragEnded -= HandleDragEnded;
        }

        private void Update()
        {
            UpdateHovering();
        }

        private Ray GetMouseRay() => LocalCamera.ScreenPointToRay(PointerInput.MousePosition);

        private void UpdateHovering() 
        {
            if(PointerInput.HoveringPointersCount > 0 && TryGetInteractableTarget(GetMouseRay(), out var newTarget))
            {
                if(newTarget != target)
                {
                    if(hoverTarget != null)
                        interactionManager.HoverExit(this, hoverTarget);
                    
                    if(newTarget is IXRHoverInteractable newHoverTarget)
                    {
                        hoverTarget = newHoverTarget;
                        interactionManager.HoverEnter(this, hoverTarget);
                    }

                    target = newTarget;
                }
            }
            else
            {
                if(target != null)
                {
                    if(hoverTarget != null)
                    {
                        interactionManager.HoverExit(this, hoverTarget);
                        hoverTarget = null;
                    }

                    if(selectableTarget == null)
                        target = null;
                }
            }
        }

        private Vector3 GetRayPoint(Ray ray, float distance) => ray.origin + ray.direction * distance;

        private void HandleDragBegan(PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Right)
                return;

            dragStartRay = LocalCamera.ScreenPointToRay(eventData.position);

            if(target != null || TryGetInteractableTarget(dragStartRay, out target))
            {
                if(target is IXRSelectInteractable newSelectableTarget)
                {
                    selectableTarget = newSelectableTarget;
                    transform.position = target.transform.position;
                    dragDistance = (transform.position - dragStartRay.origin).magnitude;
                    dragOffset = transform.position - GetRayPoint(dragStartRay, dragDistance);
                    interactionManager.SelectEnter(this, selectableTarget);
                }
            }
        }

        private void HandleDragged(PointerEventData eventData)
        {
            if(eventData.button != PointerEventData.InputButton.Right)
                return;

            if(selectableTarget != null)
            {
                var ray = LocalCamera.ScreenPointToRay(eventData.position);
                transform.position = GetRayPoint(ray, dragDistance) + dragOffset;
            }
        }

        private void HandleDragEnded(PointerEventData eventData)
        {
            if(target != null)
            {
                if(selectableTarget != null)
                {
                    interactionManager.SelectExit(this, selectableTarget);
                    selectableTarget = null;
                }

                if(hoverTarget == null)
                    target = null;
            }
        }

        private bool TryGetInteractableTarget(Ray pointerRay, out IXRInteractable target)
        {
            int count = Physics.SphereCastNonAlloc(pointerRay, sphereCastRadius, raycastHits, raycastDistance, raycastMask);

            return TryGetInteractableTarget(count, raycastHits, out target);
        }

        private bool TryGetInteractableTarget(int raycastHitCount, RaycastHit[] hits, out IXRInteractable target)
        {
            for(int i = 0; i < raycastHitCount; i++)
            {
                var raycastHit = hits[i];

                if(interactionManager.TryGetInteractableForCollider(raycastHit.collider, out target))
                    return true;
            }

            target = null;
            return false;
        }
    }
}
