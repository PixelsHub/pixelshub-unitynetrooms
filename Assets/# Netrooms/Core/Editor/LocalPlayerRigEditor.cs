#if UNITY_EDITOR
using System;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEditor;

namespace PixelsHub.Netrooms.CustomUnityEditor
{
    [CustomEditor(typeof(LocalPlayerRig))]
    public class LocalPlayerRigEditor : Editor
    {
        private SerializedProperty pivotProperty;
        private SerializedProperty cameraProperty;

        private void OnEnable()
        {
            pivotProperty = serializedObject.FindProperty("pivot");
            cameraProperty = serializedObject.FindProperty("camera");
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LocalPlayerRig rigComponent = (LocalPlayerRig)target;
            
            if(IsPrefabAsset(rigComponent.gameObject))
                return;
            
            EditorGUILayout.Space();

            if(GUILayout.Button("Setup prefab for Immersive XR"))
            {
                const string message = "Are you sure you want to setup this prefab? This process may destroy existing Netrooms XR components.";
                    
                if(EditorUtility.DisplayDialog("Prefab setup", message, "Proceed", "Cancel"))
                {
                    SetupForImmersiveXR(rigComponent);
                }
            }
        }

        private void SetupForImmersiveXR(LocalPlayerRig rig)
        {
            if(!rig.TryGetComponent(out XROrigin origin))
            {
                origin = rig.GetComponentInChildren<XROrigin>();

                if(origin == null)
                {
                    Debug.LogError($"No {nameof(XROrigin)} component found. Cannot setup Immersive XR.");
                    return;
                }
            }

            pivotProperty.objectReferenceValue = origin.transform;

            if(origin.Camera != null)
                cameraProperty.objectReferenceValue = origin.Camera;
            else
                Debug.LogError($"XROrigin camera is null. Ignored for {nameof(LocalPlayerRig)} setup.");

            if(origin.TryGetComponent(out XRInputModalityManager modalityManager))
            {
                SetupController(modalityManager.leftController, InteractorHandedness.Left);
                SetupController(modalityManager.rightController, InteractorHandedness.Right);
                SetupHand(modalityManager.leftHand, Handedness.Left);
                SetupHand(modalityManager.rightHand, Handedness.Right);
            }
            else
                Debug.LogError($"Could not setup controllers/hands components because no modality manager was found.");
            
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetupController(GameObject controller, InteractorHandedness handedness)
        {
            if(!controller.TryGetComponent(out LocalXRControllerReference controllerRef))
            {
                controllerRef = controller.gameObject.AddComponent<LocalXRControllerReference>();
                Debug.Log($"Added new local XR controller reference ({handedness}) in '{controller}'.");
            }

            controllerRef.EditorSetHandedness(handedness);
        }
        
        private static void SetupHand(GameObject hand, Handedness handedness)
        {
            var meshController = hand.GetComponentInChildren<XRHandMeshController>();
            if(meshController == null)
            {
                Debug.LogError($"Could not find {nameof(XRHandMeshController)} under {hand} object. Ignoring '{handedness}' hand setup.");
                return;
            }
            
            if(!meshController.TryGetComponent(out LocalXRHandReference handRef))
            {
                handRef = meshController.gameObject.AddComponent<LocalXRHandReference>();
                Debug.Log($"Added new local XR hand reference ({handedness}) in '{meshController.gameObject}'.");
            }

            handRef.EditorSetHandedness(handedness);
            
            var handRenderer = (SkinnedMeshRenderer)meshController.handMeshRenderer;
            if(handRenderer == null)
            {
                Debug.LogError($"Could not perform hierarchy setup for {hand} object because there is no skinned renderer.");
                return;
            }
            
            var wrist = handRenderer.rootBone;
            string prefix = $"{handedness.ToString().ToUpperInvariant()[0]}_";
            
            try
            {
                handRef.EditorSetHierarchy(wrist, prefix);
                Debug.Log($"Finished setting up hierarchy for hand '{handedness}'.");
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        private static bool IsPrefabAsset(GameObject obj)
        {
            return PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab 
                   && PrefabUtility.IsPartOfPrefabAsset(obj);
        }
    }
}
#endif