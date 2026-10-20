using BepInEx;
using HarmonyLib;
using IFSCL;
using IFSCL.UI;
using UnityEngine.CrashReportHandler;
using IfsclMods.Api;
using BepInEx.Configuration;
using UnityEngine;

[BepInPlugin("ch.aogirikarma.ifsclmods", "IFSCL Mods", "0.1.0")]
[ModInfo("Karma", "Mod manager")]
[ModDescription("fr", "Gestionnaire de mods")]
[ModDescription("es", "Gestor de mods")]
public class Plugin : BaseUnityPlugin
{
    Harmony harmony;

    public enum Theme { Classic, Dark, Lyoko }
    void Awake()
    {
        Logger.LogInfo("IFSCL Mods loaded");
        Config.Bind("General", "Theme", Theme.Classic, "Shell color theme.");
        Config.Bind("Debug", "Volume", 50, new ConfigDescription("Test int.", new AcceptableValueRange<int>(0, 100)));
        Config.Bind("Debug", "TestKey", KeyCode.K, "Test key capture.");
        Config.Bind("Debug", "Speed", 1.5f, new ConfigDescription("Test float slider.", new AcceptableValueRange<float>(0.5f, 3f)));
        Config.Bind("Debug", "Name", "Karma", "Test string (display only).");
        harmony = new Harmony("ch.aogirikarma.ifsclmods");
        harmony.PatchAll();
        Lang.OptionName("mods", "Mods", "Mods", "Mod", "Mods", "Mody", "Mods");
        Lang.OptionName("TestKey", "Test key", "Touche de test");
        Lang.OptionDescr("TestKey", "Press to rebind.", "Cliquer pour réassigner.");
        Lang.Word("modded", "modded", "moddé", "moddato", "modeado", "zmodowany", "modificado");
        CrashReportHandler.enableCaptureExceptions = false;
    }

    // Called by ScriptEngine on reload (F6): remove patches so they do not stack up.
    void OnDestroy() => harmony?.UnpatchSelf();

    [HarmonyPatch(typeof(MenuBase), "SetTitle")]
    class TitlePatch
    {
        static void Postfix(MenuBase __instance)
        {
            if (MenuBase.currentMenuMode == MenuBaseModes.main)
            {
                __instance.title.text += " " + MSG.GetWord("modded");
                __instance.title.enableAutoSizing = true;
                __instance.title.fontSizeMin = 20;
                __instance.title.fontSizeMax = __instance.title.fontSize;
                __instance.title.enableWordWrapping = false;
            }
        }
    }

    // Back button on a config page returns to the mods list.
    [HarmonyPatch(typeof(MenuBase), nameof(MenuBase.DoButtonActionPerMenu))]
    static class ModConfigMenu_Back
    {
        static bool Prefix(MenuBase __instance, MenuButton bl, BtnLineStatus blAction)
        {
            if (MenuBase.currentMenuMode != ModConfigMenu.Mode || bl.enm != BtnLineAction.Back || blAction != BtnLineStatus.LeftClick) return true;
            __instance.Initialisation(ModsMenu.Mode);
            return false;
        }
    }
}
