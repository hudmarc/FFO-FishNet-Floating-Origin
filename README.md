# FishNet Floating Origin
 Floating Origin for FishNet
# Installation
Click "Add package from git URL..." in the UPM and paste in https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git
# How to Use
See the provided example NetworkManager to see what you should set up. Aside from that make sure your player prefab (or whatever acts as the 'observer' on clients) has an FOObserver component. The FOmanager will then try to keep this object as close as possible to 0,0,0 in its simulation space, reducing and in most cases completely eliminating inaccuracies caused by floating point errors.

### Usage Notes
If you're making a server authoritative game you must change all calls to raycast/spherecast etc from Physics.Raycast to physicsScene.Raycast where physicsScene is the physics scene of the stacked scene you want to work in. If you are making a client authoritarive game you can just use the normal Physics.Raycast method and since clients only simulate their local scenes it should 'just work'.

If you have subscribed to the Time Manager's Physics tick events this will cause them not to fire, so use the physics tick events provided by the FOManager's built-in TimeManager mode.

When setting the Physics Mode of the FOManager keep in mind this will overwrite Physics.AutoSimulation and the TimeManager's setting.

Currently when rebasing remote clients will not resync correctly so you must set your network transforms to Teleport. See https://github.com/FirstGearGames/FishNet/issues/164 for a potential fix.

If you go very far (like Saturn's distance from the Sun far) from the origin this could potentially cause objects near the original origin position to lose accuracy in their positioning. I would recommend having scene objects as children of an Empty at 0,0,0 in order to migigate this effect (since they will use local position relative to their parent when offset, and the accuracy of this is not affected by floating origin rebases since they only affect scene root objects)