# About
Core project for cross-platform and cross-immersive networking applications.\
Current implementation uses **Unity Netcode for GameObjects**.

## Documentation
[Immersiveness (XR)](docs/Immersiveness.md)\
[Transformations Basics](docs/Transformations.md)\
[Interactions](docs/Interactions.md)\
[User Experience](docs/UX.md)
### Player
[Player Basics](docs/PlayerBasics.md)\
[Player Avatar](docs/PlayerAvatar.md)\
[Player Permissions](docs/PlayerPermissions.md)
### Built-in features
[HTTP](docs/Http.md)\
[Text Chat](docs/TextChat.md)

## Known issues
> [!IMPORTANT]
> Unity's **Multiplayer Playmode** commonly causes errors when testing in the editor after changes in prefabs or scenes. It requires regular restart of players and possibly deletion of the *Library* folder. Android as target platform seems to not work correctly.

> [!CAUTION]
> Some texts with TextMeshPro seem to generate freezing problems when the player is spawned- This has lead to the current OpenSource font present in the project to not being used.

> [!WARNING]
> Dissonance (Voice Comms) currently causes a significant freeze when initialized on a host.
