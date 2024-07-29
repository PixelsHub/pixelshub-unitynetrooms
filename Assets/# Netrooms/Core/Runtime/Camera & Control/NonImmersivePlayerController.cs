using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PixelsHub.Netrooms
{
    public class NonImmersivePlayerController : MonoBehaviour
    {
        public NonImmersiveNavigationContext NavigationContext;

        [SerializeField]
        private new Camera camera;

        [SerializeField]
        private TransformOrbiter cameraOrbiter;

        [Header("Input")]
        [SerializeField]
        private LayerMask dragPointerHitMask = 1;

        [Space(8)]
        [SerializeField]
        private float rotationSensitivity = 160;

        [SerializeField]
        private float zoomSensitivity = 16;

        [SerializeField]
        private string zoomActionName = "Zoom";

        private float dragStartDistance;
        private Vector3 previousDragWorldPosition;

        private InputAction zoomAction;

        private void Start()
        {
            PointerInput.OnDragBegan += BeginDrag;
            PointerInput.OnDragged += Drag;

            zoomAction = InputSystem.actions.FindAction(zoomActionName);
        }

        private void OnDestroy()
        {
            PointerInput.OnDragBegan -= BeginDrag;
            PointerInput.OnDragged -= Drag;
        }

        public void BeginDrag(PointerEventData data)
        {
            dragStartDistance = Vector3.Distance(cameraOrbiter.orbitPoint, camera.transform.position);

            Ray ray = new(camera.transform.position, camera.transform.forward);
            if(Physics.Raycast(ray, out var hit, dragStartDistance, dragPointerHitMask))
            {
                dragStartDistance = Vector3.Distance(hit.point, camera.transform.position);
            }

            previousDragWorldPosition = ScreenToWorldPoint(data.position);
        }

        public void Drag(PointerEventData data) 
        {
            Vector2 ScreenDelta() => data.delta / new Vector2(Screen.width, Screen.height);

            var worldPosition = ScreenToWorldPoint(data.position);

#if UNITY_EDITOR || UNITY_STANDALONE
            switch(data.button)
            {
                case PointerEventData.InputButton.Left:
                    Rotate(ScreenDelta());
                    break;

                case PointerEventData.InputButton.Middle:
                    Translate(worldPosition - previousDragWorldPosition);
                    break;
            }
#else
            Rotate(ScreenDelta());
#endif

            previousDragWorldPosition = worldPosition;
        }

        private void Update()
        {
            if(PointerInput.HoveringPointersCount > 0)
                ProcessZoom();
        }

        private void ProcessZoom() 
        {
            var zoom = zoomAction.ReadValue<Vector2>();
            cameraOrbiter.distance -= zoom.y * Time.unscaledDeltaTime * zoomSensitivity;
        }

        private void Rotate(Vector2 screenDelta) 
        {
            cameraOrbiter.Rotation += screenDelta.x * rotationSensitivity;
            cameraOrbiter.Pitch -= screenDelta.y * rotationSensitivity;
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
