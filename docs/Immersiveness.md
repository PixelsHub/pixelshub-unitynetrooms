[Return to main page](../)

# Immersiveness (XR)
Virtual Reality/Mixed Reality are supported for the *same scene* in the *same project* as a non-immersive-targeted application.\
This support is labelled **Cross-Immersive**.\
\
Cross-immersiveness is provided using Unity's native XR packages and  incorporates the following elements:
- Runtime check of Immersiveness activation.
- Automatic detection of Immersiveness through analysis of active providers in the XR settings (editor and builds)
- Automatic build-time add or removal of the preprocessor directive `IMMERSIVE_XR_BUILD` to allow ignoring Immersive logic and content on builds
- Available logic and components to handle instantiation of Immersive prefabs when the execution is Immersive.

## Immersiveness check
The **static** class `XRImmersiveness` provides the boolean check `IsActive`, which will notify at runtime if the application is running as an Immersive application.\
This class will perform a single automatic analysis to establish the nature of the runtime:
- The analysis is always performed **before the scene is loaded**, using a `RuntimeInitializeOnLoadMethod`
- The `XRGeneralSettings`'s active providers will be searched for (OpenXR) to determine if Immersiveness is desired
- **On builds**, if `IMMERSIVE_XR_BUILD` is not present for the build, the analysis will automatically return **false**
- **On the editor**, Unity's **Multiplayer Playmode Tags** can be used to force the analysis result by using the tags:
  - `Immersive`
  - `NotImmersive`

> [!NOTE]
> `XRGeneralSettingsPerBuildTarget` will be used on the editor, since the editor playmode will rely on the computer's platform to set the `XRGeneralSettings` provider.

## Immersive Avatar 
Components are provided specifically to help setup the player's avatars for Immersive usage display on all clients.
- The component `LocalXRHandReference` allows to track the local player's hand pose data based on Unity's XRHands package, requiring placement on the **local player's Immersive prefab**
- The component `PlayerAvatarXRHand` allows to apply hand pose data tracked from a player's `LocalXRHandReference` onto a skinned mesh to display the avatar hands

> [!TIP]
> The `LocalXRHandReference` is expected on the object holders of `XRHandMeshController` for each hand, which will be **Left Hand Interaction Visual** and **Right Hand Interaction Visual** on the **XR Interaction Toolkit**'s XR Rig prefab with hand, found in *Hands Interaction Demo sample*. 

## Utilities
- The component `XRImmersivenessActiveStateEvents` invokes either of two UnityEvents based on current Immersiveness status upon component enabling.
- The editor component `BuildImmersivenessSymbolDefinition` incorporates preprocess build analysis of XR build settings and will write or delete the preprocessor directive `IMMERSIVE_XR_BUILD` automatically, providing a dialog to the user with the result of the edit to ensure notifying before building.
