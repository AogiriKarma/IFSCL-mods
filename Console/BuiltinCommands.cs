using System.Linq;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using IFSCL;
using IFSCL.Save;
using IFSCL.UI;

namespace IfsclMods.Console;

// Commands shipped with the loader.
static class BuiltinCommands
{
    public static void RegisterAll()
    {
        Commands.Register("help", "List commands.", (_, print) =>
        {
            foreach (var (n, h) in Commands.List()) print($"{n}  -  {h}");
        });

        Commands.Register("mods", "List loaded mods.", (_, print) =>
        {
            foreach (var p in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.Name))
                print($"{p.Metadata.Name} v{p.Metadata.Version}  [{p.Metadata.GUID}]");
        });

        Commands.Register("get", "get <guid> [section.key]  -  Show a mod's config.", (args, print) =>
        {
            if (!TryPlugin(args, print, out var cfg)) return;
            foreach (var kv in cfg)
                if (args.Length < 2 || Matches(kv.Key, args[1]))
                    print($"{kv.Key.Section}.{kv.Key.Key} = {kv.Value.BoxedValue}");
        });

        Commands.Register("set", "set <guid> <section.key> <value>  -  Change a config value.", (args, print) =>
        {
            if (args.Length < 3) { print("Usage: set <guid> <section.key> <value>"); return; }
            if (!TryPlugin(args, print, out var cfg)) return;
            var entry = cfg.FirstOrDefault(kv => Matches(kv.Key, args[1])).Value;
            if (entry == null) { print($"No entry '{args[1]}'."); return; }
            entry.SetSerializedValue(string.Join(" ", args.Skip(2))); // BepInEx parses the string for the entry's type
            print($"{args[1]} = {entry.BoxedValue}");
        });

        Commands.Register("lang", "lang <en|fr|it|es|pl|pt>  -  Show or switch the game language.", (args, print) =>
        {
            if (args.Length == 0) { print(VarG.langage); return; }
            int i = Array.IndexOf(VarG.langagesListe, args[0]);
            if (i < 0) { print("Languages: " + string.Join(", ", VarG.langagesListe)); return; }
            // Same steps as SavedSettingsOptions.Load: option index, global code, notify, then refresh the open menu.
            SavedSettingsOptions.langageID.SetValue(i);
            VarG.langage = args[0];
            VarG.OnLangageChanged();
            SaveGameManager.SaveSettingsAndProfile();
            MenuBase.GetCurrent()?.Initialisation(MenuBase.currentMenuMode, simpleReset: true);
            print("Language: " + VarG.langage);
        });
    }

    static bool Matches(ConfigDefinition d, string spec) =>
        string.Equals($"{d.Section}.{d.Key}", spec, System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(d.Key, spec, System.StringComparison.OrdinalIgnoreCase);

    static bool TryPlugin(string[] args, System.Action<string> print, out ConfigFile cfg)
    {
        cfg = null;
        if (args.Length < 1) { print("Usage: <guid> ..."); return false; }
        if (!Chainloader.PluginInfos.TryGetValue(args[0], out var p)) { print($"No mod '{args[0]}'. See 'mods'."); return false; }
        cfg = p.Instance.Config;
        return true;
    }
}
