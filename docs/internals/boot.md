# Game startup

Entry point: `GamePreInit` (IFSCL.Runtime). Two loading waves:

1. `AfterAssembliesLoaded`: static reference tables (`XxxList.PRE_LOAD()`).
2. `AfterSceneLoad` → `LoadAll()`: content files loaded async, a second wave of `PRE_LOAD`, then `fullyLoaded = true`.

BepInEx hooks `AfterSceneLoad` (see the README, entrypoint section).
