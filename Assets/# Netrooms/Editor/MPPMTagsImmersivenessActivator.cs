#if UNITY_EDITOR
using UnityEngine;
using PixelsHub.XR;

namespace PixelsHub.UnityEditorAutomated
{
    public class MPPMTagsImmersivenessActivator : Immersiveness.IEditorActivationProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            Immersiveness.editorActivationProviders.Push(new MPPMTagsImmersivenessActivator());
        }

        public bool TrySetImmersivenessActivation(out bool? isActive)
        {
            isActive = null;

            string[] mppmTag = Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags();
            bool anyTagFound = false;
            foreach(string tag in mppmTag)
            {
                if(tag == "Immersive")
                {
                    Debug.Log("Immersive XR forced active due to MultiplayerPlaymode tag.");
                    isActive = true;
                    anyTagFound = true;
                }
                else if(tag == "NotImmersive")
                {
                    Debug.Log("Immersive XR forced inactive due to MultiplayerPlaymode tag.");
                    isActive = false;
                    anyTagFound = true;
                }
            }

            return anyTagFound;
        }
    }
}
#endif