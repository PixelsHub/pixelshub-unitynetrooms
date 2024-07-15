[Return to main page](../)

# Player Basics
Players are considered the instance of a user on the running application. In the networking context, a player is spawned for a given user that joins the room.

## Local Behaviours
The local player prefab that oversees the local use of the application must be instantiated from the start of the scene.\
In Cross-Immersive applications, use the provided `CrossImmersiveLocalObjectLifespan` component to instantiate the target local player prefab.\
\
Examples of local functionality include:
- Camera control behaviours
- User movement and/or input control
- Reference transforms for player avatar
- UI/UX setup and management
- Scene objects manipulation

## Networking (Unity Netcode)
The class `NetworkPlayer` is used to control players in the networking context. A prefab with this component must be added as the *Player Prefab* in the *NetworkManager*.\
From each local player's local actions, the required data will be synchronized across the scene.\
\
Static utilities are provided, such as accessing the local NetworkPlayer, and a dictionary collection of spawned players keyed by their OwnerClientId.
```C#
NetworkPlayer localPlayer = NetworkPlayer.Local;

int playersCount = NetworkPlayer.Players.Count;
NetworkPlayer.Players.TryGetValue(someId, out NetworkPlayer somePlayer);
```

## Player Slots
A persistent instance of the *NetworkBehaviour* `NetworkPlayerSlots` is required on the scene to control the available player slots.
These slots provide a way of distributing players in an indexed collection that is synchronized across all connections, while also specifying the maximum amount of allowed players.\
\
Players can therefore be accessed via index and viceversa:
```C#
NetworkPlayer myPlayer = NetworkPlayerSlots.Instance.GetPlayer(2);

int index = NetworkPlayerSlots.Instance.GetPlayerIndex(NetworkPlayer.Local);

List<NetworkPlayer> orderedPlayers = NetworkPlayerSlots.Instance.GetPlayers(); // Empty slots are null
```
> [!NOTE]
> Any player that attempts to connect when there are no available slots (the maximum supported player amount has been reached) will be kicked automatically by the server.\
> Reducing the available slots will also kick any excess players. 

## Player Validation
Spawned players need to be validated in the server before being initialized. Players that fail validation will be kicked immediately.\
To perform validation, each player is checked against any **enabled** instances of `PlayerConnectionRequirement` derived classes. Implementations should create these classes to determine how access is granted to players.\
\
A connection requirement class `PlayerInvitationRequirement` is already provided, which, when enabled on scene, will only allow acess to players whose identifier is contained in a list of invited players.
> [!NOTE]
> Custom connection requirement classes should also contemplate if and how to kick connected players if their internal data for validation criteria changes.
> For example, `PlayerInvitationRequirement` will kick any connected players if they are no longer contained in their *invited players* collection when changed.

## Local Player User Identifier
Players are expected to be uniquely identified through a string (fixed 512 bytes) that is synchronized across each `NetworkPlayer` as the network variable `player.userIdentifier`.\
This variable is automatically set by the local client when spawned based on the value of the internal static class `LocalPlayerUserIdentifier`.\
This value must be set before a network connection on the local client, each implementation determining its behaviour based on functionality requirements, such as:
- Automatically after a successful user login before user connects
- UX-driven naming before user connects

## Player association of components and data
Players can be associated additional data and behaviours through external composition relying on network ownership and the `OwnerClientId` variable, which can be used to obtain the correct player via the `NetworkPlayer.Players` dictionary.\
\
For components that require being added and configured in prefabs, the abstract class `NetworkPlayerComponent` is provided. Derived classes should be added as a behaviour associated with a **NetworkObject** that will be owned by the player, such as the player prefab itself or the player avatar.\
> [!TIP]
> The class `NetworkPlayerBasicUserVariables` provides an example of a player associated component that will synchronize **display name** and **role** strings for the player, expecting its addtion as part of the avatar to show their texts above the player head. 
