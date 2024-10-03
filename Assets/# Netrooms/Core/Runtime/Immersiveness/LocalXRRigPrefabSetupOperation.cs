#if UNITY_EDITOR
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;
using PixelsHub.Netrooms;

namespace PixelsHub.UnityEditorOperations
{
    public class LocalXRRigPrefabSetupOperation : MonoBehaviour
    {
        [ContextMenu("Setup Local XRRig (Requires XROrigin in hierarchy)")]
        private void SetupXRRig()
        {
            if(gameObject.GetComponent<XROrigin>() == null)
            {
                Debug.LogError($"No {nameof(XROrigin)} component found.");
                return;
            }
            
            
        }
    }
}
#endif