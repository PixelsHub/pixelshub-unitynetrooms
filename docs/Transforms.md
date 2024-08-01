[Return to main page](../)

# Transforms
Most transformation synchronization is expected through built-in Unity Netcode components `NetworkTransform` and derived classes, with a core requirement based around an origin transform that will act as the root reference for any other transform-synchronized object.

> [!NOTE]
> Specific cases of transformations with custom implementations are added for some use cases, such as grab interactions or avatar hand synchronization.

> [!TIP]
> The default `NetworkTransform` component is always server authoritative. A simple override `NetworkTransformOwnerAuthority` with owner authority is provided.

> [!NOTE]
> Scale synchronization is supported and expected, since some users might, for example, want to look at a real sized scene as a miniature.
> Remember scaling needs to be set in the fields of each `NetworkTransform`.

### World Origin
A scene **must** contain an object with the network singleton `NetworkWorldOrigin` which will act as the root of all synchronized transforms.
- Any intended transforms to synchronize **must** be set as children of the origin.
- The origin should be set itself **without** any `NetworkTransform` component, since it is expected to be moved locally without replication.
- Objects with network transformation should be configured for local space.
- Objects that may temporarily not be parented to the origin during custom implementation transformation synchronization should:
  - Lock their origin to ensure correctness while transformations occur through `NetworkWorldOrigin.AddLockTransformationRequest(object requester)` and `NetworkWorldOrigin.RemoveLockTransformationRequest(object requester)`
  - Ensure the transform data used for replication takes into account the relative-to-origin values

The following example is a method used in the networking of **Grab Interactables**, since their parenting is changed while being grabbed:
```
private void ProcessLocalGrabTransformation() 
{
    var relative = NetworkWorldOrigin.WorldToLocal(new(transform.position, transform.rotation, transform.lossyScale));

    if(Vector3.Distance(targetPosition, relative.position) > positionThreshold)
    {
        targetPosition = relative.position;
        ReplicatePositionRpc(targetPosition);
    }

    if(Quaternion.Angle(targetRotation, relative.rotation) > rotationThreshold)
    {
        targetRotation = relative.rotation;
        ReplicateRotationRpc(targetRotation);
    }

    if(Vector3.Distance(targetScale, relative.scale) > scaleThreshold)
    {
        targetScale = relative.scale;
        ReplicateScaleRpc(targetScale);
    }
}
```
Since the interactables will be children of the origin in the replication targets, the actual result of the replication can be interpreted safely as local transforms:
```
[Rpc(SendTo.NotMe)]
private void ReplicatePositionRpc(Vector3 position) 
{
    originPosition = transform.localPosition;
    targetPosition = position;
    positionTimer = interpolationTime;
}
```
