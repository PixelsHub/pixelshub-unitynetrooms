#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using TMPro;
using PixelsHub.XR;

namespace PixelsHub.Netrooms.UX
{
    /// <summary>
    /// Global XR Keyboard that is automatically shown and hidden upon TMP_InputField selection.
    /// </summary>
    public class XRKeyboardDrivenByInputFieldSelection : GlobalNonNativeKeyboard
    {
        private GameObject lastSelected;

        private TMP_InputField detectedInputField;

#if UNITY_EDITOR
        [SerializeField]
        private bool executeInEditor;
#endif

        private void Start()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            Debug.Assert(origin != null);
            
            if(playerRoot != origin.transform)
                playerRoot = origin.transform;

            if(cameraTransform != origin.Camera.transform)
                cameraTransform = origin.Camera.transform;
        }

        private void OnEnable()
        {
            if(keyboard == null)
                Debug.LogError($"Missing keyboard prefab. Expecting `XRI Spatial Keyboard` from samples.");
            
            if(!Immersiveness.IsActive)
            {
                Debug.LogError($"{nameof(XRKeyboardDrivenByInputFieldSelection)} has been enabled without immersiveness.");
                DestroyImmediate(this);
                return;
            }
            
            keyboard.onTextUpdated.AddListener(OnTextUpdate);
            keyboard.onClosed.AddListener(OnKeyboardClosed);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if(!executeInEditor)
                return;
#endif
            var system = EventSystem.current;

            if(system == null)
                return;

            if(system.currentSelectedGameObject == lastSelected)
                return;

            var selectedObject = system.currentSelectedGameObject;

            if(selectedObject != null)
            {
                if(selectedObject.TryGetComponent(out detectedInputField))
                {
                    detectedInputField.onFocusSelectAll = false;
                    detectedInputField.resetOnDeActivation = false;
                    detectedInputField.shouldHideSoftKeyboard = true;

                    ShowKeyboard(detectedInputField);

                    detectedInputField.caretPosition = keyboard.caretPosition;

                    RepositionKeyboardIfOutOfView();
                }
                else
                { 
                    HideKeyboard();
                }
            }

            lastSelected = selectedObject;
        }

        private void OnTextUpdate(KeyboardTextEventArgs args)
        {
            UpdateText(args.keyboardText);
        }

        private void UpdateText(string text)
        {
            detectedInputField.text = text;
            detectedInputField.caretPosition = keyboard.caretPosition;
        }

        private void OnKeyboardClosed(KeyboardTextEventArgs _)
        {
            detectedInputField.ReleaseSelection();
            detectedInputField = null;
        }
    }
}
#endif