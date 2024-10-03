using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PixelsHub.Netrooms
{
    public class LocalXRControllerReference : MonoBehaviour
    {
        public static readonly List<LocalXRControllerReference> controllers = new(2);

        public bool IsActive => gameObject.activeInHierarchy;

        public InteractorHandedness Handedness => handedness;

        [SerializeField]
        private InteractorHandedness handedness;

        private void Start()
        {
            Debug.Assert(HandednessNotAlreadyPresent());

            controllers.Add(this);
        }

        private void OnDestroy()
        {
            controllers.Remove(this);
        }

        private bool HandednessNotAlreadyPresent() 
        {
            foreach(var c in controllers)
                if(c.handedness == handedness)
                    return false;
            return true;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Call only from editor code.
        /// </summary>
        public void EditorSetHandedness(InteractorHandedness handedness)
        {
            if(Application.isPlaying)
            {
                Debug.LogError("Cannot perform operation if application is playing.");
                return;
            }
            
            this.handedness = handedness;
        }
#endif
    }
}
