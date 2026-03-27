> This branch is not currently in a working state and is definitely not ready for production use. The branch is here to track progress when work on this project resumes.

### 0.1.0 TODO list

Implement:

### Preferences

🔲 FOPreferences (centralized preference management for the FO system)

### Management

🔲 OffsetScene

🔲 OffsetManager

### Helpers

🔲 FloatingOffset

🔲 FOExtensions

### Transforms

🔲 OffsetTransform (Formally FOObject)

🔲 OffsetView (FOObserver)

🔲 OffsetAnchor (Formally FOAnchor)

🔲 IgnoreOffset

### Example

🔲 Default Offsetter

🔲 DefaultFOSceneManager

🔲 TestingSetup.unitypackage

🔲 FishNetIntegration.unitypackage

### Abstractions

✅ IOffsetter

🔲 FOSceneManager

### Testing

🔲 Rewrite tests to work with new API

🔲 Maximize test coverage

---

## OffsetScene

Manages the offset of a particular scene and all OffsetViews and OffsetTransforms within the scene. If an OffsetView escapes the bounds of the scene, it will trigger a rebase. If, after the rebase, any tracked OTs or OVs are out of bounds, they will be queued to move to a queued offsetScene. Each time an OV is queued, it is considered no longer inside the scene, and the rebase of the scene is recalculated based on all remaining OVs in the scene, EXLCLUDING the queued OV's.

If two OS's are within each other's merge area they will merge. merge area = 1/2 rebase area. Merging is handled by the OM.

All OS's have a real velocity and a real position. The real velocity of an OS is always zero, unless ALL OV's in the scene have a velocity significantly greater than zero (this margin should be settable, but should be set to the maximum velocity at which collisions can still be accurately detected)

If an OS has a non-zero root velocity, all root objects in the scene without an OffsetView component (including OT's) will be removed and the dev will have to handle accurate collision detection in that case. Perhaps sweep test helper methods can be provided to this end.

Should not depend on OffsetManager.
Update mode is settable (default Unity, but can also be updated from the OffsetManager)

## GlobalScaleScene

Scene which is scaled so that the imprecise part of float64 = float32. This scene is not offset, and is meant to hold stuff like giant stellar objects. Helper methods are provided for accurate collision detection and proximity detection at stellar scales.

## OffsetManager

When an OffsetScene detects an OV that must be moved to a different OS, it will call a method on the OM that queues the OV for transfer to a different OS. While an OV is marked as "queued" it will  be ignored by the OffsetScene it is currently in, and will be moved to the first pooled OS as soon as it is ready.

OffsetScenes which contain no OV's will report this to the OM, which will pool them for later use (this means moving all their OT's to a null-scene)

OM's essentially manage the pooling of OS's and the transfer of OV's and OT's between all active OS's and the null scene. The null scene can simply be the scene the OM is in. The OM furthermore must be part of a "manager scene" and not part of the game world (since that is managed by OS's)

Default update mode is Unity, can also be Custom. There is also a derived class OffsetManagerFishNet that overrides the setup and updates subscribed to the FishNet OnTick and also handles network synchronisation for clients (default is first View spawned by the client is considered the player, but methods are provided to change the View registered for any given player)

## OffsetTransform

should always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed.

Has a real position. When an OT is moved to the null scene, its real position will be cached. If the OT's real position is in range of any merge area, it will be taken from the nullscene and moved to the local position relative to its real position.

## OffsetView

When leaving the rebase area the OS will be rebased to the centroid of all OV's in the scene.

Tracks the real position (relative to real zero) and the real velocity (in absolute space, relative to real zero velocity) of itself.

### OffsetAnchor

Offset Anchors ensure that the object they are attached to are always at the exact position specified in the OA's target position.

## IgnoreOffset

Marks an object as ignored by the Offset system, when a scene is rebased this object will not be moved.

## OffsetSceneNetworking

See OffsetManagerFishNet

## SceneRegistry

Provides getScened<component>(Scene), and the helper method findFirstCached<component>(Scene), and findFirst<component>(Scene). The OffsetManager and the OffsetScene both depend on this Component for interop.

findFirstCached finds the first component of the given type in the given scene, and caches the index of the GameObject where the component was found so that subsequent calls will be faster. If on subsequent calls the Component is not found, findFirstCached will run the search process again (findFirst) and cache the new index.

## OffsetScenesLoggingConfig

Sets the desired logging configuration for the package. Like FishNet, if no logging config is present it will create a default logging configuration in the project folder.

> Is it possible to subclass classes from UPM packages? That way devs could integrate this solution into their own networking packages/games. It would also be possible to give frequently modified components to devs in the form of a UnityPackage.
