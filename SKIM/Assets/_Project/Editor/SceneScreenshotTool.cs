using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Headless visual QA: opens Boot, enters play mode, lets the Game scene
// load additively, then renders the main camera to a PNG and quits.
// Run with: Unity.exe -batchmode -projectPath <path> -executeMethod SceneScreenshotTool.CaptureBootFlow -logFile <log>
//
// Domain reload on Play would wipe a static frame counter and unsubscribe our
// EditorApplication.update callback, so state lives in EditorPrefs and domain
// reload is disabled for the duration of the capture.
public static class SceneScreenshotTool
{
    const string FrameKey = "SKIM_Screenshot_Frame";
    const string OutDirKey = "SKIM_Screenshot_OutDir";
    const string LaunchFrameKey = "SKIM_Screenshot_LaunchFrame";
    const string SunkFrameKey = "SKIM_Screenshot_SunkFrame";
    const int CaptureAtFrame = 180; // ~3s at 60fps — enough for additive load + init

    [MenuItem("SKIM/Capture Boot Flow Screenshot")]
    public static void CaptureBootFlow()
    {
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../screenshots"));
        Directory.CreateDirectory(outDir);
        EditorPrefs.SetString(OutDirKey, outDir);
        EditorPrefs.SetInt(FrameKey, 0);
        EditorPrefs.SetInt(SunkFrameKey, -1);

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload
                                             | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        EditorApplication.update += OnUpdate;
        EditorApplication.EnterPlaymode();
    }

    static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;

        int frame = EditorPrefs.GetInt(FrameKey, 0) + 1;
        EditorPrefs.SetInt(FrameKey, frame);

        if (frame == CaptureAtFrame)
        {
            Capture("boot_flow.png");
        }
        else if (frame == CaptureAtFrame + 10)
        {
            ClickTab("TabSettings");
        }
        else if (frame == CaptureAtFrame + 15)
        {
            Capture("settings_panel.png");
        }
        else if (frame == CaptureAtFrame + 20)
        {
            // The tab bar lives inside MainPanel, which hides when a sub-panel is
            // shown — has to go back to the hub before another tab is clickable.
            ClickTab("BackButton");
        }
        else if (frame == CaptureAtFrame + 25)
        {
            ClickTab("TabAchievements");
        }
        else if (frame == CaptureAtFrame + 30)
        {
            Capture("achievements_panel.png");
        }
        else if (frame == CaptureAtFrame + 35)
        {
            ClickTab("BackButton");
        }
        else if (frame == CaptureAtFrame + 40)
        {
            ClickTab("LaunchButton"); // hides the menu, shows the HUD (0.2s pop-out anim)
        }
        else if (frame == CaptureAtFrame + 100)
        {
            SimulateLaunch(); // bypasses gesture detection, drives the same Launch() a real flick would
            EditorPrefs.SetInt(LaunchFrameKey, frame);
        }
        else if (frame > CaptureAtFrame + 100)
        {
            int sinceLaunch = frame - EditorPrefs.GetInt(LaunchFrameKey, frame);

            if (sinceLaunch % 200 == 0) LogStoneState(sinceLaunch);
            if (sinceLaunch == 120) Capture("gameplay_1.png");
            if (sinceLaunch == 500) Capture("gameplay_2.png");

            bool sunk = ServiceLocator.TryGet<IStoneSimulator>(out var s)
                     && s.CurrentState.Phase == StoneState.StonePhase.Sunk;
            int sunkFrame = EditorPrefs.GetInt(SunkFrameKey, -1);
            if (sunk && sunkFrame < 0) { sunkFrame = frame; EditorPrefs.SetInt(SunkFrameKey, sunkFrame); Capture("gameplay_sunk.png"); }

            if ((sunkFrame >= 0 && frame - sunkFrame >= 40) || sinceLaunch >= 8000)
            {
                if (sunkFrame >= 0) Capture("post_launch.png");
                else Debug.LogWarning("[SceneScreenshotTool] Stone never sunk within the wait window.");

                EditorApplication.update -= OnUpdate;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }

    const string SplashCap1Key = "SKIM_Screenshot_SplashCap1";
    const string SplashCap2Key = "SKIM_Screenshot_SplashCap2";

    [MenuItem("SKIM/Capture Splash Screen")]
    public static void CaptureSplashScreen()
    {
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../screenshots"));
        Directory.CreateDirectory(outDir);
        EditorPrefs.SetString(OutDirKey, outDir);
        EditorPrefs.SetBool(SplashCap1Key, false);
        EditorPrefs.SetBool(SplashCap2Key, false);

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload
                                             | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Splash.unity");
        EditorApplication.update += OnSplashUpdate;
        EditorApplication.EnterPlaymode();
    }

    // Uses Time.time, not a tick count — batch-mode editor ticks can be a fraction of a
    // millisecond apart (see the gameplay capture above), so real elapsed seconds is the
    // only reliable way to land these captures mid-animation.
    static void OnSplashUpdate()
    {
        if (!EditorApplication.isPlaying) return;

        if (!EditorPrefs.GetBool(SplashCap1Key) && Time.time >= 0.5f)
        {
            EditorPrefs.SetBool(SplashCap1Key, true);
            Capture("splash_1_fadein.png");
        }
        else if (!EditorPrefs.GetBool(SplashCap2Key) && Time.time >= 1.0f)
        {
            EditorPrefs.SetBool(SplashCap2Key, true);
            Capture("splash_2_hop.png");
        }
        else if (Time.time >= 1.6f)
        {
            Capture("splash_3_late.png");
            EditorApplication.update -= OnSplashUpdate;
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }

    static void LogStoneState(int sinceLaunch)
    {
        if (!ServiceLocator.TryGet<IStoneSimulator>(out var sim)) return;
        var st = sim.CurrentState;
        Debug.Log($"[SceneScreenshotTool] t+{sinceLaunch} phase={st.Phase} pos={st.Position} " +
                  $"skips={st.SkipCount} Time.time={Time.time:F2} deltaTime={Time.deltaTime:F4}");
    }

    // Drives the real MainMenuController.onClick listener instead of simulating a
    // pointer — same effect, no input-system plumbing needed for a QA capture.
    static void ClickTab(string tabName)
    {
        var tab = GameObject.Find(tabName)?.GetComponent<UnityEngine.UI.Button>();
        if (tab == null) { Debug.LogError($"[SceneScreenshotTool] Tab '{tabName}' not found."); return; }
        tab.onClick.Invoke();
    }

    // Drives StoneSimulatorImpl.Launch() directly — same physics/scoring/VFX/bonus-zone
    // path a real flick gesture triggers (see GameBootstrapper.WireEvents), just skipping
    // the touch-drag detection itself, which isn't worth simulating for a QA capture.
    static void SimulateLaunch()
    {
        if (!ServiceLocator.TryGet<IStoneSimulator>(out var sim))
        {
            Debug.LogError("[SceneScreenshotTool] IStoneSimulator not registered.");
            return;
        }
        ServiceLocator.TryGet<IScoringSystem>(out var scoring);
        ServiceLocator.TryGet<IBonusZoneSystem>(out var zones);
        ServiceLocator.TryGet<IProgressionSystem>(out var prog);

        sim.OnImpact += state => Debug.Log(
            $"[SceneScreenshotTool] Impact skip={state.SkipCount} pos={state.Position} speed={state.Velocity.magnitude:F2}");
        sim.OnSunk += result => Debug.Log(
            $"[SceneScreenshotTool] Sunk dist={result.Distance:F1}m skips={result.SkipCount} score={result.Score}");

        scoring?.ResetForNewLaunch();
        zones?.GenerateForLaunch();
        sim.Launch(new FlickInput(50f, 0.85f, 0.3f), prog?.SelectedStone, false);
        Debug.Log("[SceneScreenshotTool] Launch triggered.");
    }

    static void Capture(string fileName)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[SceneScreenshotTool] Camera.main is null at capture time.");
            return;
        }

        // Screen Space Overlay canvases draw straight to the display, bypassing any
        // camera — invisible to Camera.Render(). Switch them to Screen Space Camera
        // just for this capture so the UI shows up in the render texture too.
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var prevModes = new RenderMode[canvases.Length];
        var prevCams = new Camera[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            prevModes[i] = canvases[i].renderMode;
            prevCams[i] = canvases[i].worldCamera;
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f;
            }
        }

        int w = 1080, h = 1920; // portrait, typical mobile
        var rt = new RenderTexture(w, h, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        float prevNear = cam.nearClipPlane;
        cam.nearClipPlane = Mathf.Min(prevNear, 0.1f);

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        cam.nearClipPlane = prevNear;
        RenderTexture.active = prevActive;

        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = prevModes[i];
            canvases[i].worldCamera = prevCams[i];
        }

        var path = Path.Combine(EditorPrefs.GetString(OutDirKey), fileName);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        Debug.Log("[SceneScreenshotTool] Saved " + path);
    }
}
