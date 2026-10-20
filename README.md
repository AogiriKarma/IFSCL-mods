# IFSCL Mods

A mod loader and in-game mod menu for [IFSCL](https://www.ifscl.org/), the Code Lyoko fan game by Immudelki. Built on [BepInEx](https://github.com/BepInEx/BepInEx).

It adds a **Mods** button to the main menu. From there you can see every installed mod, read its description, and change its settings with the game's own menu widgets: text lines, sliders, key binding popup. Nothing in the game files is modified.

## Install (players)

1. Install BepInEx 5.4.x (`BepInEx_linux_x64` or `BepInEx_win_x64`) into the IFSCL folder, next to `IFSCL.x86_64` / `IFSCL.exe`. On Linux, edit `run_bepinex.sh` and set `executable_name="IFSCL.x86_64"`.
2. Run the game once so `BepInEx/config/BepInEx.cfg` is created, then quit.
3. In `BepInEx/config/BepInEx.cfg`, section `[Preloader.Entrypoint]`, set:
   ```
   Assembly = IFSCL.Runtime.dll
   Type = GamePreInit
   Method = AfterSceneLoad
   ```
   Without this, BepInEx starts before the first scene exists and Unity destroys its manager object, so plugins lose `Update` and coroutines.
4. Drop `IfsclMods.dll` into `BepInEx/plugins/`.
5. Launch through `run_bepinex.sh` (Linux) or normally (Windows). A **Mods** button appears between Options and Credits.

Mods go into `BepInEx/plugins/` as well.

## Console

Press **F2** (configurable) to open the console, drawn as a genuine IFSCL program window. The game is frozen while it is open. Built-in commands: `help`, `mods`, `get`, `set`, `lang`, `skin on|off` (plain overlay instead of the game window). Mouse wheel scrolls, arrows recall history.

Mods register their own commands:

```csharp
using IfsclMods.Console;
Commands.Register("spawn", "<monster>: spawn a monster on Lyoko.", (args, print) => print("..."));
```

Need to freeze the game for your own overlay? `Freeze.Acquire()` / `Freeze.Release()` (reference counted) does what the game does for its bug-report form.

## Write a mod

A mod is a regular BepInEx plugin. Reference `IfsclMods.dll` to get the attributes below.

```csharp
[BepInPlugin("you.yourmod", "Your Mod", "1.0.0")]
[ModInfo("You", "What the mod does.")]
[ModDescription("fr", "Ce que fait le mod.")]
public class Plugin : BaseUnityPlugin
{
    void Awake()
    {
        Config.Bind("General", "Enabled", true, "Enable the mod.");
        Config.Bind("General", "Speed", 1.5f, new ConfigDescription("Movement speed.", new AcceptableValueRange<float>(0.5f, 3f)));
        Config.Bind("General", "Key", KeyCode.F7, "Toggle key.");
    }
}
```

What the menu does with your `Config.Bind` entries:

| Type | Widget |
|---|---|
| `bool` | yes / no line, click toggles |
| `enum` | line, left / right click cycles |
| `int`, `float` with `AcceptableValueRange` | slider |
| `int`, `float` without range | line, click steps |
| `KeyCode`, `KeyboardShortcut` | "press a key" popup |
| `string` | displayed, read only |

The entry description shows on hover.

**Enabled convention.** A `bool` entry at `General/Enabled` is shown in the mods list and can be toggled there with a right click. React to it yourself:

```csharp
enabled.SettingChanged += (_, _) => { if (enabled.Value) harmony.PatchAll(); else harmony.UnpatchSelf(); };
```

**Translations.** Labels default to the config key. To translate them, add rows to the game's own translation tables (columns: en, fr, it, es, pl, pt):

```csharp
using IfsclMods.Api;
Lang.OptionName("Speed", "Speed", "Vitesse");
Lang.OptionDescr("Speed", "Movement speed.", "Vitesse de déplacement.");
Lang.Word("hello", "hello", "bonjour");   // then MSG.GetWord("hello") anywhere
```

Keys are global across mods, prefix them if you fear collisions.

## Build

```
dotnet build
```

The csproj references the game DLLs through the `Game` property; adjust its path to your install. Build output is copied to `BepInEx/scripts/` for use with [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) hot reload during development.

## A note on the game's author

IFSCL ships Unity Cloud Diagnostics, which uploads every exception to the developer. IFSCL Mods disables that capture while mods are loaded, so modded stack traces do not end up in Immudelki's bug reports. Please keep it that way, and do not redistribute game files.

## License

MIT
