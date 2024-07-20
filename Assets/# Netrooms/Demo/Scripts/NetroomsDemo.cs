using UnityEngine;
using PixelsHub.Netrooms;
using UnityEngine.InputSystem;
using UnityEditor;

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
            // Debug.Log($"{ev.parameters[0]}");
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

    int logEventCount;
    private void OnValidate()
    {
        if(testEvent)
        {
            testEvent = false;

            EditorApplication.delayCall += () => 
            {
                Color color = NetworkPlayer.Local != null ? NetworkPlayer.Local.Color : PlayerColoringScheme.undefinedColor;
                logEventCount++;
                NetworkLogEvents.Add(logEventCount.ToString(), color, new string[] { "hey", "hou" });
            };
        }
    }
}
