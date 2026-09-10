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
    const int CaptureAtFrame = 180; // ~3s at 60fps — enough for additive load + init

    [MenuItem("SKIM/Capture Boot Flow Screenshot")]
    public static void CaptureBootFlow()
    {
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../screenshots"));
        Directory.CreateDirectory(outDir);
        EditorPrefs.SetString(OutDirKey, outDir);
        EditorPrefs.SetInt(FrameKey, 0);

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
            EditorApplication.update -= OnUpdate;
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }

    // Drives the real MainMenuController.onClick listener instead of simulating a
    // pointer — same effect, no input-system plumbing needed for a QA capture.
    static void ClickTab(string tabName)
    {
        var tab = GameObject.Find(tabName)?.GetComponent<UnityEngine.UI.Button>();
        if (tab == null) { Debug.LogError($"[SceneScreenshotTool] Tab '{tabName}' not found."); return; }
        tab.onClick.Invoke();
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
