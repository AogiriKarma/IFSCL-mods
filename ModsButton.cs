using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using IFSCL;
using IFSCL.UI;
using UnityEngine;
using UnityEngine.UI;

// Two cloned main-menu buttons: "Mods" next to Credits, and a "Modded with" line under "Created by".
[HarmonyPatch(typeof(MenuBase), nameof(MenuBase.Initialisation))]
static class ModsButtonInjector
{
    public const string ModsName = "ModsButton";
    public const string ModdedName = "ModdedByLine";

    static void Postfix(MenuBase __instance)
    {
        if (__instance is not MenuBaseMain main) return;
        Clone(main, BtnLineAction.Credits, ModsName, "mods", before: true);
        Clone(main, BtnLineAction.CreatedBy, ModdedName, "moddedWith", before: false);
    }

    static void Clone(MenuBaseMain main, BtnLineAction source, string name, string word, bool before)
    {
        var src = main.listeButtonMainTiles.FirstOrDefault(b => b.enm == source)
                  ?? Object.FindObjectsOfType<ButtonLine>(true).FirstOrDefault(b => b.enm == source);
        if (src == null) return;

        var parent = src.transform.parent;
        var clone = parent.Find(name);
        if (clone == null)
        {
            clone = Object.Instantiate(src.gameObject, parent).transform;
            clone.name = name;
            clone.SetSiblingIndex(src.transform.GetSiblingIndex() + (before ? 0 : 1));
            // Without a layout group the clone would sit on top of its source: push it one row down.
            if (!before && parent.GetComponent<LayoutGroup>() == null)
            {
                var rt = (RectTransform)clone;
                rt.anchoredPosition = ((RectTransform)src.transform).anchoredPosition + Vector2.down * rt.rect.height;
            }
            clone.GetComponent<ButtonLine>().SetContent(BtnLineStatus.Init, word);
        }
        clone.gameObject.SetActive(src.gameObject.activeSelf);
    }
}

[HarmonyPatch(typeof(MenuButtonAction), nameof(MenuButtonAction.Call))]
static class ModsButtonAction
{
    static bool Prefix(MenuButton _b, BtnLineStatus blAction, ref MenuBaseModes __result)
    {
        switch (_b.name)
        {
            case ModsButtonInjector.ModsName:
                if (blAction == BtnLineStatus.LeftClick) __result = ModsMenu.Mode;
                else _b.SetContent(blAction, "mods");
                return false;

            case ModsButtonInjector.ModdedName:
                if (blAction == BtnLineStatus.LeftClick) MenuBase.OpenDPUrl("https://github.com/AogiriKarma/IFSCL-mods");
                else
                {
                    var me = Chainloader.PluginInfos["ch.aogirikarma.ifsclmods"].Metadata;
                    _b.SetContent(blAction, "moddedWith", $"{me.Name} v{me.Version} ({Chainloader.PluginInfos.Count} mods)");
                }
                return false;
        }
        return true;
    }
}
