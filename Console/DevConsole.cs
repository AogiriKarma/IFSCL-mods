using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace IfsclMods.Console;

// Quake-style overlay console. Lives on the plugin's GameObject, so a hot reload recreates it.
public class DevConsole : MonoBehaviour
{
    public static ConfigEntry<KeyCode> Key;

    bool open;
    string input = "";
    readonly List<string> lines = new();
    Vector2 scroll;

    public void Print(string text) => lines.Add(text);

    void OnGUI()
    {
        // Toggle on the IMGUI event, and consume it so the text field never sees the key.
        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == Key.Value) { open = !open; e.Use(); return; }
        if (!open) return;
        float h = Screen.height * 0.4f;
        GUI.Box(new Rect(0, 0, Screen.width, h), "");

        // Scrollback: newest line at the bottom, auto-scrolled.
        scroll.y = float.MaxValue;
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(Screen.width), GUILayout.Height(h - 30));
        GUILayout.FlexibleSpace(); // pins the lines to the bottom, right above the input
        foreach (var l in lines) GUILayout.Label(l);
        GUILayout.EndScrollView();

        // Enter submits. Checked before the TextField consumes the event.
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && input.Length > 0)
        {
            Print("> " + input);
            Commands.Execute(input, Print);
            input = "";
            e.Use();
        }

        GUI.SetNextControlName("console_input");
        input = GUI.TextField(new Rect(0, h - 25, Screen.width, 25), input);
        GUI.FocusControl("console_input");
    }
}
