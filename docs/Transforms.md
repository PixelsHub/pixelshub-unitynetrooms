[Return to main page](../)

# Transforms
Most transformation synchronization is expected through built-in Unity Netcode components `NetworkTransform` and derived classes, with a core requirement based around an origin transform that will act as the root reference for any other transform-synchronized object.

> [!NOTE]
> Specific cases of transformations with custom implementations are added for some use cases, such as grab interactions or avatar hand synchronization.

> [!TIP]
> The default `NetworkTransform` component is always server authoritative. A simple override `NetworkTransformOwnerAuthority` with owner authority is provided.

### World Origin
A scene **must** contain an object with the network singleton `NetworkWorldOrigin` which will act as the root of all synchronized transforms.
- Any intended transforms to synchronize **must** be set as children of the origin.
- The origin should be set itself **without** any `NetworkTransform` component, since it is expected to be moved locally without replication.
- Objects with network transformation should be configured for local space.

\
For any transforms that might require custom synchronization, remember to use the origin transform reference for relative transformations, while taking into account if they may be or not children of the origin.\
The Immersive Avatar hands provide an example:
```C#
var origin = NetworkWorldOrigin.Transform;

var value = wrist.Value;
value.wristPosition = origin.InverseTransformPoint(localHand.WristPosition);
value.wristRotation = Quaternion.Inverse(origin.rotation) * localHand.WristRotation;
```

> [!NOTE]
> Scale synchronization is supported and expected, since some users might, for example, want to look at a real sized scene as a miniature.
> Remember scaling needs to be set in the fields of each `NetworkTransform`.
