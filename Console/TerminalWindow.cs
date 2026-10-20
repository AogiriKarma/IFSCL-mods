using System;
using System.Collections.Generic;
using IFSCL;
using IFSCL.Programs;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using DG.Tweening;

namespace IfsclMods.Console;

// The console UI: the game's own program window (WindowFrame_SC) with the terminal content
// (FTerminalPrefab) inside. Visuals and drag only: Fenetre's logic needs a Pupitre, so we strip
// it and redo the few layout steps of Fenetre.Init by hand.
static class TerminalWindow
{
    const string FrameKey = "WindowFrame_SC";
    const string ContentKey = "Assets/AddressableAssets/FContentPrefab/FTerminalPrefab.prefab";

    static GameObject root;
    static RectTransform frameRect, shadowRect, contentRect;
    static GameObject surbrillanceInterne;
    static float openW, openH;
    static TextMeshProUGUI history, placeholder, systemInfo;

    // Texts that depend on the game language; called again after a 'lang' switch.
    public static void RefreshTexts()
    {
        if (placeholder != null) placeholder.text = MSG.GetWord("enterCommand") + "...";
        if (systemInfo != null)
        {
            var me = BepInEx.Bootstrap.Chainloader.PluginInfos["ch.aogirikarma.ifsclmods"].Metadata;
            systemInfo.text = $"IFSCL MODS v{me.Version} | BEPINEX {typeof(BepInEx.Paths).Assembly.GetName().Version.ToString(3)}\n{MSG.GetOptionNames("mods").ToUpper()}: {BepInEx.Bootstrap.Chainloader.PluginInfos.Count}";
        }
    }
    static bool loading, wantOpen;

    public static TMP_InputField Input { get; private set; }
    public static Action<string> OnSubmit;
    public static Action OnBuilt; // DevConsole re-renders the transcript into the fresh window

    public static bool IsReady => root != null;

    // Debug: log every RectTransform under the console canvas with anchors and sizes.
    public static void Dump(Action<string> print)
    {
        if (root == null) { print("not loaded"); return; }
        foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
        {
            string indent = new string(' ', Depth(rt) * 2);
            var comps = new System.Text.StringBuilder();
            foreach (var c in rt.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                comps.Append(c.GetType().Name);
                if (c is CanvasGroup cg) comps.Append($"(a={cg.alpha:0.##})");
                if (c is Graphic g) comps.Append($"(a={g.color.a:0.##},on={g.enabled})");
                if (c is TMP_Text t) comps.Append($"[margin={t.margin} align={t.alignment} wrap={t.enableWordWrapping} overflow={t.overflowMode} bounds={t.textBounds.size.x:0}]");
                if (c is Behaviour b && !(c is Graphic)) comps.Append($"(on={b.enabled})");
                comps.Append(' ');
            }
            Debug.Log($"[uidump] {indent}{rt.name} active={rt.gameObject.activeSelf} pos={rt.anchoredPosition} rect={rt.rect.size} scale={rt.localScale} | {comps}");
        }
        print("dumped to Player.log");
    }
    static int Depth(Transform t) { int d = 0; while (t.parent != null && t.parent != root.transform) { d++; t = t.parent; } return d; }

    public static void Show() { wantOpen = true; if (root != null) { root.SetActive(true); PlayOpen(); Refocus(); } else Load(); }

    // Fenetre.Ouverturesuite for an SC window: starts folded (title bar only), unfolds in 0.4s with
    // an InOutCirc ease while the inner highlight flickers. Tweens run on unscaled time: the game is frozen.
    static void PlayOpen()
    {
        const float d = 0.4f;
        frameRect.DOKill(); shadowRect.DOKill(); contentRect.DOKill();
        frameRect.sizeDelta = new Vector2(openH, 0f);
        contentRect.localScale = new Vector3(1f, 0f, 1f);
        shadowRect.localScale = new Vector3(1f, 0f, 1f);
        shadowRect.localPosition = new Vector2(7f, -11f);
        frameRect.DOSizeDelta(new Vector2(openW, openH), d).SetEase(Ease.InOutCirc).SetUpdate(true);
        shadowRect.DOSizeDelta(new Vector2(openW + 10f, openH + 40f), d).SetEase(Ease.InOutCirc).SetUpdate(true);
        shadowRect.DOScaleY(1f, d).SetEase(Ease.InOutCirc).SetUpdate(true);
        contentRect.DOScaleY(1f, d).SetEase(Ease.InOutCirc).SetUpdate(true);

        var hl = surbrillanceInterne.GetComponent<Image>();
        surbrillanceInterne.transform.SetAsLastSibling();
        surbrillanceInterne.SetActive(true);
        hl.DOKill();
        hl.color = new Color(hl.color.r, hl.color.g, hl.color.b, Fenetre.openingSuiteAlphaStart);
        hl.DOFade(Fenetre.openingSuiteAlphaMax, Fenetre.openingSuiteYoyoAlphaDuration)
          .SetLoops(Fenetre.openingSuiteYoyoQuantity, LoopType.Yoyo).SetUpdate(true)
          .OnComplete(() => surbrillanceInterne.SetActive(false));
    }
    public static void Hide() { wantOpen = false; if (root != null) PlayClose(); }

    // Fenetre.SuiteFermeture: frame folds back to its title bar in 0.5s (InOutExpo), content squashes,
    // inner highlight fades in; then the window goes away.
    static void PlayClose()
    {
        const float d = 0.5f;
        frameRect.DOKill(); shadowRect.DOKill(); contentRect.DOKill();
        frameRect.DOSizeDelta(new Vector2(openW, 0f), d).SetEase(Ease.InOutExpo).SetUpdate(true);
        shadowRect.DOSizeDelta(new Vector2(openW + 10f, 40f), d).SetEase(Ease.InOutExpo).SetUpdate(true);
        var hl = surbrillanceInterne.GetComponent<Image>();
        hl.DOKill();
        surbrillanceInterne.SetActive(true);
        hl.DOFade(0.5f, 1f).SetUpdate(true);
        contentRect.DOScaleY(0f, d).SetEase(Ease.InOutExpo).SetUpdate(true).OnComplete(() => { if (!wantOpen) root.SetActive(false); });
    }

    // Newest block on top, like FTerminalPrefab.AddHistory. Blocks are owned by DevConsole.
    public static void Render(IReadOnlyList<string> blocks, int scroll)
    {
        if (history == null) return;
        var sb = new System.Text.StringBuilder();
        for (int i = scroll; i < blocks.Count; i++) { if (i > scroll) sb.Append('\n'); sb.Append(blocks[i]); }
        history.text = sb.ToString();
    }

    // TMP_InputField deactivates itself right after onSubmit, so re-activating must wait a frame.
    public static void Refocus() =>
        DOVirtual.DelayedCall(0.02f, () => { if (Input != null) { Input.Select(); Input.ActivateInputField(); } }).SetUpdate(true);

    // Called by DevConsole on Enter: take the line, clear, keep focus.
    public static void SubmitCurrent()
    {
        if (Input == null) return;
        var line = Input.text.Replace("\n", "").Replace("\r", "").Trim();
        Input.text = "";
        if (line.Length > 0) OnSubmit?.Invoke(line);
        Refocus();
    }

    public static void SetInput(string text)
    {
        if (Input == null) return;
        Input.text = text;
        Input.caretPosition = text.Length;
    }

    static void Load()
    {
        if (loading) return;
        loading = true;
        Addressables.LoadAssetAsync<GameObject>(FrameKey).Completed += hf =>
        Addressables.LoadAssetAsync<GameObject>(ContentKey).Completed += hc =>
        {
            loading = false;
            if (hf.Result == null || hc.Result == null) { Debug.LogError("[IFSCL Mods] console prefabs not found"); return; }
            Build(hf.Result, hc.Result);
            OnBuilt?.Invoke();
            if (wantOpen) Show(); else root.SetActive(false);
        };
    }

    // Hot reload gives us fresh statics but the old canvas survives (DontDestroyOnLoad): remove it.
    public static void DestroyAll()
    {
        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c.name == "ModConsoleCanvas" && c.gameObject.scene.IsValid()) UnityEngine.Object.Destroy(c.gameObject);
        root = null; history = null; Input = null;
    }

    // The terminal is laid out in absolute coordinates: widen the content and move/stretch the pieces by hand.
    // DNA block stays left, input and last-commands stretch, history stretches, top info line is already stretch-anchored.
    static void Widen(FTerminalPrefab term, RectTransform content, float extra)
    {
        content.sizeDelta += new Vector2(extra, 0f);
        var cadre = (RectTransform)term.inputField.transform.parent.parent; // inputFieldContainer -> cadre
        cadre.anchoredPosition += new Vector2(extra / 2f, 0f);
        cadre.sizeDelta += new Vector2(extra, 0f);
        foreach (RectTransform child in cadre)
        {
            bool stretches = child.name == "bloc" || child.name == "HL" || child.name == "enterFeedback" || child.name == "derniereCommandes";
            if (stretches) child.sizeDelta += new Vector2(extra, 0f);
            else child.anchoredPosition -= new Vector2(extra / 2f, 0f); // keep left-side pieces at the left edge
        }
        // The separator lines inside 'bloc' are centered: the vertical one goes back left, the horizontal one stretches.
        foreach (RectTransform line in cadre.Find("bloc"))
        {
            if (line.name != "line") continue;
            if (line.rect.width > 10f) line.sizeDelta += new Vector2(extra, 0f); // underline: stretch to the new right edge
            else line.anchoredPosition -= new Vector2(extra / 2f, 0f);          // DNA separator: stay at the left
        }
        var field = (RectTransform)term.inputField.transform;
        field.sizeDelta += new Vector2(extra, 0f);
        field.anchoredPosition += new Vector2(extra / 2f, 0f);
        term.history.rectTransform.sizeDelta += new Vector2(extra, 0f);
        // TMP caches its container size: make it re-measure after the resize.
        term.history.margin = Vector4.zero;
        term.history.SetLayoutDirty();
        term.history.ForceMeshUpdate(true, true);
    }

    static void Build(GameObject framePrefab, GameObject contentPrefab)
    {
        DestroyAll();
        root = new GameObject("ModConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.Object.DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var frameGo = UnityEngine.Object.Instantiate(framePrefab, root.transform);
        var frame = frameGo.GetComponent<Fenetre>();
        var contentGo = UnityEngine.Object.Instantiate(contentPrefab);
        var term = contentGo.GetComponent<FTerminalPrefab>();

        history = term.history;
        Input = term.inputField;
        term.unfocusPanel.SetActive(false);
        placeholder = term.inputFieldPlaceholder;
        systemInfo = term.systemInfoTXT;
        term.energie.SetActive(false); // "remaining energy": meaningless here
        RefreshTexts();
        history.text = "";
        frame.windowTitleTXT.text = "IFSCL Mods";
        frame.remoteIndicator.SetActive(false);

        // Fenetre.Init, by hand: content under the mask, centered pivots, frame width from content height,
        // border image moved on top of the content.
        var content = (RectTransform)contentGo.transform;
        content.SetParent(frame.maskContent, false);
        content.pivot = content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        // The content Animator drives the input block (bool ShowInput, state terminalInputShow) and the
        // history size every frame. Pose it on "input shown" once, then switch it off so our layout sticks.
        foreach (var a in contentGo.GetComponentsInChildren<Animator>(true)) a.updateMode = AnimatorUpdateMode.UnscaledTime;
        var anim = contentGo.GetComponent<Animator>();
        anim.SetBool("ShowInput", true);
        anim.Play("terminalInputShow", 0, 1f);
        anim.Update(0f);
        anim.enabled = false;

        Widen(term, content, 220f);
        openW = content.rect.width; openH = content.rect.height;
        contentRect = content;
        frameRect = (RectTransform)frameGo.transform;
        shadowRect = (RectTransform)frame.shadow.transform;
        surbrillanceInterne = frame.surbrillanceInterne;
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        // SC windows run unmasked (Fenetre does this for osParent == SC).
        frame.maskContent.GetComponent<Image>().enabled = false;
        frame.maskContent.GetComponent<Mask>().enabled = false;
        frame.bordure.transform.SetParent(content, false);
        frame.bordure.transform.SetAsLastSibling();
        frame.shadow.transform.SetAsFirstSibling();
        frame.shadow.SetActive(true);
        // The prefab is sized for the pupitre screen; scale the whole window to ~60% of the reference width.
        frameRect.localScale = Vector3.one * 1.4f;
        var rect = frameRect;

        // DragPanel calls back into Fenetre; replace it with a plain drag on the same object.
        var drag = frame.dragP.gameObject.AddComponent<SimpleDrag>();
        drag.target = rect;

        // Strip program-side logic from both prefabs; they expect a Pupitre and a ProgramsF.
        foreach (var c in frameGo.GetComponentsInChildren<MonoBehaviour>(true))
            if (c is Fenetre || c is DragPanel || c is FContentPrefab || c is KeyboardWindowInteract || c is OverCheck || c is IFSCL.Programs.Helpers.AutoCloseGauge || c is UnityEngine.EventSystems.EventTrigger)
                UnityEngine.Object.Destroy(c);

        history.overflowMode = TextOverflowModes.Truncate; // cut cleanly, no ellipsis: we scroll instead
        Input.characterLimit = 0;
        Input.lineType = TMP_InputField.LineType.SingleLine;
        Input.text = ""; // the prefab ships with leftover editor text; FTerminalPrefab.PostInit clears it too
        Input.onSubmit.RemoveAllListeners();
        Input.onEndEdit.RemoveAllListeners();
        Dump(_ => { });
    }
}

// Minimal window drag: moves the target rect by the pointer delta.
class SimpleDrag : MonoBehaviour, UnityEngine.EventSystems.IDragHandler
{
    public RectTransform target;
    public void OnDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        if (target != null) target.anchoredPosition += e.delta / target.lossyScale.x * target.localScale.x;
    }
}
