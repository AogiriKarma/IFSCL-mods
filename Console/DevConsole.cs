using System.Collections.Generic;
using BepInEx.Configuration;
using IfsclMods.Api;
using UnityEngine;

namespace IfsclMods.Console;

// Console controller: key toggle, game freeze, transcript, command history, and the two views
// (the game's terminal window, or a plain IMGUI overlay when Console/TerminalSkin is off).
public class DevConsole : MonoBehaviour
{
    public static ConfigEntry<KeyCode> Key;
    public static ConfigEntry<bool> Skin;

    bool open;
    readonly List<string> blocks = new();   // newest first, one block per command
    int scroll;                             // newest blocks hidden
    readonly List<string> history = new();
    int historyIndex;                       // == history.Count when editing a fresh line
    string imguiInput = "";
    Vector2 imguiScroll;

    void Awake()
    {
        TerminalWindow.OnSubmit = Submit;
        TerminalWindow.OnBuilt = RenderWindow;
        Skin.SettingChanged += (_, _) => { if (open) ApplyView(); };
    }

    public void Print(string block)
    {
        blocks.Insert(0, block);
        if (blocks.Count > 500) blocks.RemoveAt(blocks.Count - 1);
        scroll = 0;
        RenderWindow();
    }

    void RenderWindow() => TerminalWindow.Render(blocks, scroll);

    void ApplyView()
    {
        if (Skin.Value) { TerminalWindow.Show(); RenderWindow(); }
        else TerminalWindow.Hide();
    }

    void SetOpen(bool on)
    {
        open = on;
        if (on) { Freeze.Acquire(); ApplyView(); }
        else { TerminalWindow.Hide(); Freeze.Release(); }
    }

    void OnDisable() { if (open) SetOpen(false); } // hot reload or quit while open: give the game back
    void OnDestroy() => TerminalWindow.DestroyAll();

    // One block per command so a multi-line output reads top-down even with newest-first display.
    void Submit(string line)
    {
        history.Add(line);
        historyIndex = history.Count;
        var block = new List<string> { "<color=white>> " + line + "</color>" };
        Commands.Execute(line, block.Add);
        Print(string.Join("\n", block));
    }

    void Scroll(int delta)
    {
        scroll = Mathf.Clamp(scroll + delta, 0, Mathf.Max(0, blocks.Count - 1));
        RenderWindow();
    }

    void Update()
    {
        if (Input.GetKeyDown(Key.Value) && (Skin.Value || !open)) { SetOpen(!open); return; } // plain view closes from OnGUI
        if (!open) return;

        if (Skin.Value && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) TerminalWindow.SubmitCurrent();

        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0f || Input.GetKeyDown(KeyCode.PageUp)) Scroll(+1);
        if (wheel < 0f || Input.GetKeyDown(KeyCode.PageDown)) Scroll(-1);

        if (Input.GetKeyDown(KeyCode.UpArrow) && historyIndex > 0) SetInput(history[--historyIndex]);
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (historyIndex < history.Count - 1) SetInput(history[++historyIndex]);
            else { historyIndex = history.Count; SetInput(""); }
        }
    }

    void SetInput(string text) { if (Skin.Value) TerminalWindow.SetInput(text); else imguiInput = text; }

    // Plain overlay view: oldest at the top, newest right above the input, like a classic console.
    void OnGUI()
    {
        if (!open || Skin.Value) return;
        var e = Event.current;
        // A focused IMGUI text field swallows keys before Input sees them: close from the event here.
        if (e.type == EventType.KeyDown && e.keyCode == Key.Value) { SetOpen(false); e.Use(); return; }

        float h = Screen.height * 0.4f;
        GUI.Box(new Rect(0, 0, Screen.width, h), "");
        var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };

        imguiScroll.y = float.MaxValue;
        imguiScroll = GUILayout.BeginScrollView(imguiScroll, GUILayout.Width(Screen.width), GUILayout.Height(h - 30));
        GUILayout.FlexibleSpace();
        for (int i = blocks.Count - 1; i >= scroll; i--) GUILayout.Label(blocks[i], style);
        GUILayout.EndScrollView();

        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            if (imguiInput.Length > 0) Submit(imguiInput);
            imguiInput = "";
            e.Use();
        }
        GUI.SetNextControlName("console_input");
        imguiInput = GUI.TextField(new Rect(0, h - 25, Screen.width, 25), imguiInput);
        GUI.FocusControl("console_input");
    }
}
