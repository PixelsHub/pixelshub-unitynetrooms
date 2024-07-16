using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PixelsHub.Netrooms
{
    public class NonImmersivePlayerController : MonoBehaviour, IPointerInputReceiver
    {
        public NonImmersiveNavigationContext NavigationContext;

        [SerializeField]
        private new Camera camera;

        [SerializeField]
        private TransformOrbiter cameraOrbiter;

        [Header("Input")]
        [SerializeField]
        private PointerInput pointerInput;

        [SerializeField]
        private LayerMask dragPointerHitMask = 1;

        [Space(8)]
        [SerializeField]
        private float zoomSensitivity = 10;

        [SerializeField]
        private string zoomActionName = "Zoom";

        private float dragStartDistance;
        private float dragStartHitDistance;
        private Vector3 previousDragWorldPosition;

        private InputAction zoomAction;

        private void Start()
        {
            pointerInput.inputReceiver = this;

            zoomAction = InputSystem.actions.FindAction(zoomActionName);
        }

        public void Click(PointerEventData data) 
        {

        }

        public void BeginDrag(PointerEventData data)
        {
            dragStartDistance = Vector3.Distance(cameraOrbiter.orbitPoint, camera.transform.position);

            Ray ray = new(camera.transform.position, camera.transform.forward);
            if(Physics.Raycast(ray, out var hit, dragStartDistance, dragPointerHitMask))
            {
                dragStartHitDistance = Vector3.Distance(hit.point, cameraOrbiter.orbitPoint);
                dragStartDistance = Vector3.Distance(hit.point, camera.transform.position);
            }

            previousDragWorldPosition = ScreenToWorldPoint(data.position);
        }

        public void Drag(PointerEventData data) 
        {
            var worldPosition = ScreenToWorldPoint(data.position);

            Vector3 delta = worldPosition - previousDragWorldPosition;

            previousDragWorldPosition = worldPosition;

#if UNITY_STANDALONE
            switch(data.button)
            {
                case PointerEventData.InputButton.Left:
                    Rotate(delta);
                    break;

                case PointerEventData.InputButton.Middle:
                    Translate(delta);
                    break;
            }
#else
            Rotate(delta);
#endif
        }

        public void EndDrag(PointerEventData data)
        {
        }

        private void Update()
        {
            if(pointerInput.HoveringPointersCount == 0)
                return;

            var zoom = zoomAction.ReadValue<Vector2>();

            cameraOrbiter.distance -= zoom.y * Time.unscaledDeltaTime * zoomSensitivity;
        }

        private void Rotate(Vector2 worldDelta) 
        {
            cameraOrbiter.Rotation += worldDelta.x / dragStartHitDistance * 90;
            cameraOrbiter.Pitch -= worldDelta.y / dragStartHitDistance * 90;
        }

        private void Translate(Vector2 worldDelta) 
        {
            var point = cameraOrbiter.orbitPoint;
            point -= camera.transform.right * worldDelta.x;
            point -= camera.transform.up * worldDelta.y;
            cameraOrbiter.orbitPoint = point;

        }

        private Vector3 ScreenToWorldPoint(Vector3 screenPoint)
        {
            Vector3 viewportPoint = camera.ScreenToViewportPoint(screenPoint);

            float multiplier = 2.0f * dragStartDistance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float localX = (viewportPoint.x - 0.5f) * multiplier * camera.aspect;
            float localY = (viewportPoint.y - 0.5f) * multiplier;

            return new Vector3(localX, localY, dragStartDistance);
        }
    }
}
