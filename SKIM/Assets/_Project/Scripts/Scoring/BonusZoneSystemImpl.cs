using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// GDD §2.5/§7.3 — floating scoring zones the stone must skip through.
// Regenerated fresh for every launch, same spirit as the procedural ocean:
// nothing about a throw repeats exactly.
public class BonusZoneSystemImpl : MonoBehaviour, IBonusZoneSystem
{
    class Zone
    {
        public float XCenter;
        public BonusZoneCategory Category;
        public float HalfWidth;
        public bool Hit;
        public GameObject Marker;
    }

    IOceanSystem _ocean;
    readonly List<Zone> _zones = new();

    const int MIN_ZONES = 3;
    const int MAX_ZONES = 5;
    const float MIN_SPACING = 18f;
    const float MAX_SPACING = 45f;

    public event Action<Vector3, BonusZoneCategory> OnZoneHit;

    public void SetOceanReference(IOceanSystem ocean) => _ocean = ocean;

    public void GenerateForLaunch()
    {
        ClearZones();

        int count = UnityEngine.Random.Range(MIN_ZONES, MAX_ZONES + 1);
        float x = UnityEngine.Random.Range(MIN_SPACING, MAX_SPACING);
        for (int i = 0; i < count; i++)
        {
            SpawnZone(x, RollCategory());
            x += UnityEngine.Random.Range(MIN_SPACING, MAX_SPACING);
        }
    }

    static BonusZoneCategory RollCategory()
    {
        float r = UnityEngine.Random.value;
        if (r < 0.55f) return BonusZoneCategory.Green;
        if (r < 0.85f) return BonusZoneCategory.Blue;
        if (r < 0.97f) return BonusZoneCategory.Gold;
        return BonusZoneCategory.Rainbow;
    }

    void SpawnZone(float x, BonusZoneCategory category)
    {
        float diameter = CategoryDiameter(category);
        var zone = new Zone { XCenter = x, Category = category, HalfWidth = diameter * 0.5f };

        float y = (_ocean?.GetHeightAt(x, _ocean.SessionTime) ?? 0f) + 0.06f;
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = $"BonusZone_{category}";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.SetParent(transform, false);
        marker.transform.position = new Vector3(x, y, 0f);
        marker.transform.localScale = new Vector3(diameter, 0.015f, diameter);
        var renderer = marker.GetComponent<Renderer>();
        renderer.material = MakeMarkerMaterial(category);
        if (category == BonusZoneCategory.Rainbow)
            marker.AddComponent<RainbowZoneHue>();

        // Colorblind accessibility (GDD §15): category is never color-only —
        // a label spells it out too.
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(marker.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        labelGO.transform.localScale = new Vector3(1f / diameter, 1f, 1f / diameter);
        var label = labelGO.AddComponent<TextMeshPro>();
        label.text = CategoryLabel(category);
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        zone.Marker = marker;
        _zones.Add(zone);
    }

    // GDD §2.5 diameters, in meters.
    static float CategoryDiameter(BonusZoneCategory category) => category switch
    {
        BonusZoneCategory.Green   => 3.0f,
        BonusZoneCategory.Blue    => 1.5f,
        BonusZoneCategory.Gold    => 0.6f,
        BonusZoneCategory.Rainbow => 1.0f,
        _ => 1.5f
    };

    // The "arco iris" zone is the one category defined by NOT having a fixed color —
    // cycle its hue slowly so it actually reads as a rainbow, matching its "muy rara
    // (evento)" status instead of just being purple.
    class RainbowZoneHue : MonoBehaviour
    {
        Renderer _renderer;
        bool _isUniversal;
        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _isUniversal = _renderer.material.shader.name.StartsWith("Universal");
        }
        void Update()
        {
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f, 1f), 0.75f, 1f);
            var translucent = new Color(c.r, c.g, c.b, 0.55f);
            if (_isUniversal) _renderer.material.SetColor("_BaseColor", translucent);
            else _renderer.material.color = translucent;
        }
    }

    static Material MakeMarkerMaterial(BonusZoneCategory category)
    {
        var color = CategoryColor(category);
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        var translucent = new Color(color.r, color.g, color.b, 0.55f);
        if (shader.name.StartsWith("Universal"))
        {
            mat.SetColor("_BaseColor", translucent);
            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else mat.color = translucent;
        return mat;
    }

    static string CategoryLabel(BonusZoneCategory category) => category switch
    {
        BonusZoneCategory.Green   => "+200",
        BonusZoneCategory.Blue    => "+500",
        BonusZoneCategory.Gold    => "+1500",
        BonusZoneCategory.Rainbow => "×2",
        _ => ""
    };

    static Color CategoryColor(BonusZoneCategory category) => category switch
    {
        BonusZoneCategory.Green   => new Color(0.3f, 0.85f, 0.4f),
        BonusZoneCategory.Blue    => new Color(0.2f, 0.55f, 0.95f),
        BonusZoneCategory.Gold    => new Color(0.96f, 0.82f, 0.25f),
        BonusZoneCategory.Rainbow => new Color(0.85f, 0.4f, 0.95f),
        _ => Color.white
    };

    public BonusZoneCategory? CheckHit(Vector3 impactPosition)
    {
        foreach (var zone in _zones)
        {
            if (zone.Hit) continue;
            if (Mathf.Abs(impactPosition.x - zone.XCenter) > zone.HalfWidth) continue;

            zone.Hit = true;
            if (zone.Marker != null) Destroy(zone.Marker);
            OnZoneHit?.Invoke(impactPosition, zone.Category);
            return zone.Category;
        }
        return null;
    }

    public void ClearZones()
    {
        foreach (var zone in _zones)
            if (zone.Marker != null) Destroy(zone.Marker);
        _zones.Clear();
    }
}
