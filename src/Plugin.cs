using BepInEx;
using HarmonyLib;
using IFSCL;
using IFSCL.UI;
using UnityEngine.CrashReportHandler;
using IfsclMods.Api;
using IfsclMods.Console;
using BepInEx.Configuration;
using UnityEngine;

[BepInPlugin("ch.aogirikarma.ifsclmods", "IFSCL Mods", "0.1.0")]
[ModInfo("Karma", "Mod manager")]
[ModDescription("fr", "Gestionnaire de mods")]
[ModDescription("es", "Gestor de mods")]
public class Plugin : BaseUnityPlugin
{
    Harmony harmony;
    void Awake()
    {
        Logger.LogInfo("IFSCL Mods loaded");
        TerminalWindow.DestroyAll(); // hot reload: a window built by the previous assembly may still be on screen
        DevConsole.Key = Config.Bind("Console", "Key", KeyCode.F2, "Open the console.");
        DevConsole.Skin = Config.Bind("Console", "TerminalSkin", true, "Use the game's terminal window for the console. Off = plain overlay, more readable.");
        gameObject.AddComponent<DevConsole>();
        BuiltinCommands.RegisterAll();
        harmony = new Harmony("ch.aogirikarma.ifsclmods");
        harmony.PatchAll();
        Lang.OptionName("mods", "Mods", "Mods", "Mod", "Mods", "Mody", "Mods");
        Lang.OptionName("moddedWith", "Modded with", "Moddé avec", "Moddato con", "Modeado con", "Zmodowane z", "Modificado com");
        CrashReportHandler.enableCaptureExceptions = false;
    }

    // Called by ScriptEngine on reload (F6): remove patches so they do not stack up.
    void OnDestroy() => harmony?.UnpatchSelf();
}
