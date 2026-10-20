using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using IFSCL;
using IFSCL.UI;

static class ModsMenu
{
    // Enums are ints: a value the game never uses gives us our own screen for free.
    public const MenuBaseModes Mode = (MenuBaseModes)1000;

    public static List<PluginInfo> Plugins =>
        Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.Name).ToList();

    public static string DescriptionOf(PluginInfo p)
    {
        var type = p.Instance?.GetType();
        var info = type?.GetCustomAttribute<ModInfoAttribute>();
        if (info == null) return p.Metadata.GUID;

        var localized = type.GetCustomAttributes<ModDescriptionAttribute>()
            .FirstOrDefault(d => d.Lang == VarG.langage);
        var desc = localized?.Text ?? info.Description;

        return $"{desc}\n<size=70%>{info.Author}</size>";
    }
}

[HarmonyPatch(typeof(MenuBase), "SetTitle")]
static class ModsMenu_Title
{
    static void Postfix(MenuBase __instance)
    {
        if (MenuBase.currentMenuMode == ModsMenu.Mode || MenuBase.currentMenuMode == ModConfigMenu.Mode)
            __instance.title.text = MSG.GetOptionNames("mods").ToUpper();
    }
}

[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.SetUnderTitle))]
static class ModConfigMenu_UnderTitle
{
    static void Postfix(MenuBase __instance)
    {
        if (MenuBase.currentMenuMode == ModConfigMenu.Mode)
            __instance.underTitle.text = ModConfigMenu.Selected.Metadata.Name;
    }
}

[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.Initialisation))]
static class ModsMenu_Cam
{
    static void Postfix(MenuBase __instance, MenuBaseModes newMainMenuMode)
    {
        if (__instance is MenuBaseMain main && (newMainMenuMode == ModsMenu.Mode || newMainMenuMode == ModConfigMenu.Mode))
            main.optionsCam.enabled = true;
    }
}

[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.DoButtonActionPerMenu))]
static class ModsMenu_Lines
{
    static void Postfix(MenuButton bl, BtnLineStatus blAction)
    {
        if (MenuBase.currentMenuMode != ModsMenu.Mode || bl.enm != BtnLineAction.IdBased || bl is not ButtonLine) return;

        var plugins = ModsMenu.Plugins;
        if (bl.id >= plugins.Count) return; // ponytail: pool has ~8 lines, instantiate more when we have more mods
        var p = plugins[bl.id];

        if (blAction == BtnLineStatus.LeftClick) { 
            ModConfigMenu.Open(MenuBase.GetCurrent(), p); 
            MenuBase.GetCurrent().descr.SetDescription();
            return; 
        }

        bl.SetContent(blAction, p.Metadata.Name, "v" + p.Metadata.Version, translateFirstWord: false);
        if (blAction == BtnLineStatus.Enter)
            MenuBase.GetCurrent().descr.SetDescription(ModsMenu.DescriptionOf(p), buttonEnabled: true, delete: false, null, alreadyTranslated: true);
    }
}
