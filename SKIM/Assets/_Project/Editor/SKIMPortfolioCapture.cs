using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off automation: builds the menus, boots the prototype and captures the
// menu, the collection screen and a real mid-skip gameplay frame — timed past
// the 3rd skip so the combo trail/light and splash particles are both visible.
public static class SKIMPortfolioCapture
{
    enum Step { EnteringPlay, WaitBoot, ShootMenu, OpenStones, ShootStones, StartRun, WaitCombo, ShootGame, Done }

    static Step _step = Step.EnteringPlay;
    static float _timer;
    static float _safety;
    static int _settleFrames;
    static bool _pastThirdSkip;
    static string _outDir;

    [MenuItem("SKIM/Capture Portfolio Screenshots")]
    public static void Run()
    {
        _outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        SKIMSetup.RunForced();
        SKIMMenuSetup.BuildMenus();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        OpenAndFocusGameView();

        _step = Step.EnteringPlay;
        _timer = _safety = 0f;
        _settleFrames = 0;
        _pastThirdSkip = false;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    static void OpenAndFocusGameView()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        var gameView = EditorWindow.GetWindow(gameViewType);
        gameView.position = new Rect(80, 60, 560, 1000);
        gameView.Show();
        gameView.Focus();
    }

    static void Tick()
    {
        _safety += 0.0166f;
        if (_safety > 60f) { Finish("timed out"); return; }

        switch (_step)
        {
            case Step.EnteringPlay:
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                {
                    _timer = 0f;
                    _step = Step.WaitBoot;
                }
                break;

            case Step.WaitBoot:
                _timer += Time.deltaTime;
                if (_timer > 2f) { Capture("skim_menu.png"); _step = Step.ShootMenu; _settleFrames = 0; }
                break;

            case Step.ShootMenu:
                if (Settled()) { ShowPanel("_stoneSelectorPanel"); _step = Step.OpenStones; _settleFrames = 0; }
                break;

            case Step.OpenStones:
                if (Settled()) { Capture("skim_stones.png"); _step = Step.ShootStones; _settleFrames = 0; }
                break;

            case Step.ShootStones:
                if (Settled()) { StartRun(); _step = Step.WaitCombo; _timer = 0f; }
                break;

            case Step.WaitCombo:
                if (ServiceLocator.TryGet<IStoneSimulator>(out var sim))
                {
                    var st = sim.CurrentState;
                    _timer += Time.deltaTime;
                    // wait past the 3rd skip so the combo trail/light have kicked in,
                    // then a little more so a splash/score-popup is mid-flight too
                    bool pastThird = st.Phase == StoneState.StonePhase.InFlight
                                  && st.SkipCount >= 3
                                  && st.CurrentHeight > 0.03f;
                    if (pastThird && !_pastThirdSkip) { _pastThirdSkip = true; _timer = 0f; }
                    if (_pastThirdSkip && _timer > 0.12f)
                    {
                        Capture("skim_gameplay.png");
                        _step = Step.ShootGame;
                        _settleFrames = 0;
                    }
                }
                break;

            case Step.ShootGame:
                if (Settled()) Finish("done");
                break;
        }
    }

    static bool Settled()
    {
        _settleFrames++;
        return _settleFrames > 6;
    }

    static void Capture(string file)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, file));
        Debug.Log("[SKIMCapture] captured " + file);
    }

    static void ShowPanel(string fieldName)
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        if (menu == null) return;

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var names = new[] { "_mainPanel", "_stoneSelectorPanel", "_climateSelectorPanel",
                            "_settingsPanel", "_dailyChallengePanel" };

        foreach (var n in names)
        {
            var panel = typeof(MainMenuController).GetField(n, flags)?.GetValue(menu) as GameObject;
            if (panel != null) panel.SetActive(n == fieldName);
        }
    }

    // Dismiss the menu the same way the LANZAR button does, then flick with a
    // strong forward force so we reliably get several skips.
    static void StartRun()
    {
        var canvas = GameObject.Find("MenuCanvas");
        if (canvas != null) canvas.SetActive(false);

        var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu != null)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var hud = typeof(MainMenuController).GetField("_hudRoot", flags)?.GetValue(menu) as GameObject;
            if (hud != null) hud.SetActive(true);
        }

        if (!ServiceLocator.TryGet<IStoneSimulator>(out var sim)) return;
        if (!ServiceLocator.TryGet<IProgressionSystem>(out var prog)) return;
        if (ServiceLocator.TryGet<IInputController>(out var input)) input.IsEnabled = true;

        PullCameraCloser();
        sim.Launch(new FlickInput(10f, 0.95f, 0.2f), prog.SelectedStone, false);
    }

    static void PullCameraCloser()
    {
        var cam = GameObject.FindWithTag("MainCamera");
        if (cam == null) return;
        var ctrl = cam.GetComponent<CameraController>();
        if (ctrl == null) return;

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        ctrl.GetType().GetField("_fixedY", flags)?.SetValue(ctrl, 2.6f);
        ctrl.GetType().GetField("_fixedZ", flags)?.SetValue(ctrl, -5.5f);
    }

    static void Finish(string reason)
    {
        _step = Step.Done;
        EditorApplication.update -= Tick;
        Debug.Log("[SKIMCapture] finishing: " + reason);
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}
