## Todo:
- Syncing to clients does not work properly
- determining whether or not to rebuild an offset group is somewhat iffy at the moment
- Add screenshots for manager and NetworkObject setup
- Fix Network Condition so it doesn't constantly update


# Installation
FishNet is a dependency for this package. Make sure you have the latest version installed first.

Then click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)

<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">


# Known bugs

TODO

# Quick Setup

TODO

# FAQ

### Why is my physics not working properly?

Because this package uses multi-scene stacking, you MUST remember to convert all calls to the Physics library to instead use the local physics scene. For example `Physics.Raycast` would be `gameObject.scene.GetPhysicsScene().Raycast`. Otherwise your physics will not work correctly!

### Question?
Answer
