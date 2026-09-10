using System.IO;
using UnityEditor;
using UnityEngine;

// Procedural app icon — same house style as everything else in this project
// (StoneMeshFactory, SKIMMenuSetup's rounded sprites): no imported art, drawn
// from the Fase 6 palette by code.
public static class IconGenerator
{
    const string ICON_PATH = "Assets/_Project/Art/Generated/icon_1024.png";
    const int SIZE = 1024;

    static readonly Color BG_TOP = Hex("0A1628");
    static readonly Color BG_BOTTOM = Hex("0F2035");
    static readonly Color TEAL = Hex("00C4CC");
    static readonly Color STONE = Hex("B8C5D0");
    static readonly Color STONE_HIGHLIGHT = Hex("E8EEF4");

    [MenuItem("SKIM/Generate App Icon")]
    public static void GenerateAndAssign()
    {
        var tex = Render();
        Directory.CreateDirectory(Path.GetDirectoryName(ICON_PATH));
        File.WriteAllBytes(ICON_PATH, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ICON_PATH, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON_PATH);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        if (icon == null) { Debug.LogError("[IconGenerator] Icon failed to import."); return; }

        AssignToGroup(BuildTargetGroup.Android, icon);
        AssignToGroup(BuildTargetGroup.iOS, icon);
        AssignToGroup(BuildTargetGroup.Unknown, icon); // default icon (Editor, Standalone/.exe)

        Debug.Log("[IconGenerator] App icon generated and assigned.");
    }

    static void AssignToGroup(BuildTargetGroup group, Texture2D icon)
    {
        var sizes = PlayerSettings.GetIconSizesForTargetGroup(group);
        if (sizes == null || sizes.Length == 0) return;
        var icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = icon;
        PlayerSettings.SetIconsForTargetGroup(group, icons);
    }

    static Texture2D Render()
    {
        var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        var pixels = new Color[SIZE * SIZE];

        float cx = SIZE * 0.5f;
        float cy = SIZE * 0.52f;
        float stoneCy = SIZE * 0.60f;
        float stoneRx = SIZE * 0.27f;
        float stoneRy = SIZE * 0.16f;

        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                Color c = Color.Lerp(BG_TOP, BG_BOTTOM, y / (float)(SIZE - 1));

                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                c = Color.Lerp(c, TEAL, RingAlpha(dist, SIZE * 0.34f, SIZE * 0.020f) * 0.95f);
                c = Color.Lerp(c, TEAL, RingAlpha(dist, SIZE * 0.42f, SIZE * 0.012f) * 0.5f);
                c = Color.Lerp(c, TEAL, RingAlpha(dist, SIZE * 0.48f, SIZE * 0.008f) * 0.25f);

                float ex = (x - cx) / stoneRx;
                float ey = (y - stoneCy) / stoneRy;
                float ellipse = ex * ex + ey * ey;
                if (ellipse < 1.05f)
                {
                    float edge = Mathf.Clamp01((1.05f - ellipse) * 30f);
                    c = Color.Lerp(c, STONE, edge);

                    float hx = (x - (cx - stoneRx * 0.32f)) / (stoneRx * 0.55f);
                    float hy = (y - (stoneCy - stoneRy * 0.45f)) / (stoneRy * 0.45f);
                    float h = hx * hx + hy * hy;
                    if (h < 1f) c = Color.Lerp(c, STONE_HIGHLIGHT, Mathf.Clamp01(1f - h) * 0.5f * edge);
                }

                pixels[y * SIZE + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static float RingAlpha(float dist, float radius, float thickness)
    {
        float d = Mathf.Abs(dist - radius);
        return Mathf.Clamp01(1f - d / thickness);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
