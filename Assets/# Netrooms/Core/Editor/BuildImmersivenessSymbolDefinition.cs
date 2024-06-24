#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;

namespace UnityEditor.PixelsHub
{
    public class BuildImmersivenessSymbolDefinition : IPreprocessBuildWithReport
    {
        public const string immersiveDefineSymbol = "IMMERSIVE_XR_BUILD";

        public int callbackOrder => 0;

        private static readonly Dictionary<BuildTargetGroup, NamedBuildTarget> buildTargets = new()
        {
            { BuildTargetGroup.Standalone, NamedBuildTarget.Standalone },
            { BuildTargetGroup.Android, NamedBuildTarget.Android }
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            // Ensure define symbols when building
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

            if(buildTargets.TryGetValue(buildTarget, out var name))
                ProcessXRProviderToDefineSymbol<OpenXRLoaderBase>(buildTarget, name);
            else
                EditorUtility.DisplayDialog("XR build settings", $"Build target not registered: {buildTarget}", "Ok");
        }

        public static void ProcessProjectDefineSymbols(BuildTargetGroup targetGroup, bool lockAssemblyReload = true)
        {
            if(lockAssemblyReload)
                EditorApplication.LockReloadAssemblies();

            if(buildTargets.TryGetValue(targetGroup, out var name))
                ProcessXRProviderToDefineSymbol<OpenXRLoaderBase>(targetGroup, name);

            if(lockAssemblyReload)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.UnlockReloadAssemblies();
                    AssetDatabase.Refresh();
                };
            }
            else
                AssetDatabase.Refresh();
        }

        private static void ProcessXRProviderToDefineSymbol<T>(BuildTargetGroup group, NamedBuildTarget name) where T : XRLoader
        {
            if(IsXRProviderActive<T>(group))
            {
                if(ProjectSettingsUtilities.AddDefineSymbol(immersiveDefineSymbol, name))
                {
                    string text = $"Added define symbol  {immersiveDefineSymbol}  for {name.TargetName} build.";
                    EditorUtility.DisplayDialog("XR build settings", text, "Ok");
                }
            }
            else
            {
                if(ProjectSettingsUtilities.RemoveDefineSymbol(immersiveDefineSymbol, name))
                {
                    string text = $"Removed define symbol  {immersiveDefineSymbol}  for {name.TargetName} build.";
                    EditorUtility.DisplayDialog("XR build settings", text, "Ok");
                }
            }
        }

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
    }
}
#endif