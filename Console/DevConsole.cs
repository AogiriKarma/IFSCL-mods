using System.Collections.Generic;
using BepInEx.Configuration;
using IfsclMods.Api;
using UnityEngine;

namespace IfsclMods.Console;

// Quake-style overlay console. Lives on the plugin's GameObject, so a hot reload recreates it.
public class DevConsole : MonoBehaviour
{
    public static ConfigEntry<KeyCode> Key;

    bool open;
    string input = "";
    readonly List<string> lines = new();
    readonly List<string> history = new();
    int historyIndex; // == history.Count when editing a fresh line
    Vector2 scroll;

    public void Print(string text) => lines.Add(text);

    void SetOpen(bool on)
    {
        open = on;
        if (on) Freeze.Acquire(); else Freeze.Release();
    }

    void OnDisable() { if (open) SetOpen(false); } // hot reload or quit while open: give the game back

    void Submit()
    {
        Print("> " + input);
        history.Add(input);
        historyIndex = history.Count;
        Commands.Execute(input, Print);
        input = "";
    }

    void OnGUI()
    {
        var e = Event.current;
        // Toggle on the IMGUI event, and consume it so the text field never sees the key.
        if (e.type == EventType.KeyDown && e.keyCode == Key.Value) { SetOpen(!open); e.Use(); return; }
        if (!open) return;

        float h = Screen.height * 0.4f;
        GUI.Box(new Rect(0, 0, Screen.width, h), "");

        // Scrollback: newest line at the bottom, auto-scrolled.
        scroll.y = float.MaxValue;
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(Screen.width), GUILayout.Height(h - 30));
        GUILayout.FlexibleSpace(); // pins the lines to the bottom, right above the input
        foreach (var l in lines) GUILayout.Label(l);
        GUILayout.EndScrollView();

        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (input.Length > 0) Submit();
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    if (historyIndex > 0) input = history[--historyIndex];
                    e.Use();
                    break;
                case KeyCode.DownArrow:
                    if (historyIndex < history.Count - 1) input = history[++historyIndex];
                    else { historyIndex = history.Count; input = ""; }
                    e.Use();
                    break;
            }
        }

        GUI.SetNextControlName("console_input");
        input = GUI.TextField(new Rect(0, h - 25, Screen.width, 25), input);
        GUI.FocusControl("console_input");
        // Keep the caret at the end after history navigation.
        if (GUIUtility.keyboardControl != 0 && GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is TextEditor te && te.cursorIndex > input.Length)
            te.cursorIndex = te.selectIndex = input.Length;
    }
}
