#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using TMPro;

namespace PixelsHub.Netrooms.UX
{
    /// <summary>
    /// Global XRKeboard that is automatically shown and hidden upon TMP_InputField selection.
    /// </summary>
    public class AutoManagedXRKeyboard : GlobalNonNativeKeyboard
    {
        private GameObject lastSelected;

        private TMP_InputField detectedInputField;

#if UNITY_EDITOR
        [SerializeField]
        private bool executeInEditor;
#endif

        private void OnEnable()
        {
            if(!XRImmersiveness.IsActive)
                DestroyImmediate(this);
            
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