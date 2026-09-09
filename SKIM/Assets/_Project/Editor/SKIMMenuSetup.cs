using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Builds the menu UI described in fase5-uxui into the Game scene: main screen,
// stone selector, climate selector, settings and the daily challenge panel.
// Idempotent — rebuilds the canvas from scratch each run.
public static class SKIMMenuSetup
{
    const string GAME_SCENE = "Assets/_Project/Scenes/Game.unity";
    const string SPRITE_DIR = "Assets/_Project/Art/Generated";

    // Palette — fase6-arte section 2.
    static readonly Color BG      = Hex("0A1628");
    static readonly Color CARD    = Hex("0F2035");
    static readonly Color BORDER  = Hex("1E3A5F");
    static readonly Color TEAL    = Hex("00C4CC");
    static readonly Color GOLD    = Hex("F4D03F");
    static readonly Color MUTED   = Hex("8DB4D4");
    static readonly Color WHITE   = Hex("FFFFFF");
    static readonly Color LOCKED  = Hex("091422");

    static Sprite _card16, _card20, _card24, _pill35, _circle;

    [MenuItem("SKIM/Build Menus")]
    public static void BuildMenus()
    {
        var scene = EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);

        var existing = GameObject.Find("MenuCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        EnsureSprites();
        EnsureEventSystem();

        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var menu = canvasGO.AddComponent<MainMenuController>();

        var templates = new GameObject("Templates");
        templates.transform.SetParent(canvasGO.transform, false);

        var mainPanel     = BuildMainPanel(canvasGO.transform, out var climateLabel, out var recordLabel, out var launchBtn,
                                           out var tabStones, out var tabClimates, out var tabChallenge, out var tabSettings);
        var stonePanel    = BuildStonePanel(canvasGO.transform, templates.transform);
        var climatePanel  = BuildClimatePanel(canvasGO.transform, templates.transform);
        var settingsPanel = BuildSettingsPanel(canvasGO.transform);
        var challengePanel = BuildChallengePanel(canvasGO.transform);

        templates.SetActive(false);

        Wire(menu, "_hudRoot", GameObject.Find("HUD"));
        Wire(menu, "_recordLabel", recordLabel);
        Wire(menu, "_climateLabel", climateLabel);
        Wire(menu, "_mainPanel", mainPanel);
        Wire(menu, "_stoneSelectorPanel", stonePanel);
        Wire(menu, "_climateSelectorPanel", climatePanel);
        Wire(menu, "_settingsPanel", settingsPanel);
        Wire(menu, "_dailyChallengePanel", challengePanel);
        Wire(menu, "_launchButton", launchBtn);
        Wire(menu, "_tabStones", tabStones);
        Wire(menu, "_tabClimates", tabClimates);
        Wire(menu, "_tabSettings", tabSettings);
        Wire(menu, "_tabDailyChallenge", tabChallenge);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SKIMMenus] MenuCanvas rebuilt in Game.unity");
    }

    // ─────────────────────────── MAIN SCREEN ───────────────────────────

    static GameObject BuildMainPanel(Transform parent, out TMP_Text climateLabel, out TMP_Text recordLabel,
                                     out Button launch, out Button tabStones, out Button tabClimates,
                                     out Button tabChallenge, out Button tabSettings)
    {
        var panel = Panel(parent, "MainPanel");

        Title(panel.transform, "SKIM", 128f, new Vector2(0f, -190f));
        Label(panel.transform, "Una piedra. Un flick. El océano entero.", 30f, MUTED,
              new Vector2(0f, -390f), new Vector2(900f, 50f));

        // Climate card
        var climateCard = Card(panel.transform, "ClimateCard", new Vector2(0f, -500f), new Vector2(900f, 190f), _card20);
        CardTextLeft(climateCard.transform, "CLIMA ACTUAL", 26f, MUTED, 48f, 400f);
        climateLabel = CardTextLeft(climateCard.transform, "CALMA", 52f, TEAL, -22f, 500f);

        // Stats card
        var statsCard = Card(panel.transform, "StatsCard", new Vector2(0f, -720f), new Vector2(900f, 190f), _card20);
        CardTextLeft(statsCard.transform, "TU MEJOR TIRADA", 26f, MUTED, 48f, 400f);
        recordLabel = CardTextLeft(statsCard.transform, "RÉCORD: 0.0m", 46f, WHITE, -22f, 600f);

        // Daily challenge card with progress bar
        var challengeCard = Card(panel.transform, "ChallengeCard", new Vector2(0f, -960f), new Vector2(900f, 250f), _card20);
        CardTextLeft(challengeCard.transform, "DESAFÍO DIARIO", 26f, MUTED, 82f, 400f);
        CardTextLeft(challengeCard.transform, "Recorre 100m en una sola tirada", 32f, WHITE, 28f, 700f);
        ProgressBar(challengeCard.transform, new Vector2(0f, -30f), new Vector2(800f, 16f), 0.47f);
        CardTextLeft(challengeCard.transform, "47m / 100m", 26f, MUTED, -78f, 300f);
        CardTextRight(challengeCard.transform, "+120 conchas", 26f, GOLD, -78f, 300f);

        // Primary CTA — teal pill, 70px tall at 1x (140 at this 2x reference)
        launch = PillButton(panel.transform, "LaunchButton", "LANZAR", new Vector2(0f, -1200f), new Vector2(760f, 140f));

        // Tab bar
        var tabs = new GameObject("TabBar", typeof(RectTransform));
        tabs.transform.SetParent(panel.transform, false);
        var tabsRect = tabs.GetComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0.5f, 1f);
        tabsRect.anchorMax = new Vector2(0.5f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.anchoredPosition = new Vector2(0f, -1400f);
        tabsRect.sizeDelta = new Vector2(1000f, 120f);

        tabStones    = TabButton(tabs.transform, "TabStones", "Colección", -375f);
        tabClimates  = TabButton(tabs.transform, "TabClimates", "Clima", -125f);
        tabChallenge = TabButton(tabs.transform, "TabChallenge", "Desafíos", 125f);
        tabSettings  = TabButton(tabs.transform, "TabSettings", "Config", 375f);

        return panel;
    }

    // ─────────────────────────── SELECTORS ───────────────────────────

    static GameObject BuildStonePanel(Transform parent, Transform templates)
    {
        var panel = Panel(parent, "StonePanel");
        Title(panel.transform, "COLECCIÓN", 64f, new Vector2(0f, -160f));
        Label(panel.transform, "Cada piedra cambia el rebote y la sensibilidad al spin", 28f, MUTED,
              new Vector2(0f, -250f), new Vector2(900f, 40f));

        var container = Container(panel.transform, new Vector2(0f, -330f), new Vector2(920f, 900f), 24f);
        var template = RowTemplate(templates, "StoneRow", withBadge: true);

        var screen = panel.AddComponent<StoneSelectorScreen>();
        Wire(screen, "_rowContainer", container.transform);
        Wire(screen, "_rowPrefab", template);

        BackButton(panel.transform);
        return panel;
    }

    static GameObject BuildClimatePanel(Transform parent, Transform templates)
    {
        var panel = Panel(parent, "ClimatePanel");
        Title(panel.transform, "CLIMAS", 64f, new Vector2(0f, -160f));
        var count = Label(panel.transform, "2/5 niveles", 28f, TEAL, new Vector2(0f, -250f), new Vector2(400f, 40f));

        var container = Container(panel.transform, new Vector2(0f, -330f), new Vector2(920f, 1000f), 24f);
        var template = RowTemplate(templates, "ClimateRow", withBadge: false);

        var screen = panel.AddComponent<ClimateSelectorScreen>();
        Wire(screen, "_rowContainer", container.transform);
        Wire(screen, "_rowPrefab", template);
        Wire(screen, "_unlockedCountLabel", count);

        BackButton(panel.transform);
        return panel;
    }

    // ─────────────────────────── SETTINGS ───────────────────────────

    static GameObject BuildSettingsPanel(Transform parent)
    {
        var panel = Panel(parent, "SettingsPanel");
        Title(panel.transform, "CONFIGURACIÓN", 58f, new Vector2(0f, -160f));

        Label(panel.transform, "AUDIO", 26f, MUTED, new Vector2(-370f, -280f), new Vector2(300f, 40f),
              TextAlignmentOptions.Left);
        var audioCard = Card(panel.transform, "AudioCard", new Vector2(0f, -330f), new Vector2(900f, 260f), _card20);
        var music = SliderRow(audioCard.transform, "Música", new Vector2(0f, 60f), 0.7f);
        var sfx   = SliderRow(audioCard.transform, "Efectos", new Vector2(0f, -60f), 1f);

        Label(panel.transform, "ACCESIBILIDAD", 26f, MUTED, new Vector2(-330f, -630f), new Vector2(400f, 40f),
              TextAlignmentOptions.Left);
        var a11yCard = Card(panel.transform, "A11yCard", new Vector2(0f, -690f), new Vector2(900f, 150f), _card20);
        var vibration = ToggleRow(a11yCard.transform, "Vibración", new Vector2(0f, 0f), true);

        var deleteBtn = TextButton(panel.transform, "DeleteData", "Eliminar datos", Hex("E74C3C"),
                                   new Vector2(0f, -880f), new Vector2(500f, 80f));

        var screen = panel.AddComponent<SettingsScreen>();
        Wire(screen, "_musicSlider", music);
        Wire(screen, "_sfxSlider", sfx);
        Wire(screen, "_vibrationToggle", vibration);
        Wire(screen, "_deleteDataButton", deleteBtn);

        BackButton(panel.transform);
        return panel;
    }

    static GameObject BuildChallengePanel(Transform parent)
    {
        var panel = Panel(parent, "ChallengePanel");
        Title(panel.transform, "DESAFÍO DIARIO", 58f, new Vector2(0f, -160f));

        var card = Card(panel.transform, "ChallengeMain", new Vector2(0f, -340f), new Vector2(900f, 340f), _card24);
        CardTextLeft(card.transform, "DISTANCIA", 26f, TEAL, 118f, 400f);
        CardTextLeft(card.transform, "Recorre 100m en una sola tirada", 34f, WHITE, 62f, 700f);
        ProgressBar(card.transform, new Vector2(0f, 0f), new Vector2(800f, 16f), 0.47f);
        CardTextLeft(card.transform, "47m / 100m  ·  47%", 28f, MUTED, -52f, 420f);
        CardTextRight(card.transform, "+120 conchas", 30f, GOLD, -52f, 320f);
        CardTextLeft(card.transform, "Renueva en 6h 12m", 26f, MUTED, -118f, 500f);

        BackButton(panel.transform);
        return panel;
    }

    // ─────────────────────────── WIDGETS ───────────────────────────

    static GameObject Panel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());

        var img = go.AddComponent<Image>();
        img.color = new Color(BG.r, BG.g, BG.b, 0.94f);
        return go;
    }

    static GameObject Card(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = CARD;

        var border = new GameObject("Border", typeof(RectTransform));
        border.transform.SetParent(go.transform, false);
        Stretch(border.GetComponent<RectTransform>());
        var bimg = border.AddComponent<Image>();
        bimg.sprite = sprite;
        bimg.type = Image.Type.Sliced;
        bimg.color = new Color(BORDER.r, BORDER.g, BORDER.b, 0.9f);
        bimg.raycastTarget = false;
        border.transform.SetAsFirstSibling();

        return go;
    }

    static TMP_Text Title(Transform parent, string text, float size, Vector2 pos)
        => Label(parent, text, size, WHITE, pos, new Vector2(900f, size * 1.4f));

    // Text pinned a fixed distance from a card's left edge, so it never spills out
    // regardless of the card's width.
    static TMP_Text CardTextLeft(Transform card, string text, float size, Color color, float y, float width,
                                 float padding = 44f)
    {
        var tmp = Label(card, text, size, color, Vector2.zero, new Vector2(width, size * 1.5f),
                        TextAlignmentOptions.Left);
        var rect = tmp.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(padding, y);
        return tmp;
    }

    static TMP_Text CardTextRight(Transform card, string text, float size, Color color, float y, float width,
                                  float padding = 44f)
    {
        var tmp = Label(card, text, size, color, Vector2.zero, new Vector2(width, size * 1.5f),
                        TextAlignmentOptions.Right);
        var rect = tmp.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-padding, y);
        return tmp;
    }

    static TMP_Text Label(Transform parent, string text, float size, Color color, Vector2 pos, Vector2 sizeDelta,
                          TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;

        var rect = tmp.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = sizeDelta;
        return tmp;
    }

    static Button PillButton(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = _pill35;
        img.type = Image.Type.Sliced;
        img.color = TEAL;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var tmp = Label(go.transform, text, 44f, WHITE, Vector2.zero, size);
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.anchorMin = tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.anchoredPosition = Vector2.zero;

        return btn;
    }

    static Button TabButton(Transform parent, string name, string text, float x)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(230f, 100f);

        var img = go.AddComponent<Image>();
        img.sprite = _card16;
        img.type = Image.Type.Sliced;
        img.color = new Color(CARD.r, CARD.g, CARD.b, 0.85f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var tmp = Label(go.transform, text, 28f, MUTED, Vector2.zero, new Vector2(220f, 90f));
        tmp.rectTransform.anchorMin = tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.anchoredPosition = Vector2.zero;

        return btn;
    }

    static Button TextButton(Transform parent, string name, string text, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var tmp = Label(go.transform, text, 30f, color, Vector2.zero, size);
        tmp.rectTransform.anchorMin = tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.anchoredPosition = Vector2.zero;

        return btn;
    }

    static void BackButton(Transform parent)
    {
        var btn = TextButton(parent, "BackButton", "‹ Volver", MUTED, new Vector2(-380f, -70f), new Vector2(260f, 80f));
        btn.gameObject.AddComponent<SkimBackButton>();
    }

    static GameObject Container(Transform parent, Vector2 pos, Vector2 size, float spacing)
    {
        var go = new GameObject("RowContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        return go;
    }

    // Row shape the selector screens expect: children named Name/Desc/Lock plus a badge.
    static GameObject RowTemplate(Transform templateHolder, string name, bool withBadge)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(templateHolder, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 150f);

        var img = go.AddComponent<Image>();
        img.sprite = _card16;
        img.type = Image.Type.Sliced;
        img.color = CARD;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 150f;
        le.preferredHeight = 150f;

        var nameLabel = CardTextLeft(go.transform, "Nombre", 38f, WHITE, 26f, 500f);
        nameLabel.gameObject.name = "Name";
        nameLabel.fontStyle = FontStyles.Bold;

        var descLabel = CardTextLeft(go.transform, "Descripción", 26f, MUTED, -30f, 620f);
        descLabel.gameObject.name = "Desc";

        // The art direction calls for a lock icon; with no icon font shipped yet the
        // locked state is spelled out instead of rendering a tofu box.
        var lockLabel = CardTextRight(go.transform, "BLOQUEADA", 22f, MUTED, 0f, 260f);
        lockLabel.gameObject.name = "Lock";

        if (withBadge)
        {
            var badge = CardTextRight(go.transform, "EQUIPADA", 22f, TEAL, 0f, 260f);
            badge.gameObject.name = "EquippedBadge";
        }
        else
        {
            var active = new GameObject("ActiveBorder", typeof(RectTransform));
            active.transform.SetParent(go.transform, false);
            Stretch(active.GetComponent<RectTransform>());
            var aimg = active.AddComponent<Image>();
            aimg.sprite = _card16;
            aimg.type = Image.Type.Sliced;
            aimg.color = new Color(TEAL.r, TEAL.g, TEAL.b, 0.35f);
            aimg.raycastTarget = false;
        }

        return go;
    }

    static void ProgressBar(Transform parent, Vector2 pos, Vector2 size, float fill)
    {
        var track = new GameObject("ProgressTrack", typeof(RectTransform));
        track.transform.SetParent(parent, false);
        var rect = track.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var timg = track.AddComponent<Image>();
        timg.sprite = _pill35;
        timg.type = Image.Type.Sliced;
        timg.color = LOCKED;

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(track.transform, false);
        var frect = fillGO.GetComponent<RectTransform>();
        frect.anchorMin = new Vector2(0f, 0f);
        frect.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
        frect.offsetMin = Vector2.zero;
        frect.offsetMax = Vector2.zero;

        var fimg = fillGO.AddComponent<Image>();
        fimg.sprite = _pill35;
        fimg.type = Image.Type.Sliced;
        fimg.color = TEAL;
    }

    static Slider SliderRow(Transform parent, string label, Vector2 pos, float value)
    {
        CardTextLeft(parent, label, 30f, WHITE, pos.y, 300f);

        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(130f, pos.y);
        rect.sizeDelta = new Vector2(480f, 40f);

        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = _pill35;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = LOCKED;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = _pill35;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = TEAL;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var hrect = handle.GetComponent<RectTransform>();
        hrect.sizeDelta = new Vector2(44f, 44f);
        var hImg = handle.AddComponent<Image>();
        hImg.sprite = _circle;
        hImg.color = WHITE;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hrect;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;

        return slider;
    }

    static Toggle ToggleRow(Transform parent, string label, Vector2 pos, bool isOn)
    {
        CardTextLeft(parent, label, 30f, WHITE, pos.y, 300f);

        var go = new GameObject("Toggle", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(330f, pos.y);
        rect.sizeDelta = new Vector2(110f, 56f);

        var bgImg = go.AddComponent<Image>();
        bgImg.sprite = _pill35;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = isOn ? TEAL : LOCKED;

        var knob = new GameObject("Checkmark", typeof(RectTransform));
        knob.transform.SetParent(go.transform, false);
        var krect = knob.GetComponent<RectTransform>();
        krect.sizeDelta = new Vector2(44f, 44f);
        krect.anchoredPosition = new Vector2(isOn ? 24f : -24f, 0f);
        var kImg = knob.AddComponent<Image>();
        kImg.sprite = _circle;
        kImg.color = WHITE;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = kImg;
        toggle.isOn = isOn;

        return toggle;
    }

    // ─────────────────────────── SPRITES ───────────────────────────

    static void EnsureSprites()
    {
        Directory.CreateDirectory(SPRITE_DIR);
        // Radii are doubled from the spec because the canvas reference resolution
        // (1080x1920) is 2x the 540pt design frame the art direction was drawn at.
        _card16 = RoundedSprite("card16", 32);
        _card20 = RoundedSprite("card20", 40);
        _card24 = RoundedSprite("card24", 48);
        _pill35 = RoundedSprite("pill35", 70);
        _circle = RoundedSprite("circle", 32, 68);
    }

    // Unity UI has no corner-radius property, so the radii from the art direction
    // are baked into 9-sliced sprites generated here.
    static Sprite RoundedSprite(string name, int radius, int size = 0)
    {
        int dim = size > 0 ? size : radius * 2 + 4;
        string path = $"{SPRITE_DIR}/{name}.png";

        var tex = new Texture2D(dim, dim, TextureFormat.RGBA32, false);
        var pixels = new Color32[dim * dim];

        for (int y = 0; y < dim; y++)
        for (int x = 0; x < dim; x++)
        {
            float dx = Mathf.Max(radius - x, x - (dim - 1 - radius), 0f);
            float dy = Mathf.Max(radius - y, y - (dim - 1 - radius), 0f);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(radius - d + 0.5f);
            if (radius <= 0) a = 1f;
            pixels[y * dim + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(radius, radius, radius, radius);
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[SKIMMenus] rounded sprite failed to import: {path}");
        return sprite;
    }

    // ─────────────────────────── HELPERS ───────────────────────────

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Wire(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
