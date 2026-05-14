# Quickstart
- [Install FishNet](https://assetstore.unity.com/packages/tools/network/fishnet-networking-evolved-207815)
- Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)
<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">

## To create a basic scene
- Create an Offline Scene, this should have your FishNet `NetworkManager`, add the `OffsetManagerNetworking` component and untick Unity Physics if you want to use TimeManager physics.
- Add an OffsetTransform to your player, and tick 'isView'
- If you have static structures that can be duplicated, anchor them in real space using the `OffsetAnchor`
- If your game uses line renderers or particle effects and you want them to be offset correctly add the `ffectOffsetter` to your manager object and assign it to the `OffsetManager`
- To configure the Floating Offset backend, set your preferences to the `OffsetUniverse`. It should be automatically created at the root of your project. If not, you can create your own under  `Assets/Create/Floating Offset/OffsetUniverse`
- To teleport views to specific real positions, use `universe.TeleportTo(OffsetTransform offsetTransform, Vector3d position)`

<img width="747" height="771" alt="image" src="https://github.com/user-attachments/assets/e1c23c88-0a73-4a33-9ce6-fd838c6d8fc3" />

<img width="309" height="223" alt="Screenshot 2026-05-10 at 21 00 31" src="https://github.com/user-attachments/assets/00ba01c7-183e-4c16-b5af-229625b91048" />

---

## OffsetScene (class)

Manages the offset of a particular scene and all OffsetViews and OffsetTransforms within the scene. If an OffsetView escapes the bounds of the scene, it will trigger a rebase. If, after the rebase, any tracked OTs or OVs are out of bounds, they will be queued to move to a queued offsetScene. Each time an OffsetView is queued, it is considered no longer inside the scene, and the rebase of the scene is recalculated based on all remaining OVs in the scene, EXLCLUDING the queued OV's.

If two OS's are within each other's merge area they will merge. merge area = 1/2 rebase area. Merging is handled by the OM.

All OS's have a real velocity and a real position. The real velocity of an OffsetScene is always zero, unless ALL OV's in the scene have a velocity significantly greater than zero (this margin should be settable, but should be set to the maximum velocity at which collisions can still be accurately detected)

If an OffsetScene has a non-zero root velocity, all root objects in the scene without an OffsetView component (including OT's) will be removed and the dev will have to handle accurate collision detection in that case. Perhaps sweep test helper methods can be provided to this end.

Should not depend on OffsetManager.
Update mode is settable (default Unity, but can also be updated from Offset Manager the OffsetManager)

## OffsetManager

Implements Unity functions on behalf of the OffsetServer.

Bootstraps the OffsetServer.

Default update mode is Unity, can also be Custom. There is also a derived class OffsetManagerFishNet that overrides the setup and updates subscribed to the FishNet OnTick and also handles network synchronisation for clients (default is first View spawned by the client is considered the player, but methods are provided to change the View registered for any given player)

See also `OffsetManagerNetworking`

## OffsetServer

When an OffsetScene detects an OffsetView that must be moved to a different OffsetScene, it will add the OffsetView to its transfer queue. The OffsetServer will monitor queues of OffsetScenes. While an OffsetView is marked as "queued" it will  be ignored by the OffsetScene it is currently in, and will be moved to the first pooled OffsetScene as soon as it is ready.

OffsetScenes which contain no OffsetViews will report this to the OffsetManager, which will pool them for later use (this means moving all their OffsetTransforms to a null-scene and keying them on a hash grid or similar data structure for fast retrieval later)

The Offset Server essentially manages the pooling of Offset Scenes and the transfer of Offset Views and Offset Transforms between all active Offset Scenes and the null scene. The null scene can simply be the scene the Offset Manager is in. The Offset Server is not actually a Monobehaviour so it is instantiated within the OffsetManager as a plain C# object.

This architectural decsision was taken to facilitate compatibility with other engines in the future.



## OffsetTransform

should always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed.

Has a real position. When an OT is moved to the null scene, its real position will be cached. If the OT's real position is in range of any merge area, it will be taken from Offset Manager the nullscene and moved to the local position relative to its real position.

### isView

When leaving the rebase area the OffsetScene will be rebased to the centroid of all OV's in the scene.

Tracks the real position (relative to real zero) and the real velocity (in absolute space, relative to real zero velocity) of itself.

### OffsetAnchor

Offset Anchors ensure that the object they are attached to are always at the exact position specified in the OA's target position.

## IgnoreOffset

Marks an object as ignored by the Offset system, when a scene is rebased this object will not be moved.

## OffsetSceneNetworking

See OffsetManagerFishNet

## OffsetScenesLoggingConfig

Sets the desired logging configuration for the package. Like FishNet, if no logging config is present it will create a default logging configuration in the project folder.

> Is it possible to subclass classes from Offset Manager UPM packages? That way devs could integrate this solution into their own networking packages/games. It would also be possible to give frequently modified components to devs in the form of a UnityPackage.

### 2.0.0-dev TODO list

### Networking

🔲 Test offset syncing to clients

🔲 ~Implement and test OffsetCondition for hiding objects not in your offset scene~ Default FN SceneCondition seems to work.

### Preferences

✅ OffsetUniverse for centralized preferences and state

### Management

✅ ~~Thin OffsetSceneHandler that is decoupled from the internal data representation of OffsetScenes~~ It was so thin that functionality was moved into OffsetManager, `OffsetSceneBootstrapper` only handles registration of scenes now.

✅ Thin OffsetManager (only bootstraps OffsetServer and adds reference to OffsetUniverse)

### Helpers

🔲 Helper methods/extension

### Transforms

✅ OffsetTransform (Formally FOObject, now also performs tasks of FOObserver)

🔲 ~~TODO: how to 're-discover' offset transforms after they have been moved out of bounds? hash grid? (or: keep a cap on the # of disabled offset transforms and just do an O(n) search on the few left over? could also be sped up with sweep and prune)~~ Changed: Offset Transforms are now destroyed if out of range. If you are being pursued by NPC's they will be offset with you, but if they are out of range they will be despawned. If you need to support Offset Transform persistence the best solution would probably be building your own spawning/persistence system with OffsetAnchors.

✅ Deprecate FOObserver in favor of boolean on OffsetTransform

✅ OffsetAnchor (Formally FOAnchor)

🔲 IgnoreOffset

### Example

🔲 TestingSetup.unitypackage (test harness, example setup)

✅ FishNetDemo.unitypackage (example FishNet demo)

🔲 OffsetDemo.unitypackage (small singleplayer demo)

### Abstractions

✅ Platform independent lightweight OffsetServer core

### Testing

✅ Rewrite tests to work with new API

🔲 Maximize test coverage

🔲 Review tests and make sure they are actually testing what they say they are

### Code Quality

✅ Make sure access modifiers are as restrictive as possible

✅ Remove var keyword where unnecessary

✅ Documentation

✅ Lint everything
