using UnityEngine;
using UnityEngine.Events;

namespace PixelsHub.Netrooms
{
    public class XRImmersivenessActiveStateEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onImmersivenessActiveDetected;

        [SerializeField]
        private UnityEvent onImmersivenessInactiveDetected;

        private void OnEnable()
        {
            if(XRImmersiveness.IsActive)
                onImmersivenessActiveDetected.Invoke();
            else
                onImmersivenessInactiveDetected.Invoke();
        }
    }
}
