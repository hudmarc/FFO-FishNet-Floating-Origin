<img width="1076" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/547675bd-5f47-422e-8555-6a736e8e9082">

> Screenshot from demo project. The faraway boxes are the other FOView's bounds, and the lines surrounding the camera are the local FOView's bounding box.

# Installation
> FishNet is a dependency for this package. Make sure you have the latest version installed first.

Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)

<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">

# Quick Setup

### `Network Manager`

<img width="503" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/4d8e927e-a204-48c7-aa11-1f3cc8479bd5">

> Add the `Floating Origin Condition` to the default observers of the `ObserverManager` in order to hide views in different offset groups from eachother. Note the Condition has not been fully tested as of writing this Readme.

> The current "best practice" is to separate your "game world" from your "manager world". You can use FishNet's `DefaultScene` component in order to automatically load the game world when needed. The idea behind this is to avoid unnecessarily cloning the `NetworkManager` and attached `FOManager`. Both modules should be able to tolerate cloning in any case.

### `FOView`

<img width="503" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/b0d23fa3-3246-4ad7-b8c8-64cfea18cc45">

> Remember to *disable* Teleport on your NetworkTransform, unless you absolutely need to teleport the NT somewhere! Then it's ok to enable it temporarily. Otherwise it will cause rubberbanding!

> Client Authoritative network transforms are untested and not supported. This package is designed for use with server-authoritative movement with (or without) client side prediction.

### `FOObject`
Attach the `FOObject` component to any object you want only a single instance of (Anything with a `NetworkObject` component that can't move far enough to cause a rebase)

If you want the object anchored to a point in 3D space, set the Anchored Position to something other than 0,0,0

> For example unique settlements or trader posts or AI that always stays within a close radius from its spawn.

> FOObjects are just FOView's which don't get updated every time they move.

> It ~~should be~~ is possible (and fully tested) to have scened NetworkObjects as FOObjects.

# FAQ

### Why are my physics interactions not working properly?

Because this package uses multi-scene stacking, you MUST remember to convert all calls to the `Physics` library to instead use the local physics scene. For example `Physics.Raycast` would be `gameObject.scene.GetPhysicsScene().Raycast` or the shortcut provided by this package `gameObject.Physics().Raycast`. Otherwise your physics will not work correctly!

### Why are FOViews on game clients desynchronizing on offset?
You should enable Teleport on your FOView's `NetworkTransform` if this is a problem you are encountering with your game.

### When should I use an `FOView` or an `FOObject`?

For giant, immovable stuff like planets, don't use `FOObject` or `FOView`. This way they will exist in all stacked scenes, so that all players can view them and interact with them.

The general rule is to use an `FOObject` for any NetworkObjects which:

- Don't have a `NetworkTransform` component OR can't move far enough to cause a rebase
- Must be synchronized accross the network

The `FOObject` will ensure only one instance of the `NetworkObject` exists at a time.

FOViews are best used for FOObjects with NetworkTransforms which can move far distances, (i.e. players or AI, )

> Multiple FOViews on one client is supported. Currently, the scene which will be shown to that client is whatever scene the first spawned FOView that the client owns.

## Todo:
### Quality of Life
✅ Add screenshots for manager and NetworkObject setup

🔲 Re-add FOAnchor component. (currently FOObjects expose the same behaviour, but it makes more sense to have the FOAnchor as a separate component, to anchor i.e. huge stuff like planets that shouldn't be an FOObject because they can't exist in only one scene)

🔲 Add a function to set the "main view" for a connection. Might be necessary if you spawned in your AI's before your player.

🔲 Create a demo scene/game

🔲 Create a demo video

🔲 Integrate CI testing on GitHub repo for automatic testing (currently the tests are run manually)

### Code Quality of Life

✅ Added `transform.GetRealPosition()` extension method

✅ Add extension method `gameObject.Physics()` as alias for `gameObject.scene.GetPhysicsScene()`

🔲 Add method to update grid position of `FOObject` component (will not be implemented, instead if you *must* move an FOObject, unregister it, move it, then re-register it)

### Refactoring
✅ Core rewrite

✅ Extraction of helper functions to testable context

### Performance
✅ Optimize Hashgrid search to use lookup table for adjacent squares

## Unit Testing

Runtime:

<img width="284" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/8d7f8b31-61cf-4378-af6e-3fd05639329b">


Editor:

<img width="503" alt="image" src="https://github.com/hudmarc/FFO-FishNet-Floating-Origin/assets/44267994/0252f9fe-9bcb-47cc-b130-c7de6abe9b77">



#### Runtime
✅ Test ensure errors do not accumulate thanks to offsets (see `ErrorAccumulator`)

✅ Test offsetting and offsetting far from origin (see `OffsetTest`)

✅ Test Merging (case: server FOClient merges with client FOClient) (see `MergeUntilFailServer`)

✅ Test Merging (case: client FOClient merges with server FOClient) (see `MergeUntilFailClient`)

✅ Test more than one FOClient per connection (see `MultipleViewsSameClient`)

✅ Test multiple FOClients merging then separating (see `MultipleViewsSameClient`)

✅ Test FOObjects being moved around between groups (An FOObject is placed somewhere in the scene, then the two FOViews take turns moving into range of the object. Should assert the FOObject does not have a change in real position and the FOObject is always present in the same scene as the nearest FOView) (see `FOObjectGroupChange`)

✅ Test wandering agents (tests two clients wandering around, starting at an FOObject, and then meeting again at the FOObject, asserts the FOObject and both clients end up in the same group) (see `FOObjectGroupChange`)

✅ Test stragglers vs group (tests a group of two clients heading in the opposite direction to a straggler client, which should be kicked out of the group the two clients are in) (see `StragglersVsGroup`)

#### Editor
✅ Test HashGrid implementation

✅ Test Core Space Conversion functions

#### Networking

I have observed all of the below working correctly when running other networked tests, but have not written automated tests specifically for these network faliure cases. They are low priority since no bugs have been observered.

🔲 Test client FOClients only, no server FOClient

🔲 Test client joining then leaving then rejoining

🔲 Test hot reloading/ starting a new game without restarting the server

🔲 Stress test FO Observer Network Condition



