> This branch is not currently in a working state and is definitely not ready for production use. The branch is here to track progress when work on this project resumes.

### 2.0.0-dev TODO list

Implement:

### Preferences

✅ OffsetUniverse for centralized preferences and state

### Management

🔲 Thin OffsetSceneManager that is decoupled from the internal data representation of OffsetScenes

✅ Thin OffsetManager (only bootstraps OffsetServer and adds reference to OffsetUniverse)

### Helpers

🔲 Helper methods/extension

### Transforms

✅ OffsetTransform (Formally FOObject, now also performs tasks of FOObserver)

🔲 TODO: how to 're-discover' offset transforms after they have been moved to the null scene? hash grid (or: keep a cap on the # of disabled offset transforms and just do an O(n) search on the few left over? could also be sped up with sweep and prune.

✅ Deprecate FOObserver in favor of boolean on OffsetTransform

✅ OffsetAnchor (Formally FOAnchor)

🔲 IgnoreOffset

### Example

🔲 TestingSetup.unitypackage (test harness, example setup)

🔲 FishNetDemo.unitypackage (example FishNet demo)

🔲 OffsetDemo.unitypackage (small singleplayer demo)

### Abstractions

✅ Platform independent lightweight OffsetServer core

### Testing

🔲 Rewrite tests to work with new API

🔲 Maximize test coverage

---

## OffsetScene

Manages the offset of a particular scene and all OffsetViews and OffsetTransforms within the scene. If an OffsetView escapes the bounds of the scene, it will trigger a rebase. If, after the rebase, any tracked OTs or OVs are out of bounds, they will be queued to move to a queued offsetScene. Each time an OffsetView is queued, it is considered no longer inside the scene, and the rebase of the scene is recalculated based on all remaining OVs in the scene, EXLCLUDING the queued OV's.

If two OS's are within each other's merge area they will merge. merge area = 1/2 rebase area. Merging is handled by the OM.

All OS's have a real velocity and a real position. The real velocity of an OffsetScene is always zero, unless ALL OV's in the scene have a velocity significantly greater than zero (this margin should be settable, but should be set to the maximum velocity at which collisions can still be accurately detected)

If an OffsetScene has a non-zero root velocity, all root objects in the scene without an OffsetView component (including OT's) will be removed and the dev will have to handle accurate collision detection in that case. Perhaps sweep test helper methods can be provided to this end.

Should not depend on OffsetManager.
Update mode is settable (default Unity, but can also be updated from Offset Manager the OffsetManager)

## OffsetSceneManager

Implements Unity functions on behalf of the OffsetScene

## GlobalScaleScene

Scene which is scaled so that the imprecise part of float64 = float32. This scene is not offset, and is meant to hold stuff like giant stellar objects. Helper methods are provided for accurate collision detection and proximity detection at stellar scales.

## OffsetServer

When an OffsetScene detects an OffsetView that must be moved to a different OffsetScene, it will add the OffsetView to its transfer queue. The OffsetServer will monitor queues of OffsetScenes. While an OffsetView is marked as "queued" it will  be ignored by the OffsetScene it is currently in, and will be moved to the first pooled OffsetScene as soon as it is ready.

OffsetScenes which contain no OffsetViews will report this to the OffsetManager, which will pool them for later use (this means moving all their OffsetTransforms to a null-scene and keying them on a hash grid or similar data structure for fast retrieval later)

The Offset Server essentially manages the pooling of Offset Scenes and the transfer of Offset Views and Offset Transforms between all active Offset Scenes and the null scene. The null scene can simply be the scene the Offset Manager is in. The Offset Server is not actually a Monobehaviour so it is instantiated within the OffsetManager as a plain C# object.

This architectural decsision was taken to facilitate compatibility with other engines in the future.

## Offset Manager

Bootstraps the OffsetServer.

Default update mode is Unity, can also be Custom. There is also a derived class OffsetManagerFishNet that overrides the setup and updates subscribed to the FishNet OnTick and also handles network synchronisation for clients (default is first View spawned by the client is considered the player, but methods are provided to change the View registered for any given player)

## OffsetTransform

should always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed.

Has a real position. When an OT is moved to the null scene, its real position will be cached. If the OT's real position is in range of any merge area, it will be taken from Offset Manager the nullscene and moved to the local position relative to its real position.

## OffsetView

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
