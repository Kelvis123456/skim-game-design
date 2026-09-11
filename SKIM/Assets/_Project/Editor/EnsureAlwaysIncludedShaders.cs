using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

// Every shader looked up at runtime via Shader.Find (water, sky, stone/VFX/bonus-zone
// fallback unlit materials) gets stripped from device builds unless it's referenced by
// a Material actually included in the build, or listed here. Nothing referenced these by
// Material, so builds shipped with a null shader — Android rendered the ocean as solid
// magenta (Unity's missing-shader fallback) and would have hit the same for stones/VFX.
public static class EnsureAlwaysIncludedShaders
{
    static readonly string[] RequiredShaders =
    {
        "SKIM/Water",
        "SKIM/GradientSky",
        "Universal Render Pipeline/Unlit",
        "Universal Render Pipeline/Particles/Unlit",
        "Unlit/Color",
    };

    [MenuItem("SKIM/Ensure Always-Included Shaders")]
    public static void Run()
    {
        var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
        var so = new SerializedObject(graphicsSettings);
        var prop = so.FindProperty("m_AlwaysIncludedShaders");

        int added = 0;
        foreach (var name in RequiredShaders)
        {
            var shader = Shader.Find(name);
            if (shader == null)
            {
                Debug.LogError($"[EnsureShaders] Shader not found in editor: {name}");
                continue;
            }

            bool alreadyPresent = false;
            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == shader) { alreadyPresent = true; break; }
            }
            if (alreadyPresent) continue;

            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = shader;
            added++;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnsureShaders] Added {added} shader(s) to Always Included Shaders.");
    }
}
