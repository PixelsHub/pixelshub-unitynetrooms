[Return to main page](../)

# UX
## Common utilities
> **BillboardRect**
> Custom billboard implementation that automatically handles immersiveness to make a rect transform look at the camera, based on its world middle point instead of the pivot. 

## Immersive XR Keyboard
Unity's Spatial Keyboard, part of **XRInteractionToolkit** samples, is used to handle keyboard input during immersive use.\
The provided class `AutoManagedXRKeyboard` should be added to the local immersive player with a reference to the spatial keyboard prefab in the package sample. This component will automatically spawn the spatial keyboard when *any* input field is selected.
