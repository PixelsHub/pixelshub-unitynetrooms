[Return to main page](../)

# Player Basics
Players are considered the instance of a user on the running application. In the networking context, a player is spawned for a given user that joins the room.\

## Local Behaviours
The local player prefab that oversees the local use of the application is instantiated from the start of the scene.\
Examples of local functionality include:
- Camera control behaviours
- User movement and/or input control
- Reference transforms for player avatar
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
NetworkPlayer player2 = NetworkPlayerSlots.Instance.GetPlayer(2);

int index = NetworkPlayerSlots.Instance.GetPlayerIndex(NetworkPlayer.Local);

List<NetworkPlayer> orderedPlayers = NetworkPlayerSlots.Instance.GetPlayers(); // Empty slots are null
```
> [!NOTE]
> Any player that attempts to connect when there are no available slots (the maximum supported player amount has been reached) will be kicked automatically by the server.\
> Reducing the available slots will also kick any excess players. 

## Player Validation
Spawned players need to be validated in the server before being initialized. Players that fail validation will be kicked immediately.\
To perform validation, each player is checked against any **enabled** instances of `PlayerConnectionRequirement` derived classes. Implementations should create these classes to determine how access is granted to players.\
A requirement class `PlayerInvitationRequirement` is already provided, which, when enabled on scene, will only allow acess to players whose identifier is contained in a list of invited players.
> [!NOTE]
> Custom connection requirement classes should also contemplate if and how to kick connected players if their internal data for validation criteria changes.
