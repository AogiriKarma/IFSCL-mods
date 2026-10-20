using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using IFSCL.UI;
using UnityEngine;

[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.Initialisation))]
static class ModsButtonInjector
{
    const string Name = "ModsButton";

    static void Postfix(MenuBase __instance, MenuBaseModes newMainMenuMode)
    {
        if (__instance is not MenuBaseMain main) return;

        var credits = main.listeButtonMainTiles.FirstOrDefault(b => b.enm == BtnLineAction.Credits);
        if (credits == null) return;

        var parent = credits.transform.parent;
        var mods = parent.Find(Name);
        if (mods == null)
        {
            mods = Object.Instantiate(credits.gameObject, parent).transform;
            mods.name = Name;
            mods.SetSiblingIndex(credits.transform.GetSiblingIndex());
            mods.GetComponent<ButtonLine>().SetContent(BtnLineStatus.Init, "mods");
        }
        // Follow the credits button visibility (hidden in sub-menus like Options).
        mods.gameObject.SetActive(credits.gameObject.activeSelf);
    }
}

[HarmonyPatch(typeof(MenuButtonAction), nameof(MenuButtonAction.Call))]
static class ModsButtonAction
{
    static bool Prefix(MenuButton _b, BtnLineStatus blAction, ref MenuBaseModes __result)
    {
        if (_b.name != "ModsButton") return true;

        if (blAction == BtnLineStatus.LeftClick) __result = ModsMenu.Mode;
        else _b.SetContent(blAction, "mods");
        return false;
    }
}
