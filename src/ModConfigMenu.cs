using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using IFSCL;
using IFSCL.UI;
using UnityEngine;
using IfsclMods.Api;

static class ModConfigMenu
{
    public const MenuBaseModes Mode = (MenuBaseModes)1001;
    public static PluginInfo Selected;
    public static ConfigEntryBase PendingKey;
    public static MenuButton PendingLine;

    static IEnumerable<ConfigEntryBase> All => Selected?.Instance?.Config.Select(kv => kv.Value) ?? Enumerable.Empty<ConfigEntryBase>();

    // Numbers with a declared range get a slider (ModConfigSliders); everything else is a text line.
    public static bool IsSlider(ConfigEntryBase e) =>
        (e.SettingType == typeof(int) || e.SettingType == typeof(float)) && Range(e).min.HasValue;

    public static List<ConfigEntryBase> Entries => All.Where(e => !IsSlider(e)).ToList();
    public static List<ConfigEntryBase> SliderEntries => All.Where(IsSlider).ToList();

    // Modders translate labels with Lang.OptionName(key, ...) / Lang.OptionDescr(key, ...); raw key otherwise.
    // ponytail: keys are global across mods, prefix them if two mods ever collide.
    public static string Label(ConfigEntryBase e) =>
        Lang.HasOptionName(e.Definition.Key) ? MSG.GetOptionNames(e.Definition.Key) : e.Definition.Key;
    public static string Descr(ConfigEntryBase e) =>
        Lang.HasOptionDescr(e.Definition.Key) ? MSG.GetOptionDescr(e.Definition.Key) : e.Description.Description;

    public static void Open(MenuBase menu, PluginInfo p)
    {
        Selected = p;
        menu.Initialisation(Mode);
        
    }

    public static string ValueText(ConfigEntryBase e) => e.BoxedValue switch
    {
        bool b => MSG.GetWord(b ? "yes" : "no"),
        float f => f.ToString("0.##"),
        var v => v?.ToString() ?? "",
    };

    public static void Step(ConfigEntryBase e, int dir)
    {
        if (e.SettingType == typeof(bool)) e.BoxedValue = !(bool)e.BoxedValue;
        else if (e.SettingType == typeof(int))
        {
            var (min, max) = Range(e);
            int step = min.HasValue ? Math.Max(1, (int)((max.Value - min.Value) / 20)) : 1;
            int n = (int)e.BoxedValue + dir * step;
            e.BoxedValue = min.HasValue ? (int)Math.Clamp(n, min.Value, max.Value) : n;
        }
        else if (e.SettingType == typeof(float))
        {
            var (min, max) = Range(e);
            float step = min.HasValue ? (float)((max.Value - min.Value) / 20) : 0.1f;
            float n = (float)e.BoxedValue + dir * step;
            e.BoxedValue = min.HasValue ? (float)Math.Clamp(n, min.Value, max.Value) : n;
        }
        else if (e.SettingType == typeof(KeyCode) || e.SettingType == typeof(KeyboardShortcut))
        {
            // Show the game's "press a key" popup for the look; ModConfigMenu_KeyCapture does the listening.
            PendingKey = e;
            PopupManager.DisplayMapping(TypePopup.mapKey, "mod_" + e.Definition.Key);
        }
        else if (e.SettingType.IsEnum)
        {
            var values = Enum.GetValues(e.SettingType);
            int i = Array.IndexOf(values, e.BoxedValue);
            e.BoxedValue = values.GetValue((i + dir + values.Length) % values.Length);
        }
    }

    public static (double? min, double? max) Range(ConfigEntryBase e)
    {
        var r = e.Description.AcceptableValues;
        if (r == null) return (null, null);
        var t = r.GetType();
        return (Convert.ToDouble(t.GetProperty("MinValue")?.GetValue(r)),
                Convert.ToDouble(t.GetProperty("MaxValue")?.GetValue(r)));
    }
}

[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.DoButtonActionPerMenu))]
static class ModConfigMenu_Lines
{
    static void Postfix(MenuButton bl, BtnLineStatus blAction)
    {
        if (MenuBase.currentMenuMode != ModConfigMenu.Mode || bl.enm != BtnLineAction.IdBased || bl is not ButtonLine) return;

        var entries = ModConfigMenu.Entries;
        if (bl.id >= entries.Count) return;
        var e = entries[bl.id];

        ModConfigMenu.PendingLine = bl;
        if (blAction == BtnLineStatus.LeftClick) ModConfigMenu.Step(e, +1);
        if (blAction == BtnLineStatus.RightClick) ModConfigMenu.Step(e, -1);

        bl.SetContent(blAction, ModConfigMenu.Label(e), ModConfigMenu.ValueText(e), translateFirstWord: false);
        if (blAction == BtnLineStatus.Enter)
            MenuBase.GetCurrent().descr.SetDescription(ModConfigMenu.Descr(e), buttonEnabled: true, delete: false, null, alreadyTranslated: true);
    }
}
// Our own key listener for "mod_" popups: any key accepted, written into the pending config entry.
[HarmonyPatch(typeof(Popup), nameof(Popup.Update))]
static class ModConfigMenu_KeyCapture
{
    static bool Prefix(Popup __instance)
    {
        if (__instance.typePopup != TypePopup.mapKey || !__instance.overrideText.StartsWith("mod_") || ModConfigMenu.PendingKey == null) return true;

        if (Input.GetKeyDown(KeyCode.Escape)) { ModConfigMenu.PendingKey = null; __instance.Cancel(); return false; }
        if (!Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return false;

        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(k)) continue;
            var e = ModConfigMenu.PendingKey;
            e.BoxedValue = e.SettingType == typeof(KeyboardShortcut) ? new KeyboardShortcut(k) : (object)k;
            ModConfigMenu.PendingLine?.SetContent(BtnLineStatus.Init, ModConfigMenu.Label(e), ModConfigMenu.ValueText(e), translateFirstWord: false);
            ModConfigMenu.PendingKey = null;
            __instance.Confirm();
            break;
        }
        return false;
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
