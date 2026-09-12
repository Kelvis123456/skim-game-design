using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

[InitializeOnLoad]
public static class SKIMSetup
{
    const string DONE_KEY = "SKIM_Setup_v7_Done";

    static SKIMSetup()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("SKIM/Run Setup Again")]
    public static void RunForced()
    {
        EditorPrefs.DeleteKey(DONE_KEY);
        Run();
    }

    // Boot.unity is committed (not regenerated per machine), so a system added after that
    // commit needs to be patched into the existing "Systems" GameObject instead of relying
    // on CreateBootScene, which no-ops once the scene file exists.
    [MenuItem("SKIM/Add Bonus Zone System")]
    public static void AddBonusZoneSystem()
    {
        const string path = "Assets/_Project/Scenes/Boot.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        var systemsGO = GameObject.Find("Systems");
        if (systemsGO == null) { Debug.LogError("[SKIM] Systems GameObject not found in Boot.unity."); return; }

        if (systemsGO.GetComponent<BonusZoneSystemImpl>() == null)
            systemsGO.AddComponent<BonusZoneSystemImpl>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SKIM] BonusZoneSystemImpl ensured on Systems in Boot.unity");
    }

    // ─────────────────────────── LIVE SCENE PATCHES ────────────────────

    // CreateGameScene() below only runs once, the first time Game.unity doesn't
    // exist yet — it's dead code for any later change, same trap AddBonusZoneSystem
    // above already worked around. This patches the ALREADY-EXISTING HUD and
    // PostLaunch panel in place instead, per the visual-audit findings: the HUD
    // canvas never had its scaler configured (ConstantPixelSize by default, while
    // the menu canvas uses ScaleWithScreenSize — they live in different coordinate
    // systems), its 3 readouts had inconsistent rect heights (baseline misalignment)
    // and sat right at the screen edge (under the status bar/cutout on a real
    // device), and PostLaunch was a 320x360 sharp-cornered rect with bare unlabeled
    // numbers — 4.4% of the screen, never visually verified until now.
    [MenuItem("SKIM/Patch HUD And Results Panel")]
    public static void PatchGameSceneUI()
    {
        const string path = "Assets/_Project/Scenes/Game.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        var card24 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Generated/card24.png");
        var pill35 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Generated/pill35.png");

        PatchHUD();
        PatchPostLaunch(card24, pill35);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SKIM] HUD and PostLaunch patched in Game.unity");
    }

    static void PatchHUD()
    {
        var hud = GameObject.Find("HUD");
        if (hud == null) { Debug.LogError("[SKIM] HUD not found."); return; }

        var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f; // matches MenuCanvas — same coordinate system

        const float margin = 48f; // was 20 — clears the status bar/camera cutout

        SetLabelRect(hud.transform, "ScoreLabel", new Vector2(0.5f, 1f), new Vector2(0f, -margin), new Vector2(300f, 60f));
        SetLabelRect(hud.transform, "DistanceLabel", new Vector2(0f, 1f), new Vector2(margin, -margin), new Vector2(200f, 60f));
        SetLabelRect(hud.transform, "MultiplierBadge", new Vector2(1f, 1f), new Vector2(-margin, -margin), new Vector2(160f, 60f));

        // A bare "0" with no unit/icon reads as meaningless on a first launch.
        var scoreLabelGO = hud.transform.Find("ScoreLabel");
        if (scoreLabelGO != null)
        {
            var caption = new GameObject("ScoreCaption", typeof(RectTransform));
            caption.transform.SetParent(scoreLabelGO, false);
            var tmp = caption.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "PUNTOS";
            tmp.fontSize = 16f;
            tmp.color = new Color(0.553f, 0.706f, 0.831f, 0.85f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            var rect = tmp.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -62f);
            rect.sizeDelta = new Vector2(300f, 30f);
        }
    }

    static void SetLabelRect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var t = parent.Find(name);
        if (t == null) return;
        var rect = (RectTransform)t;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size; // same height (60) on all three — was 60/40/40, causing the
                                // ~14px baseline drop the audit measured between them
    }

    static void PatchPostLaunch(Sprite card24, Sprite pill35)
    {
        var postLaunch = GameObject.Find("PostLaunch");
        if (postLaunch == null) { Debug.LogError("[SKIM] PostLaunch not found."); return; }

        // Rebuilt at menu scale (900x620, card24, real per-metric captions) instead
        // of the original 320x360 sharp-cornered rect with bare unlabeled numbers —
        // named children the controller looks up with transform.Find are preserved.
        // postLaunch's own Image is the BORDER (full size); a smaller inset "Fill"
        // child shows the actual card color — same fix as SKIMMenuSetup's Card(),
        // where Border-as-child-of-Fill meant no border ever actually rendered.
        var border = postLaunch.GetComponent<UnityEngine.UI.Image>();
        border.sprite = card24;
        border.type = UnityEngine.UI.Image.Type.Sliced;
        border.color = new Color(0.118f, 0.227f, 0.373f, 0.9f); // BORDER
        var borderRect = border.rectTransform;
        borderRect.sizeDelta = new Vector2(900f, 620f);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(postLaunch.transform, false);
        var fillRect = (RectTransform)fill.transform;
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f); fillRect.offsetMax = new Vector2(-4f, -4f);
        var fillImg = fill.AddComponent<UnityEngine.UI.Image>();
        fillImg.sprite = card24;
        fillImg.type = UnityEngine.UI.Image.Type.Sliced;
        fillImg.color = new Color(0.059f, 0.125f, 0.208f, 0.97f); // CARD, opaque enough to read over the ocean
        fillImg.raycastTarget = false;
        fill.transform.SetAsFirstSibling(); // drawn right after the parent's own border graphic

        RestyleResultLabel(postLaunch.transform, "DistResult", "DIST_CAPTION", "DISTANCIA", 72f, FontStyles.Bold, Color.white, new Vector2(0f, 190f));
        RestyleResultLabel(postLaunch.transform, "SkipsResult", "SKIPS_CAPTION", "SALTOS", 40f, FontStyles.Normal,
            new Color(0.553f, 0.706f, 0.831f), new Vector2(-190f, 40f));
        RestyleResultLabel(postLaunch.transform, "ScoreResult", "SCORE_CAPTION", "PUNTOS", 40f, FontStyles.Normal,
            new Color(0.553f, 0.706f, 0.831f), new Vector2(190f, 40f));

        // No separate chip background here: RecordBadge must stay both the
        // GameObject transform.Find("RecordBadge") looks up AND the direct holder
        // of the TMP_Text (PostLaunchController does GetComponent<TMP_Text> on the
        // exact object it finds) — a child background would draw ON TOP of that
        // text, since uGUI draws parents before their children. Bold + gold text
        // is the safe improvement without touching PostLaunchController.cs.
        var recordBadge = postLaunch.transform.Find("RecordBadge")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (recordBadge != null)
        {
            recordBadge.fontSize = 30f;
            recordBadge.fontStyle = FontStyles.Bold;
            recordBadge.characterSpacing = 2f;
            var rrect = recordBadge.rectTransform;
            rrect.anchoredPosition = new Vector2(0f, -90f);
            rrect.sizeDelta = new Vector2(600f, 70f);
        }

        // Retry button — was a 200x50 sharp rectangle tinted with a raw float
        // literal; now the same pill35 + dark-on-teal treatment as LANZAR.
        var retryBtnGO = GameObject.Find("RetryButton");
        if (retryBtnGO != null && retryBtnGO.transform.IsChildOf(postLaunch.transform))
        {
            var retryImg = retryBtnGO.GetComponent<UnityEngine.UI.Image>();
            retryImg.sprite = pill35;
            retryImg.type = UnityEngine.UI.Image.Type.Sliced;
            retryImg.color = new Color(0f, 0.769f, 0.8f);
            var retryRect = retryImg.rectTransform;
            retryRect.sizeDelta = new Vector2(500f, 110f);
            retryRect.anchoredPosition = new Vector2(0f, -230f);

            var retryText = retryBtnGO.transform.Find("RetryText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (retryText != null)
            {
                retryText.fontSize = 34f;
                retryText.fontStyle = FontStyles.Bold;
                retryText.color = new Color(0.02f, 0.05f, 0.09f);
                retryText.characterSpacing = 4f;
            }
        }
    }

    static void RestyleResultLabel(Transform parent, string labelName, string captionName, string captionText,
                                    float labelSize, FontStyles style, Color labelColor, Vector2 pos)
    {
        var labelT = parent.Find(labelName);
        if (labelT == null) return;
        var label = labelT.GetComponent<TMPro.TextMeshProUGUI>();
        label.fontSize = labelSize;
        label.fontStyle = style;
        label.color = labelColor;
        var rect = label.rectTransform;
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(380f, labelSize * 1.3f);

        // A bare "0.0m" / "25" / "8,500" tells the player nothing about which
        // metric it is — a small MUTED uppercase caption above each fixes that.
        if (labelT.Find(captionName) != null) return; // idempotent — patch may re-run
        var captionGO = new GameObject(captionName, typeof(RectTransform));
        captionGO.transform.SetParent(labelT, false);
        var caption = captionGO.AddComponent<TMPro.TextMeshProUGUI>();
        caption.text = captionText;
        caption.fontSize = 22f;
        caption.color = new Color(0.553f, 0.706f, 0.831f, 0.8f);
        caption.alignment = TMPro.TextAlignmentOptions.Center;
        caption.characterSpacing = 4f;
        var crect = caption.rectTransform;
        crect.anchorMin = crect.anchorMax = new Vector2(0.5f, 1f);
        crect.pivot = new Vector2(0.5f, 1f);
        crect.anchoredPosition = new Vector2(0f, labelSize * 0.75f);
        crect.sizeDelta = new Vector2(380f, 30f);
    }

    // ─────────────────────────── SPLASH SCENE ──────────────────────────

    [MenuItem("SKIM/Create Splash Scene")]
    public static void CreateSplashScene()
    {
        const string path = "Assets/_Project/Scenes/Splash.unity";
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.039f, 0.086f, 0.157f);
            camGO.AddComponent<AudioListener>();

            Directory.CreateDirectory("Assets/_Project/Art/Generated");
            var circle = CreateCircleSprite("Assets/_Project/Art/Generated/splash_circle.png", 256);
            var ring = CreateRingSprite("Assets/_Project/Art/Generated/splash_ring.png", 256, 0.62f, 0.9f);

            var canvasGO = new GameObject("SplashCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bgGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.039f, 0.086f, 0.157f);

            var ringGO = new GameObject("Ring", typeof(RectTransform));
            ringGO.transform.SetParent(canvasGO.transform, false);
            var ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = new Vector2(0f, -80f);
            ringRect.sizeDelta = new Vector2(360f, 360f);
            var ringImg = ringGO.AddComponent<UnityEngine.UI.Image>();
            ringImg.sprite = ring;
            ringImg.color = new Color(0f, 0.77f, 0.8f, 0.85f);

            var stoneGO = new GameObject("Stone", typeof(RectTransform));
            stoneGO.transform.SetParent(canvasGO.transform, false);
            var stoneRect = stoneGO.GetComponent<RectTransform>();
            stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            stoneRect.pivot = new Vector2(0.5f, 0.5f);
            stoneRect.anchoredPosition = new Vector2(0f, -80f);
            stoneRect.sizeDelta = new Vector2(90f, 60f);
            var stoneImg = stoneGO.AddComponent<UnityEngine.UI.Image>();
            stoneImg.sprite = circle;
            stoneImg.color = new Color(0.722f, 0.773f, 0.816f);

            var titleTmp = new GameObject("Title", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            titleTmp.transform.SetParent(canvasGO.transform, false);
            titleTmp.text = "SKIM";
            titleTmp.fontSize = 140f;
            titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.rectTransform.anchorMin = titleTmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleTmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleTmp.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            titleTmp.rectTransform.sizeDelta = new Vector2(900f, 200f);

            var subTmp = new GameObject("Subtitle", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            subTmp.transform.SetParent(canvasGO.transform, false);
            subTmp.text = "Una piedra. Un flick. El océano entero.";
            subTmp.fontSize = 32f;
            subTmp.alignment = TMPro.TextAlignmentOptions.Center;
            subTmp.color = new Color(0.553f, 0.706f, 0.831f);
            subTmp.rectTransform.anchorMin = subTmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            subTmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subTmp.rectTransform.anchoredPosition = new Vector2(0f, 140f);
            subTmp.rectTransform.sizeDelta = new Vector2(900f, 60f);

            var controller = canvasGO.AddComponent<SplashController>();
            SetPrivateField(controller, "_canvasGroup", canvasGroup);
            SetPrivateField(controller, "_stone", stoneRect);
            SetPrivateField(controller, "_ring", ringRect);
            SetPrivateField(controller, "_ringImage", ringImg);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[SKIM] Splash.unity created");
        }

        var scenes = EditorBuildSettings.scenes;
        if (!System.Array.Exists(scenes, s => s.path == path))
        {
            var list = new List<EditorBuildSettingsScene>(scenes);
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
        Debug.Log("[SKIM] Splash scene ensured as build index 0");
    }

    static Sprite CreateCircleSprite(string path, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(r - d + 0.5f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite CreateRingSprite(string path, int size, float innerRatio, float outerRatio)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float r = size / 2f;
        float innerR = r * innerRatio;
        float outerR = r * outerRatio;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float aOuter = Mathf.Clamp01(outerR - d + 1.5f);
            float aInner = Mathf.Clamp01(d - innerR + 1.5f);
            float a = Mathf.Min(aOuter, aInner);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Run()
    {
        if (EditorPrefs.GetBool(DONE_KEY, false)) return;

        Debug.Log("[SKIM] Starting automatic setup...");

        ConfigureURP();
        CreateBootScene();
        CreateGameScene();
        AddScenesToBuildSettings();
        ConfigureProjectSettings();

        EditorPrefs.SetBool(DONE_KEY, true);
        Debug.Log("[SKIM] Setup complete! Press Play on Boot scene to start.");
    }

    // ─────────────────────────── URP SETUP ────────────────────────────

    static void ConfigureURP()
    {
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            Debug.Log("[SKIM] URP already configured.");
            return;
        }

        Directory.CreateDirectory("Assets/_Project/Settings");

        // Create Forward Renderer Data
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/_Project/Settings/URPRenderer.asset");

        // Create URP Pipeline Asset pointing to renderer
        var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
        pipelineAsset.renderScale = 1f;
        pipelineAsset.shadowDistance = 0f;  // shadows off — mobile perf
        AssetDatabase.CreateAsset(pipelineAsset, "Assets/_Project/Settings/URPAsset.asset");

        // Assign to ALL quality levels
        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        for (int i = 0; i < QualitySettings.count; i++)
            QualitySettings.SetQualityLevel(i, false);
        QualitySettings.renderPipeline = pipelineAsset;

        AssetDatabase.SaveAssets();
        Debug.Log("[SKIM] URP Pipeline Asset created and assigned.");
    }

    // ─────────────────────────── BOOT SCENE ───────────────────────────

    static void CreateBootScene()
    {
        const string path = "Assets/_Project/Scenes/Boot.unity";
        if (File.Exists(path)) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // One GameObject holds all systems
        var systemsGO = new GameObject("Systems");

        systemsGO.AddComponent<GameBootstrapper>();
        systemsGO.AddComponent<StoneSimulatorImpl>();

        var ocean = systemsGO.AddComponent<OceanSystemImpl>();
        systemsGO.AddComponent<InputControllerImpl>();
        systemsGO.AddComponent<AudioSystemImpl>();
        systemsGO.AddComponent<ScoringSystemImpl>();
        systemsGO.AddComponent<ProgressionSystemImpl>();
        systemsGO.AddComponent<VFXSystemImpl>();
        systemsGO.AddComponent<EconomySystemImpl>();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[SKIM] Boot.unity created");
    }

    // ─────────────────────────── GAME SCENE ───────────────────────────

    static void CreateGameScene()
    {
        const string path = "Assets/_Project/Scenes/Game.unity";
        if (File.Exists(path)) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Guard — redirects to Boot if Game is played directly without Boot
        var guardGO = new GameObject("GameSceneGuard");
        guardGO.AddComponent<GameSceneGuard>();

        // Directional light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        light.color = new Color(1f, 0.97f, 0.88f);
        light.colorTemperature = 6500f;
        light.useColorTemperature = true;
        light.shadows = LightShadows.None;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Main Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        cam.backgroundColor = new Color(0.04f, 0.09f, 0.16f);
        camGO.AddComponent<CameraController>();
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0f, 4f, -8f);
        camGO.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        // Stone placeholder (sphere — replaced with real model later)
        var stoneGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stoneGO.name = "Stone";
        stoneGO.transform.localScale = Vector3.one * 0.16f;
        stoneGO.transform.position = new Vector3(0f, 0.5f, 0f);
        Object.DestroyImmediate(stoneGO.GetComponent<SphereCollider>());
        stoneGO.AddComponent<StoneVisual>();

        // Wire camera to stone
        var camCtrl = camGO.GetComponent<CameraController>();
        var stoneTransformField = typeof(CameraController).GetField(
            "_stoneTransform",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        stoneTransformField?.SetValue(camCtrl, stoneGO.transform);

        // HUD Canvas
        var hudGO = new GameObject("HUD");
        var canvas = hudGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        hudGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        hudGO.AddComponent<HUDController>();

        // Score label
        var scoreLabelGO = new GameObject("ScoreLabel");
        scoreLabelGO.transform.SetParent(hudGO.transform, false);
        var scoreLabel = scoreLabelGO.AddComponent<TMPro.TextMeshProUGUI>();
        scoreLabel.text = "0";
        scoreLabel.fontSize = 48f;
        scoreLabel.alignment = TMPro.TextAlignmentOptions.Center;
        scoreLabel.color = Color.white;
        var scoreRect = scoreLabel.rectTransform;
        scoreRect.anchorMin = new Vector2(0.5f, 1f);
        scoreRect.anchorMax = new Vector2(0.5f, 1f);
        scoreRect.pivot = new Vector2(0.5f, 1f);
        scoreRect.anchoredPosition = new Vector2(0f, -20f);
        scoreRect.sizeDelta = new Vector2(300f, 60f);

        // Distance label
        var distGO = new GameObject("DistanceLabel");
        distGO.transform.SetParent(hudGO.transform, false);
        var distLabel = distGO.AddComponent<TMPro.TextMeshProUGUI>();
        distLabel.text = "0m";
        distLabel.fontSize = 28f;
        distLabel.color = new Color(0.55f, 0.71f, 0.83f);
        var distRect = distLabel.rectTransform;
        distRect.anchorMin = new Vector2(0f, 1f);
        distRect.anchorMax = new Vector2(0f, 1f);
        distRect.pivot = new Vector2(0f, 1f);
        distRect.anchoredPosition = new Vector2(20f, -20f);
        distRect.sizeDelta = new Vector2(200f, 40f);

        // Multiplier badge
        var multGO = new GameObject("MultiplierBadge");
        multGO.transform.SetParent(hudGO.transform, false);
        var multLabel = multGO.AddComponent<TMPro.TextMeshProUGUI>();
        multLabel.text = "×1.0";
        multLabel.fontSize = 28f;
        multLabel.color = new Color(0f, 0.77f, 0.8f);
        var multRect = multLabel.rectTransform;
        multRect.anchorMin = new Vector2(1f, 1f);
        multRect.anchorMax = new Vector2(1f, 1f);
        multRect.pivot = new Vector2(1f, 1f);
        multRect.anchoredPosition = new Vector2(-20f, -20f);
        multRect.sizeDelta = new Vector2(120f, 40f);

        // Post-Launch panel
        var postLaunchGO = new GameObject("PostLaunch");
        postLaunchGO.transform.SetParent(hudGO.transform, false);
        // Always active — PostLaunchController hides itself via CanvasGroup in Start()
        var cg = postLaunchGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        var plCtrl = postLaunchGO.AddComponent<PostLaunchController>();

        var bgImg = postLaunchGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.09f, 0.16f, 0.92f);
        var bgRect = bgImg.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(320f, 360f);
        bgRect.anchoredPosition = Vector2.zero;

        var distResultGO = CreateLabel(postLaunchGO.transform, "DistResult", "0.0m", 36f, new Vector2(0f, 80f));
        var skipsResultGO = CreateLabel(postLaunchGO.transform, "SkipsResult", "0", 28f, new Vector2(0f, 30f));
        var scoreResultGO = CreateLabel(postLaunchGO.transform, "ScoreResult", "0", 28f, new Vector2(0f, -10f));
        var recordBadgeGO = CreateLabel(postLaunchGO.transform, "RecordBadge", "¡NUEVO RÉCORD!", 24f, new Vector2(0f, -50f));
        recordBadgeGO.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.96f, 0.82f, 0.25f);
        recordBadgeGO.SetActive(false);

        var retryBtnGO = new GameObject("RetryButton");
        retryBtnGO.transform.SetParent(postLaunchGO.transform, false);
        var retryImg = retryBtnGO.AddComponent<UnityEngine.UI.Image>();
        retryImg.color = new Color(0f, 0.77f, 0.8f);
        var retryRect = retryImg.rectTransform;
        retryRect.anchoredPosition = new Vector2(0f, -110f);
        retryRect.sizeDelta = new Vector2(200f, 50f);
        var retryBtn = retryBtnGO.AddComponent<UnityEngine.UI.Button>();
        var retryTextGO = CreateLabel(retryBtnGO.transform, "RetryText", "OTRA VEZ", 22f, Vector2.zero);

        // HUDController and PostLaunchController are self-contained:
        // they find their children by name at runtime — no reflection wiring needed.

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[SKIM] Game.unity created");
    }

    // ─────────────────────────── BUILD SETTINGS ───────────────────────

    static void AddScenesToBuildSettings()
    {
        const string splashPath = "Assets/_Project/Scenes/Splash.unity";
        var list = new List<EditorBuildSettingsScene>();
        if (File.Exists(splashPath)) list.Add(new EditorBuildSettingsScene(splashPath, true));
        list.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Boot.unity", true));
        list.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity", true));

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[SKIM] Build Settings updated: " + string.Join(", ", list.ConvertAll(s => s.path)));
    }

    // ─────────────────────────── PROJECT SETTINGS ─────────────────────

    static void ConfigureProjectSettings()
    {
        PlayerSettings.companyName = "IndieStudio";
        PlayerSettings.productName = "SKIM";
        PlayerSettings.applicationIdentifier = "com.indieStudio.skim";
        PlayerSettings.bundleVersion = "1.0.0";

        // Android
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // iOS
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);

        // General
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        AssetDatabase.SaveAssets();

        // Open Boot as the active editor scene so Play always starts from Boot
        if (!Application.isPlaying)
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");

        Debug.Log("[SKIM] Project Settings configured — Boot.unity is now active. Press Play.");
    }

    // ─────────────────────────── HELPERS ──────────────────────────────

    static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    static GameObject CreateLabel(Transform parent, string name, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(280f, 45f);
        return go;
    }
}
