[Return to main page](../)

# Player Avatar
Connected players have an avatar to reflect their use of the application in real time.\
Each `NetworkPlayer` will spawn the appropiate avatar prefab out of the two available for a player based on its local Immersiveness.

## Networking
The network behaviour `PlayerAvatar` performs server-client synchronization of an avatar. This means the local player's avatar is always spawned but made invisible, allowing local data to be replicated.\
\
Special local components are to be used to act as local transform data references, the main one being `LocalPlayerRig` which provides references to the local player's pivot and camera. 

## Transformations
The fundamental objetive of an avatar is to synchronize transformation data to visualize the player on non-local instances. The Unity Netcode built-in `NetworkTransform` -or derived classes- must be used to specify how to replicate transforms.\
\
Player avatars are automatically moved to children of the world origin on spawn, in order to take into account local modifications to this origin. This relationship means that:
- The player avatar prefab must be a `NetworkObject`, but does not need a `NetworkTransform` itself
- Desired `NetworkTransform` components, such as the one on the avatar's head object, **must** be a direct child of the avatar `NetworkObject` (due to Unity Netcode limitations) 
- These transform components **must** be configured to synchronize position, rotation and scale
- These transform components **must** be configured to synchronize in local space, and scale synchronization is also expected
- The basic transform required for all avatars is the head of the player, whose transform will synchronize with the local player's camera transform

# Immersive Avatar
For players that are locally executing the application as immersive, the `PlayerAvatarImmersive` provides its exclusive functionality, primarily concerning the synchronization of hand or controller data.

### Hand setup
The immersive avatar prefab **must** contain two objects for the left and right hand that:
- **Must** use Unity's **XRHand** package's hand visualizer to display the hand's skinned mesh renderer
- **Must** be children of the head due to relative transformations (local to player camera) used when applying replication
- **Must** have the `PlayerAvatarXRHand` component added and configured
- **Must not** have any `NetworkTransform` attached since hand data is synchronized manually by the `PlayerAvatarImmersive` implementation
- **Must** use the shader `ShaderGraph/AvatarHands` at one of the hand materials so that required color and scaling features can be applied

> [!NOTE]
> Not all hand joints are synchronized, since some joints' impact on hand visualization is minimal.

### Local references setup
The local immersive player should be based on Unity's **UXInteractionToolkit** XR rig (with hands), with the `LocalPlayerRig` configured at its root.\
The component `LocalXRHandReference` should be configured for each hand, specifying the wrist transform and each required joint in the hierarchy.

> [!IMPORTANT]
> The Hand Interaction Visual objects are expected to be used for the addition of the `LocalXRHandReference` component, having Unity's `XRHandMeshController` component and expected to be found at hierarchy paths:
> - *XR Rig* > Camera Offset > Left Hand > **Left Hand Interaction Visual**
> - *XR Rig* > Camera Offset > Right Hand > **Right Hand Interaction Visual**

> [!TIP]
> An editor utility to automatically obtain all joints in the child transform hierarchy for `PlayerAvatarXRHand` and `LocalXRHandReference` is provided by both components.
