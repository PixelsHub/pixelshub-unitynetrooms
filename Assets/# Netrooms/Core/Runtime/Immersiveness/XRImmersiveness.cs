using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
#endif

namespace PixelsHub.Netrooms
{
    public class XRImmersiveness
    {
        public static bool IsActive 
        {
            get
            {
                if(!isActive.HasValue)
                {
                    isActive = IsImmersiveXRProviderActive();
                    Debug.Assert(false);
                }

                return isActive.Value;
            }
        }

        private static bool? isActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() 
        {
#if UNITY_EDITOR
            List<string> mppmTag = new(Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags());
            if(mppmTag.Contains("Immersive"))
            {
                isActive = true;
                return;
            }
            else if(mppmTag.Contains("NotImmersive"))
            {
                isActive = false;
                return;
            }
#endif

            isActive = IsImmersiveXRProviderActive();
        }

        private static bool IsImmersiveXRProviderActive()
        {
            var xrManagerSettings = XRGeneralSettings.Instance.Manager;

            if(xrManagerSettings == null)
                return false;

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_WSA // OpenXR
            foreach(var loader in xrManagerSettings.activeLoaders)
                if(loader is OpenXRLoaderBase)
                {
                    Debug.Log($"Detected Immersive XR for {Application.platform} (OpenXR).");
                    return true;
                }
#endif

#if UNITY_EDITOR // On Editor check for specific platform configurations since XRManagerSettings will be Standalone
#if UNITY_ANDROID
            if(IsXRProviderActive<OpenXRLoaderBase>(BuildTargetGroup.Android))
            {
                Debug.Log($"Detected Immersive XR for Android (OpenXR).");
                return true;
            }
#elif UNITY_WSA
            if(IsXRProviderActive<OpenXRLoaderBase>(BuildTargetGroup.WSA))
            {
                Debug.Log($"Detected Immersive XR for WSA (OpenXR).");
                return true;
            }
#endif
#endif
            Debug.Log("Immersive XR not detected.");
            return false;
        }

#if UNITY_EDITOR
        private static bool IsXRProviderActive<T>(BuildTargetGroup targetGroup) where T : XRLoader
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup);

            if(generalSettings == null)
            {
                Debug.Assert(false, "Missing XR general settings.");
                return false;
            }

            var xrManagerSettings = generalSettings.Manager;
            if(xrManagerSettings == null)
            {
                Debug.Assert(false, "Missing XR general settings manager.");
                return false;
            }

            var loaders = xrManagerSettings.activeLoaders;
            for(int i = 0; i < loaders.Count; i++)
            {
                var loader = loaders[i];

                if(loader is T)
                    return true;
            }

            return false;
        }
#endif
    }
}