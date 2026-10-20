using System;
using System.Collections.Generic;
using System.Linq;

namespace IfsclMods.Console;

// Command registry shared by all mods: Commands.Register("name", "help", args => ...).
public static class Commands
{
    public delegate void Handler(string[] args, Action<string> print);

    class Entry { public string Name; public string Help; public Handler Run; public Entry(string n, string h, Handler r) { Name = n; Help = h; Run = r; } } // records need IsExternalInit, missing on netstandard2.1
    static readonly Dictionary<string, Entry> all = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, string help, Handler run) => all[name] = new Entry(name, help, run);
    public static void Unregister(string name) => all.Remove(name);

    public static void Execute(string line, Action<string> print)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        if (!all.TryGetValue(parts[0], out var cmd)) { print($"Unknown command '{parts[0]}'. Try 'help'."); return; }
        try { cmd.Run(parts.Skip(1).ToArray(), print); }
        catch (Exception ex) { print($"<color=red>{ex.GetType().Name}: {ex.Message}</color>"); }
    }

    public static IEnumerable<(string name, string help)> List() =>
        all.Values.OrderBy(e => e.Name).Select(e => (e.Name, e.Help));
}
