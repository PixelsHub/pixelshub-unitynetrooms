using UnityEngine;
using PixelsHub.Netrooms;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NetroomsDemo : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    [SerializeField]
    private NetworkPlayerSpatialPinger pinger;

    [Header("Test bools")]
    [SerializeField]
    private bool testEvent;

    private void Start()
    {
        var bounds = target.GetComponent<Renderer>().bounds;

        NonImmersiveNavigationContext context = new()
        {
            originPosition = bounds.center,
            distance = bounds.size.magnitude
        };

        if(!XRImmersiveness.IsActive)
        {
            var playerController = FindFirstObjectByType<NonImmersivePlayerController>();
            if(playerController != null)
            {
                playerController.NavigationContext = context;
            }
        }

        NetworkLogEvents.OnEventInvoked += (ev) => 
        {
            Debug.Log($"{ev.parameters[0]} - {ev.parameters[1]}");
        };

        PointerInput.OnClicked += (ev) => 
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if(Physics.Raycast(ray, out var hit, 100, 1))
            {
                pinger.Ping(hit.point, Quaternion.LookRotation(hit.normal));
            }
        };
    }

    private void OnValidate()
    {
        if(testEvent)
        {
            testEvent = false;

            ulong player = NetworkPlayer.Local != null ? NetworkPlayer.Local.OwnerClientId : 0;
            NetworkLogEvents.Add("EXAMPLE", player, new string[] { "hey", "hou" });
        }
    }

    public void Click(PointerEventData eventData)
    {
    }

    public void BeginDrag(PointerEventData eventData)
    {
    }

    public void Drag(PointerEventData eventData)
    {
    }

    public void EndDrag(PointerEventData eventData)
    {
    }
}
