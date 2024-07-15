[Return to main page](../)

# Player Permissions
Player Permissions allow to specify boolean checks against string codes in a modular way, each associated with usability capabilities.\
An example of a permission code would be `CAN_MANIPULATE_OBJECTS`, against which any given player would need to check at the necessary UX flow.\
\
Player Permissions can be accessed statically through the `PlayerPermissions` class.

## Structure
Existing permissions are a static register dictionary `Dictionary<string, List<string>>` that associates each permission's code with a list of player values. Players not contained in the list for a given code are thus not given permission.\
A json serialization of the latest permissions `PlayerPermissions.RegisterJson` is automatically kept cached.\
\
The permission register can be read with readonly objects to allow external representation through the static method `PlayerPermissions.GenerateReadonlyList()`, which will return a `ReadonlyPlayerPermissionList`.\
\
Example permissions json:
```
{
  "CAN_MANIPULATE_OBJECTS":
  [
    "playervalue1",
    "playervalue2"
  ],
  "OTHER_PERMISSION":
  [
    ...
  ]
}
```

## Permission setting
Permissions can be set through both a new dictionary or a json containing the dictionary.\
Implementations should determine when to initialize or how to allow permission modifications.
```C#
Dictionary<string, List<string>> newPermissions = new(...);
PlayerPermissions.SetPermissions(newPermissions);

string permissionsJson = ...
PlayerPermissions.ParsePermissions(permissionsJson);
```
> [!WARNING]
> Only the server should modify the Player Permissions. Connected clients must never access any of these two methods unless originally called from the server for synchronization purposes.

## Permission Check
Permissions can be checked in the code for players through the static methods:
```C#
PlayerPermission.Check(permissionCode, playerValue);
PlayerPermission.CheckLocalPlayer(permisionCode);
```
Both methods provide an additional boolean `passCheckIfCodeNotFound` (default = false) that can be used to determine if the permission should pass the check if the code is not found.

> [!NOTE]
> A local component `LocalPlayerPermissionEvent` is also already provided, which will invoke UnityEvents based on the resulting check of its indicated permission code. 

## Player Value
To allow for multiple use cases of permission capabilities, the method in which players are checked against permissions is left to decide by each implementation.\
The Player Permission system uses strings which may be associated to a player value such as:
- The identifier of each player
- Player roles
- Custom player data representing usability conditions, such as a player being the "Moderator" of the room

When checking against a permission, the player value should be provided, and thus each implementation should have control of how this player value is obtained for each player.\
**The Player Permission system already contains a prepared static variable `LocalPlayerValue`, used in the `CheckLocalPlayer()` method, which is expected to be filled by each implementation when the local player spawns.**
> [!WARNING]
> The `LocalPlayerValue` must not be **null** if `CheckLocalPlayer()` method intends to be used. If no identifiable value exists for the local player, leave it as `string.Empty`.

## Networking (Unity Netcode)
A persistent singleton NetworkBehaviour `NetworkPlayerPermissions` is in charge of providing networking requirements to Player Permissions.\
The server replicates PlayerPermission data to all clients through an Rpc method that sends a serialized json string containing the permission dictionary.
> [!NOTE]
> Permission changes can be requested from a client using the method `SetPermissionsServerRpc(string newPermissionsJson)`, which will trigger the static Action `NetworkPlayerPermissions.OnClientPermissionSetRequested` for implementations to handle how the server shall respond to these requests.  

## Setup
1. The NetworkBehaviour `NetworkPlayerPermissions` should be placed on the desired scene.
2. Implementation must determine how to obtain the player value from a player, such as a static method that takes a NetworkPlayer and returns its identifier, or its role.
3. Custom code should set the string `PlayerPermissions.LocalPlayerValue` when the local player spawns.
4. Initialization of permissions on the server should be performed, ideally through the reading of a json that establishes the desired permissions.
5. *If required*, custom permission utility behaviours can be created, imitating the existing `LocalPlayerPermissionEvent`.
6. *If required*, UI editors should be created for runtime modification, using `PlayerPermissions.GenerateReadonlyList()` to display current permissions, and later save editing on the server.
7. *If required*, listen to `NetworkPlayerPermissions.OnClientPermissionSetRequested` to allow for client-driven permission modifications.

