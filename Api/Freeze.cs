using IFSCL;
using IFSCL.UI;
using ImmuTools;

namespace IfsclMods.Api;

/// <summary>
/// Puts the game in "modal overlay" state: time frozen, radial menu muted, keyboard focus dropped,
/// anti-click layer up. Same recipe the game uses for its bug-report form.
/// Reference counted: the game resumes when every caller has released.
/// </summary>
public static class Freeze
{
    static int depth;
    public static bool IsFrozen => depth > 0;

    public static void Acquire()
    {
        if (depth++ > 0) return;
        GameSpeedControl.AdjustSpeedFromCode(0f);
        RadialMenu.DoEnableInputs(_onOff: false);
        ProgramsBridge.killAllKeyboardFocus?.Invoke();
        if (SystemWorldCanvas.instance != null) SystemWorldCanvas.instance.antiClick.SetActive(true);
    }

    public static void Release()
    {
        if (depth == 0 || --depth > 0) return;
        if (SystemWorldCanvas.instance != null) SystemWorldCanvas.instance.antiClick.SetActive(false);
        if (VarG.modePause) return; // the game's own pause owns the clock, leave it alone
        RadialMenu.DoEnableInputs(_onOff: true);
        GameSpeedControl.AdjustSpeedFromCode(1f);
    }
}
