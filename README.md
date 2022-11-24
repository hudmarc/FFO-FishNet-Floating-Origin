# FishNet Floating Origin
 Floating Origin for FishNet
# Installation
Click "Add package from git URL..." in the UPM and paste in https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git
# Caveats
If you have subscribed to the Time Manager's Physics ticks this will break it, so use the ticks provided by the FOManager's TimeManager mode.

When setting the Physics Mode of the FOManager keep in mind this will overwrite Physics.AutoSimulation and the TimeManager's setting.

Currently when rebasing remote clients will not resync correctly so you must set your network transforms to Teleport. See https://github.com/FirstGearGames/FishNet/issues/164 for a potential fix.