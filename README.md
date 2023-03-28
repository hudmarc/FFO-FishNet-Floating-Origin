# Important Usage Notes
Because this package uses multi-scene stacking, you MUST remember to convert all calls to the Physics library to instead use the local physics scene. For example `Physics.Raycast` would be `gameObject.scene.GetPhysicsScene().Raycast`. Otherwise your physics will not work correctly!

## Changes in 0.1.0
Added FOObject base class, all objects with an FOObject component on them will respect the Floating Origin system. If you want something to persist between rebases, add an FOObject component to it. FOObjects are *NOT* recalculated after creating, if your object can move around (i.e. an AI) and isn't artificially limited to stay within a certain area you should add an FOObserver to it. All players should also have FOObservers added to them. There is also an FOAnchor component to "anchor" objects at a certain Vector3d. *The FOANCHOR does not move objects between Origin Groups, so it should only be used for static map objects.* This should be useful for very faraway static objects that could otherwise suffer from precision loss. Unfortunately the Unity editor cannot handle these scales, so you will have to type in the coordinates manually (a custom Editor for the FOAnchor is provided)

## Known issues
The Floating Origin Observer Condition is currently broken and does not work as expected. A fix is forthcoming.

The example assets are outdated at the present time. I'm in the process of setting up automated testing to speed up development, and as a result of this the quality of the example assets should improve.

# FishNet Floating Origin
Floating Origin for FishNet. Tested with FN version `3.4` Should work with all `3.x` versions and also `2.x`

# Installation
FishNet is a dependency for this package. Make sure you have the latest version installed first.

Then click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)

<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">


# Will my game world fit?
Yes, as long as your game is smaller than the Milky Way. `[52,850 LY / 5*10^20m]`

This package allows for 1 meter worst-case absolute resolution at `[9 * 10 ^ 15]` meters.

Regardless of how imprecise the absolute position of something is it can be interpolated or interacted with in Unity space with no precision loss, since the local positions of objects are always a Vector3 relative to the nearby origin of the space. (hence the "floating origin")

This scale comfortably fits the real life Solar System `[~6*10^8 meters]` with planets within 1m of their intended positions. (ignoring the rendering and physics complexities of huge stellar objects, since this package currently only deals with massive positions, not scales or velocities). It should also be able to hold the Milky Way galaxy with a worst-case absolute resolution of 65 meters.

At massive scales >1cm resolution means a planet orbiting could "jump" many meters into a spaceship landed on its surface (if this planet was a few times farther out than Pluto). to combat this I plan on adding a "reference frame" component so that the relative motion of these massive interstellar bodies would carry over to any observers on their surface. In the meantime you could either not simulate the motion of planets, or create a script which offsets all observers near the planet along with the planet itself every few timesteps in order to match what the "real" position of the planet should be.

If you're writing a proper interstellar-scale game, do write your own Offsetter. You could probably get away with one that only offsets observers and then use FOAnchors for everything else, since they handle their own offsetting.

# How to Use
See the provided example NetworkManager to see what you should set up.
Your manager scene should be separate from your game scene. An easy way to do this is to use the DefaultScene component built into FN and add the manager scene as the offline scene and the game scene as the online scene.
Aside from that make sure your player prefab (or whatever acts as the 'observer' on clients) has an FOObserver component. The FOmanager will then try to keep this object as close as possible to 0,0,0 in its simulation space, reducing and in most cases completely eliminating inaccuracies caused by floating point errors.

# What component should I use?

<img width="908" alt="image" src="https://user-images.githubusercontent.com/44267994/227970953-d32c4950-de47-41aa-838c-6f9ba4149aab.png">

# How does it work?

![FNFO](https://user-images.githubusercontent.com/44267994/227974553-815db54e-71b8-42ff-8b07-9efb9a47b9af.png)


### Usage Notes
If you're making a server authoritative game you must change all calls to raycast/spherecast etc on the server from Physics.Raycast to physicsScene.Raycast where physicsScene is the physics scene of the stacked scene you want to work in. If you are making a client authoritarive game you can just use the normal Physics.Raycast method since clients only simulate their local scenes, so it should 'just work'. (A caveat is that you'd still need to use the scene-specific raycasts for the host client, since all stacked scenes are simulated on the server/host)

If you have subscribed to the Time Manager's Physics tick events this will cause them not to fire, so use the physics tick events provided by the FOManager's built-in TimeManager mode.

When setting the Physics Mode of the FOManager keep in mind this will overwrite Physics.AutoSimulation and the TimeManager's setting. It should still be doing stuff exclusively in OnTick as it would with the TimeManager enabled though.

Currently when rebasing remote clients will not resync correctly so you must set your network transforms to Teleport. The fix will hopefully soon be implemented in FN https://github.com/FirstGearGames/FishNet/discussions/265 so I will then re-enable the code. In the meantime, just set your NT's to "teleport" to avoid rubberbanding.

If you go very far (like Saturn's distance from the Sun far) from the origin this could potentially cause objects near the original origin position to lose accuracy in their positioning. I would recommend having scene objects as children of an FOAnchor in order to mitigate this effect.

## Example Setup

A general rule of thumb: Any object that has a NetworkObject on it (ESPECIALLY if it is a moving/dynamic object with a NetworkTransform or CSP) should have an FOObserver component on it to ensure it always either stays loaded or moves with other FOObservers as needed.

**AN OBJECT THAT DOESN'T HAVE AN FOOBSERVER OR FOOBJECT ON IT IT CAN BE DESTROYED AT ANY TIME WITHOUT WARNING**

Furthermore, objects without FOObservers that are in a scene will be duplicated each time the scene is newly loaded!

### Player

![image](https://user-images.githubusercontent.com/44267994/204174643-73a6e8f3-87bf-44bf-aec3-24efed2978e2.png)

You can also add an FODebugger component to your Observer prefab in order to debug where the floating origin is relative to the observer, to see the Observer Group the observer is in and the relative offset of that group.

### AI/NPC's

It should be possible to just add an FOObserver component to whatever AI you have that needs to travel far enough distances for floating origins to matter. Otherwise you can just have your AI's stay near their house/home base and despawn once the player is far away enough.

### Map objects/other stuff

If something isn't a static, unchanging part of the terrain you should add an FOObserver component to it. This will ensure that it is always in the correct scene and remains interactive.

Anything smaller than 4096 * 4096 meters (or whatever the `chunkSize` constant in the FOManager is set to) should work just fine with just one FOObserver component on it. Anything larger should probably be broken up into multiple sections with an FOObserver on each section.

Things like small procedurally generated settlements should probably have an FOObserver placed on their root node so that the entire settlement syncs correctly and persists when players leave. (your mileage may vary though) Potentially you could have a settlement that generates dynamically, then despawns when the player goes out of range. Then when another player is in range, it could respawn procedurally (again)

Essentially, if you want one instance and one instance only of a thing to exist at any particular time (like e.g. a persistent trading post/waypoint) you should have an FOObject component on it. I'm planning on adding something like a "do not destroy" component which would move objects with that component into stasis in a persistent scene instead of destroying them.

### Network Manager

![image](https://user-images.githubusercontent.com/44267994/204174657-ce4066c8-3957-4813-a338-186a08349857.png)

Note that you should not assign anything to the 'Local Observer' slot in the Floating Origin Manager, as FOObservers automatically register with the FOManager on network start.

### Scene Hierarchy

![image](https://user-images.githubusercontent.com/44267994/204174853-57ff0c56-18ec-4f54-b128-4e7fe91fc74f.png)

The best practice is to separate your game and manager scenes. This way you won't be spawning a bunch of useless extra managers each time FNFO creates a new scene when rebasing. However it should work fine with just one scene as long as your NetworkManager is set to "Destoy Newest".
