using System;
using System.Collections.Generic;
using Translations;

namespace IfsclMods.Api;

/// <summary>Adds translations to the game's own tables so MSG.GetWord / GetOptionNames / GetOptionDescr work for mods.</summary>
public static class Lang
{
    public static void Word(string id, string en, string fr = "", string it = "", string es = "", string pl = "", string pt = "")
        => Set(Words.Instance.rowIDs, Words.Instance.Rows, Words.Instance.AddRow, id, en, fr, it, es, pl, pt);

    public static void OptionName(string id, string en, string fr = "", string it = "", string es = "", string pl = "", string pt = "")
        => Set(OptionNames.Instance.rowIDs, OptionNames.Instance.Rows, OptionNames.Instance.AddRow, id, en, fr, it, es, pl, pt);

    public static void OptionDescr(string id, string en, string fr = "", string it = "", string es = "", string pl = "", string pt = "")
        => Set(Translations.OptionDescr.Instance.rowIDs, Translations.OptionDescr.Instance.Rows, Translations.OptionDescr.Instance.AddRow, id, en, fr, it, es, pl, pt);

    public static bool HasOptionName(string id) => OptionNames.Instance.rowIDs.Contains(id);
    public static bool HasOptionDescr(string id) => Translations.OptionDescr.Instance.rowIDs.Contains(id);

    // Replace if the id already exists, so a ScriptEngine reload updates instead of duplicating.
    static void Set(List<string> ids, List<Row> rows, Action<string, string, string, string, string, string, string> addRow,
                    string id, string en, string fr, string it, string es, string pl, string pt)
    {
        int i = ids.IndexOf(id);
        if (i >= 0) { ids.RemoveAt(i); rows.RemoveAt(i); }
        addRow(id, en, fr, it, es, pl, pt);
    }
}
