using UnityEngine;
using UnityEngine.Events;

namespace PixelsHub.Netrooms
{
    public class XRImmersivePlayEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onPlayModeImmersiveXRDetected;

        [SerializeField]
        private UnityEvent onPlayModeNotImmersiveDetected;

        private void OnEnable()
        {
            if(XRImmersiveness.IsActive)
                onPlayModeImmersiveXRDetected.Invoke();
            else
                onPlayModeNotImmersiveDetected.Invoke();
        }
    }
}
