# About the docs

The directory is as follows:

Preferences

- FOPreferences (full of readonly properties used primarily by the OffsetManager and OffsetScenes)

Management

- OffsetScene (Manages functions and state on a per-offset-scene-basis, if your game only has one OffsetView in game at a time feel free to just use this and ignore the OffsetManager)
- OffsetManager (Manages OffsetScenes, necessary to manage the overall scene stack if there is more than one OffsetView in game)

Helpers

- FloatingOffset (instance finder and helper methods)
- FOExtensions (static extension methods)

Transforms

- OffsetTransform
- OffsetView
- OffsetAnchor
- IgnoreOffset

Example

- Default Offsetter

- DefaultFOSceneManager (Encapsulates scene management duties, required by OffsetManager)

- TestingSetup.unitypackage

- FishNetIntegration.unitypackage

Compatibillity with more networking solutions coming "soon".

Abstractions

- IOffsetter
- FOSceneManager