# FishNet Floating Origin
 Floating Origin for FishNet. Tested with 2.5.4, 2.5.10 and 2.6.3. Should work with everything inbetween as well.
# Installation
Click "Add package from git URL..." in the UPM and paste in https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git
# How to Use
See the provided example NetworkManager to see what you should set up.
Your manager scene should be separate from your game scene. An easy way to do this is to use the DefaultScene component built into FN and add the manager scene as the offline scene and the game scene as the online scene.
Aside from that make sure your player prefab (or whatever acts as the 'observer' on clients) has an FOObserver component. The FOmanager will then try to keep this object as close as possible to 0,0,0 in its simulation space, reducing and in most cases completely eliminating inaccuracies caused by floating point errors.

### Usage Notes
If you're making a server authoritative game you must change all calls to raycast/spherecast etc from Physics.Raycast to physicsScene.Raycast where physicsScene is the physics scene of the stacked scene you want to work in. If you are making a client authoritarive game you can just use the normal Physics.Raycast method and since clients only simulate their local scenes it should 'just work'.

If you have subscribed to the Time Manager's Physics tick events this will cause them not to fire, so use the physics tick events provided by the FOManager's built-in TimeManager mode.

When setting the Physics Mode of the FOManager keep in mind this will overwrite Physics.AutoSimulation and the TimeManager's setting.

Currently when rebasing remote clients will not resync correctly so you must set your network transforms to Teleport. See https://github.com/FirstGearGames/FishNet/issues/164 for a potential fix.

If you go very far (like Saturn's distance from the Sun far) from the origin this could potentially cause objects near the original origin position to lose accuracy in their positioning. I would recommend having scene objects as children of an Empty at 0,0,0 in order to migigate this effect (since they will use local position relative to their parent when offset, and the accuracy of this is not affected by floating origin rebases since they only affect scene root objects)

## Example Setup

### Player

![image](https://user-images.githubusercontent.com/44267994/204174643-73a6e8f3-87bf-44bf-aec3-24efed2978e2.png)

### Network Manager

![image](https://user-images.githubusercontent.com/44267994/204174657-ce4066c8-3957-4813-a338-186a08349857.png)

### Scene Hierarchy

![image](https://user-images.githubusercontent.com/44267994/204174853-57ff0c56-18ec-4f54-b128-4e7fe91fc74f.png)

Best practice is to separate your game and manager scenes so that you aren't spawning a bunch of useless extra managers each time FNFO creates a new scene when rebasing. However it should work fine with just one scene as long as your NetworkManager is set to "Destoy Newest".
