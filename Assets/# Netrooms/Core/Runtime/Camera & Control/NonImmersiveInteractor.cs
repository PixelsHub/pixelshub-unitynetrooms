using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PixelsHub.Netrooms
{
    public class NonImmersiveInteractor : XRBaseInputInteractor
    {
        private Camera localCamera;

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

            if(selectableTarget != null)
                targets.Add(selectableTarget);
            else if(hoverTarget != null) // In case target is hoverable and not selectable
                targets.Add(hoverTarget);
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
            if(localCamera == null)
                localCamera = LocalPlayerRig.Instance.Camera;

            UpdateHovering();
        }

        private Ray GetMouseRay() => localCamera.ScreenPointToRay(PointerInput.MousePosition);

        private void UpdateHovering() 
        {
            if(selectableTarget != null) // Ignore hovering while selecting
                return;

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

            dragStartRay = localCamera.ScreenPointToRay(eventData.position);

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
                var ray = localCamera.ScreenPointToRay(eventData.position);
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

        protected virtual bool TryGetInteractableTarget(Ray pointerRay, out IXRInteractable target)
        {
            target = null;

            int count = Physics.SphereCastNonAlloc(pointerRay, sphereCastRadius, raycastHits, raycastDistance, raycastMask);

            float closestSqrDistance = Mathf.Infinity;

            for(int i = 0; i < count; i++)
            {
                var raycastHit = raycastHits[i];

                if(interactionManager.TryGetInteractableForCollider(raycastHit.collider, out var possibleTarget))
                {
                    float sqrDistance = (raycastHit.point - pointerRay.origin).sqrMagnitude;

                    if(sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
                        target = possibleTarget;
                    }
                }
            }

            if(target != null)
                return true;

            return false;
        }
    }
}
