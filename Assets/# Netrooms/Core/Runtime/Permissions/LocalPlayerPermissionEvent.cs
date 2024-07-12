using UnityEngine;
using UnityEngine.Events;

namespace PixelsHub.Netrooms
{
    public class LocalPlayerPermissionEvent : MonoBehaviour
    {
        [SerializeField]
        private string permissionCode;

        [Space(8)]
        [SerializeField]
        private UnityEvent onPlayerIsAllowed;

        [SerializeField]
        private UnityEvent onPlayerIsNotAllowed;

        private void OnEnable()
        {
            Check();

            PlayerPermissions.OnPermissionsChanged += Check;
        }

        private void OnDisable()
        {
            PlayerPermissions.OnPermissionsChanged -= Check;
        }

        private void Check() 
        {
            if(PlayerPermissions.CheckLocalPlayer(permissionCode))
                onPlayerIsAllowed.Invoke();
            else
                onPlayerIsNotAllowed.Invoke();
        }
    }
}
