using System;
using BepInEx.Configuration;
using HarmonyLib;
using IFSCL.Database;
using IFSCL.UI;
using IfsclMods.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sliders for ranged numbers, built with the game's own ButtonSlider (the Audio menu widget).
// The container is a clone of the Audio layout, shown only on a mod config page.
static class ModConfigSliders
{
    static GameObject container;
    static Transform group;

    static void EnsureContainer(MenuBaseMain main)
    {
        if (container != null) return;
        var src = main.audioLayout;
        container = UnityEngine.Object.Instantiate(src.gameObject, src.transform.parent);
        container.name = "ModSliders";
        container.transform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);

        var layout = container.GetComponent<Menu_AudioLayout>();
        group = layout.sliderGroup;
        if (layout.buttonLine50 != null) UnityEngine.Object.Destroy(layout.buttonLine50.gameObject);
        UnityEngine.Object.Destroy(layout); // its Awake would spawn the four audio sliders
    }

    static void Rebuild()
    {
        for (int i = group.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(group.GetChild(i).gameObject);
        foreach (var e in ModConfigMenu.SliderEntries) Create(e);
    }

    static void Create(ConfigEntryBase e)
    {
        var (min, max) = ModConfigMenu.Range(e);
        double lo = min.Value, hi = max.Value;
        int pct = (int)Math.Round((Convert.ToDouble(e.BoxedValue) - lo) / (hi - lo) * 100);

        // ButtonSlider wants a 0-100 LevelableOption as model; 'mission' type registers nowhere, so no save side effect.
        var model = new LevelableOption(LevelableType.mission, e.Definition.Key, 100, pct);
        var bs = ButtonSlider.CreateNew(model, e.Definition.Key, group);

        if (!Lang.HasOptionName(e.Definition.Key))
        {
            bs.titleTXT.content = ""; // stop TranslateText from overwriting on OnEnable
            bs.titleTXT.GetComponent<TMP_Text>().text = e.Definition.Key;
        }
        bs.slider.onValueChanged.AddListener(v =>
        {
            double nv = lo + (hi - lo) * v;
            e.BoxedValue = e.SettingType == typeof(int) ? (object)(int)Math.Round(nv) : (float)nv;
            bs.percentageTXT.text = ModConfigMenu.ValueText(e);
        });
        bs.percentageTXT.text = ModConfigMenu.ValueText(e);
    }

    // The Audio layout sits right under the lines block top; push ours down by one pitch per visible line.
    static void Offset(MenuBaseMain main)
    {
        var line = main.listButtonLines.Find(b => b != null)?.GetComponent<RectTransform>();
        var vlg = main.layoutLines.GetComponent<VerticalLayoutGroup>();
        float pitch = (line != null ? line.rect.height : 40f) + (vlg != null ? vlg.spacing : 0f);
        int extra = ModConfigMenu.Entries.Count;
        var rt = container.GetComponent<RectTransform>();
        rt.anchoredPosition = main.audioLayout.GetComponent<RectTransform>().anchoredPosition + Vector2.down * (extra * pitch);
    }

    [HarmonyPatch(typeof(MenuBase), nameof(MenuBase.Initialisation))]
    static class Show
    {
        static void Postfix(MenuBase __instance, MenuBaseModes newMainMenuMode)
        {
            if (__instance is not MenuBaseMain main) return;
            EnsureContainer(main);
            bool on = newMainMenuMode == ModConfigMenu.Mode;
            container.SetActive(on); // activate first: ButtonSlider.OnEnable rewrites the % text, we must come after
            if (!on) return;
            Rebuild();
            Offset(main);
        }
    }
}
