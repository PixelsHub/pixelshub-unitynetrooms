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
    public static class XRImmersiveness
    {
        public static bool IsActive 
        {
            get
            {
                if(!isActive.HasValue)
                {
                    isActive = IsImmersiveXRProviderActive();
                    Debug.Assert(false, "Immersiveness should have been checked before any external access.");
                }

                return isActive.Value;
            }
        }

        private static bool? isActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() 
        {
#if UNITY_EDITOR
            if(TrySetImmersivenessByMpPmTags())
                return;
#endif

            isActive = IsImmersiveXRProviderActive();
        }

#if UNITY_EDITOR
        private static bool TrySetImmersivenessByMpPmTags() 
        {
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
#endif

        private static bool IsImmersiveXRProviderActive()
        {
#if !UNITY_EDITOR && !IMMERSIVE_XR_BUILD
            return false;
#endif

            if(XRGeneralSettings.Instance == null)
            {
                Debug.LogError("Immersiveness check failed due to null XRGeneralSettings. Check possible race condition errors.");
                return false;
            }

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
#if UNITY_STANDALONE
            if(IsXRProviderActive<OpenXRLoaderBase>(BuildTargetGroup.Standalone))
            {
                Debug.Log($"Detected Immersive XR for Standalone (OpenXR).");
                return true;
            }
#endif
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