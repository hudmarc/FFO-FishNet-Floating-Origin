# Installation
> FishNet is a dependency for this package. Make sure you have the latest version installed first.

Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)

<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">

# Quick Setup

### `Network Manager`

<img width="412" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/5213f8e8-f3a1-4f89-9133-3e066068f03e">

> Add the `Floating Origin Condition` to the default observers of the `ObserverManager` in order to hide clients in different offset groups from eachother. Note the Condition has not been fully tested as of writing this Readme.

> The current "best practice" is to separate your "game world" from your "manager world". You can use FishNet's `DefaultScene` component in order to automatically load the game world when needed. The idea behind this is to avoid cloning the `NetworkManager` and attached `FOManager` but both modules should be able to tolerate cloning.

### `FOClient`

<img width="412" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/e4a396ce-81bc-4450-ad39-c6b1030b7d88">

> Remember to enable Teleport on your NetworkTransform if syncing is broken on game clients!

### `FOObject`
Attach the `FOObject` component to any object you want only a single instance of (Anything with a `NetworkObject` component that can't move far enough to cause a rebase)
> For example unique settlements or trader posts or AI that always stays within a close radius from its spawn.

> FOObjects are just FOClients which don't get updated every time they move.

> It should be possible to have scened NetworkObjects as FOObjects but this has not been thoroughly tested.

# FAQ

### Why is my physics not working properly?

Because this package uses multi-scene stacking, you MUST remember to convert all calls to the `Physics` library to instead use the local physics scene. For example `Physics.Raycast` would be `gameObject.scene.GetPhysicsScene().Raycast`. Otherwise your physics will not work correctly!

### Why are FOClients on game clients desynchronizing on offset?
You should enable Teleport on your FOClient's `NetworkTransform` if this is a problem you are encountering with your game.

### When should I use an `FOClient` or an `FOObject`?
The general rule is to use an `FOObject` for any NetworkObjects which:

- Don't have a `NetworkTransform` component OR can't move far enough to cause a rebase
- Must be synchronized accross the network

The `FOObject` will ensure only one instance of the `NetworkObject` exists at a time.

FOClients are best used for FOObjects with NetworkTransforms which can move far distances, (i.e. players and nothing else)

## Todo:
### Quality of Life
✅ Add screenshots for manager and NetworkObject setup

🔲 Create demo scene/game

🔲 Integrate CI testing on GitHub repo

### Code Quality of Life

✅ Added `transform.GetRealPosition()` extension method

🔲 Add extension method `gameObject.Physics()` as alias for `gameObject.scene.GetPhysicsScene().Raycast`

🔲 Add method to update grid position of `FOObject` component

### Refactoring
✅ Core rewrite

✅ Extraction of helper functions to testable context

### Performance
✅ Optimize Hashgrid search to use lookup table for adjacent squares

🔲 Fix Network Condition so it doesn't constantly update

### Unit Testing
#### Runtime
✅ Test ensure errors do not accumulate thanks to offsets (see `ErrorAccumulator`)

✅ Test offsetting and offsetting far from origin (see `ServersideTesterWithEnumeratorPasses`)

✅ Test Merging (case: server FOClient merges with client FOClient) (see `MergeUntilFailServer`)

✅ Test Merging (case: client FOClient merges with server FOClient) (see `MergeUntilFailClient`)

🔲 Test more than one FOClient per connection

🔲 Test FOObjects being moved around between groups

🔲 Test wandering agents (tests two clients wandering around and then meeting again at a given point)

🔲 Test stragglers vs group (tests a group of two clients heading in the opposite direction to a straggler client, which should be kicked out of the group the two clients are in)

🔲 Test client FOClient only, no server FOClient

🔲 Test host migration

🔲 Test hot reloading/ rejoining without restarting client

🔲 Test hot reloading/ starting a new game without restarting the server

#### Editor
✅ Test HashGrid implementation

✅ Test Core Space Conversion functions
