using UnityEngine;
using PixelsHub.Netrooms;

public class NetroomsDemo : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

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

        NetworkEvents.OnEventInvoked += (ev) => 
        {
            Debug.Log($"{ev.id} - {ev.player} - {ev.parameters[0]} - {ev.parameters[1]}");
        };
    }

    private void OnValidate()
    {
        if(testEvent)
        {
            testEvent = false;

            NetworkEvents.Instance.Add("EXAMPLE", NetworkPlayer.Local.OwnerClientId, new string[] { "hey", "hou" });
        }
    }
}
