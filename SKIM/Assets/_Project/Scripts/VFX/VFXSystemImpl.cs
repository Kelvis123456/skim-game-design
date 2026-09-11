using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static IAudioSystem;

public class VFXSystemImpl : MonoBehaviour, IVFXSystem
{
    Transform _ringPool;
    LineRenderer _pbLine;
    [SerializeField] TMP_Text _scorePopupPrefab;
    [SerializeField] int _maxRings = 200;

    ParticleSystem _splashSystem;
    ParticleSystem _sparkleSystem;
    TrailRenderer _comboTrail;
    Light _comboLight;
    Coroutine _pbPulseRoutine;
    Coroutine _chordPulseRoutine;

    readonly Queue<GameObject> _rings = new();
    readonly List<GameObject> _activeRings = new();

    static readonly Color[] NOTE_COLORS =
    {
        new Color(0f, 0.77f, 0.8f),    // C — teal
        new Color(0.31f, 0.8f, 0.77f), // D
        new Color(0.48f, 0.78f, 0.64f), // E
        new Color(0.66f, 0.85f, 0.66f), // G
        new Color(0.78f, 0.9f, 0.78f)   // A
    };

    static Material _lineMat;

    static Material CreateRingMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (shader != null && shader.name.StartsWith("Universal"))
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f); // transparent blend
        }
        else
            mat.color = color;
        return mat;
    }

    static Material GetLineMaterial()
    {
        if (_lineMat != null) return _lineMat;
        _lineMat = CreateRingMaterial(new Color(0f, 0.77f, 0.8f));
        return _lineMat;
    }

    void Awake()
    {
        var poolGO = new GameObject("RingPool");
        poolGO.transform.SetParent(transform);
        _ringPool = poolGO.transform;

        var pbGO = new GameObject("PBLine");
        pbGO.transform.SetParent(transform);
        _pbLine = pbGO.AddComponent<LineRenderer>();
        _pbLine.positionCount = 2;
        _pbLine.widthMultiplier = 0.05f;
        _pbLine.material = GetLineMaterial();
        _pbLine.startColor = new Color(0.96f, 0.82f, 0.25f, 0.6f);
        _pbLine.endColor   = new Color(0.96f, 0.82f, 0.25f, 0.6f);
        _pbLine.enabled = false;

        PrewarmRingPool();
        _splashSystem = BuildParticleSystem("SplashParticles", new Color(0.667f, 0.867f, 0.933f), 40);
        _sparkleSystem = BuildParticleSystem("SparkleParticles", new Color(0.961f, 0.820f, 0.251f), 20);
        BuildComboTrail();
    }

    ParticleSystem BuildParticleSystem(string name, Color color, int capacity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = capacity;
        main.startSize = 0.04f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        renderer.material = new Material(shader);
        renderer.material.color = color;
        renderer.material.mainTexture = GetSoftCircleTexture();

        return ps;
    }

    static Texture2D _softCircleTex;

    // Shuriken's default quad renders as a flat, hard-edged square with no
    // texture — this gives droplets/sparkles a soft round falloff instead.
    // Generated at runtime (not an imported asset) so it also works in a build.
    static Texture2D GetSoftCircleTexture()
    {
        if (_softCircleTex != null) return _softCircleTex;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), center) / center.x;
            float a = Mathf.Clamp01(1f - d);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        _softCircleTex = tex;
        return tex;
    }

    // A single reusable trail + light for the equipped stone's combo state — cheaper
    // than spawning/destroying per launch, per fase6-arte section 5 "Glow de Combo".
    void BuildComboTrail()
    {
        var go = new GameObject("ComboTrail");
        go.transform.SetParent(transform);

        _comboTrail = go.AddComponent<TrailRenderer>();
        _comboTrail.time = 0.35f;
        _comboTrail.widthMultiplier = 0f;
        _comboTrail.emitting = false;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        _comboTrail.material = new Material(shader);

        var lightGO = new GameObject("ComboLight");
        lightGO.transform.SetParent(go.transform);
        _comboLight = lightGO.AddComponent<Light>();
        _comboLight.type = LightType.Point;
        _comboLight.intensity = 0f;
        _comboLight.range = 0.3f;
        _comboLight.color = new Color(0f, 0.769f, 0.8f);
        _comboLight.enabled = false;
    }

    void PrewarmRingPool()
    {
        for (int i = 0; i < _maxRings; i++)
        {
            var ring = CreateRingObject();
            ring.SetActive(false);
            _rings.Enqueue(ring);
        }
    }

    GameObject CreateRingObject()
    {
        var go = new GameObject("Ring");
        go.transform.SetParent(_ringPool);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 33;
        lr.loop = true;
        lr.widthMultiplier = 0.05f;
        lr.colorGradient = MakeGradient(NOTE_COLORS[0]);
        lr.material = CreateRingMaterial(NOTE_COLORS[0]);
        return go;
    }

    static Gradient MakeGradient(Color col)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    public void SpawnWaterRing(Vector3 worldPos, MusicalNote note)
    {
        if (_activeRings.Count >= _maxRings) RecycleOldestRing();

        GameObject ring;
        if (_rings.Count > 0) ring = _rings.Dequeue();
        else ring = CreateRingObject();

        ring.SetActive(true);
        ring.transform.position = new Vector3(worldPos.x, worldPos.y + 0.02f, worldPos.z);
        _activeRings.Add(ring);

        var col = NOTE_COLORS[(int)note % NOTE_COLORS.Length];
        var lr = ring.GetComponent<LineRenderer>();
        lr.material = CreateRingMaterial(col);
        lr.startColor = col;
        lr.endColor = new Color(col.r, col.g, col.b, 0f);

        StartCoroutine(AnimateRing(ring, lr, col, 60f));
    }

    IEnumerator AnimateRing(GameObject ring, LineRenderer lr, Color col, float lifetime)
    {
        float elapsed = 0f;
        const float maxRadius = 3.5f;

        while (elapsed < lifetime && ring.activeSelf)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            float r = Mathf.Lerp(0.15f, maxRadius, Mathf.Sqrt(t));
            float alpha = Mathf.Lerp(1f, 0f, t * t);

            for (int i = 0; i <= 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                lr.SetPosition(i, ring.transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }

            lr.startColor = new Color(col.r, col.g, col.b, alpha);
            lr.endColor   = new Color(col.r, col.g, col.b, 0f);
            yield return null;
        }

        ReturnRingToPool(ring);
    }

    void RecycleOldestRing()
    {
        if (_activeRings.Count == 0) return;
        ReturnRingToPool(_activeRings[0]);
        _activeRings.RemoveAt(0);
    }

    void ReturnRingToPool(GameObject ring)
    {
        ring.SetActive(false);
        _activeRings.Remove(ring);
        _rings.Enqueue(ring);
    }

    // fase6-arte section 5 "Splash de Impacto": 6-12 droplets scaled by force, real
    // gravity, 0.4-0.8s lifetime. Was previously drawn with Debug.DrawLine, which is
    // an editor-only gizmo — invisible in any actual build. A real ParticleSystem
    // renders everywhere.
    public void SpawnImpactSplash(Vector3 pos, float force)
    {
        if (_splashSystem == null) return;

        int count = Mathf.RoundToInt(Mathf.Lerp(6f, 12f, Mathf.Clamp01(force / 12f)));
        var particles = new ParticleSystem.EmitParams();
        particles.position = pos;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(0.5f, 3f);
            particles.velocity = new Vector3(Mathf.Cos(angle) * speed, Random.Range(2f, 5f) * 0.6f,
                                             Mathf.Sin(angle) * speed);
            particles.startLifetime = Random.Range(0.4f, 0.8f);
            particles.startSize = Random.Range(0.03f, 0.05f);
            _splashSystem.Emit(particles, 1);
        }
    }

    public void SpawnScorePopup(Vector3 worldPos, int score, bool isCombo)
    {
        if (score <= 0) return;
        EnsureScorePopupTemplate();

        // Nudged above the impact point so it doesn't spawn stacked on the water
        // ring's expansion origin.
        var spawnPos = worldPos + Vector3.up * 0.15f;
        var txt = Instantiate(_scorePopupPrefab, spawnPos, Quaternion.identity);
        txt.gameObject.SetActive(true);
        txt.text = isCombo ? $"×COMBO +{score}" : $"+{score}";
        txt.color = isCombo ? new Color(0.96f, 0.82f, 0.25f) : new Color(0f, 0.77f, 0.8f);
        StartCoroutine(AnimatePopup(txt));
    }

    // No prefab is authored for this yet, so build one the first time it's needed —
    // matches fase6-arte section 5 "Score Pop-up": world-space floating text.
    // The stone itself is only ~0.16m — a UI-scale fontSize/localScale of 1 renders
    // many meters wide, so both are scaled down to match the scene's real size.
    void EnsureScorePopupTemplate()
    {
        if (_scorePopupPrefab != null) return;

        var go = new GameObject("ScorePopupTemplate");
        go.transform.SetParent(transform);
        go.transform.localScale = Vector3.one * 0.035f;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.gameObject.SetActive(false);
        _scorePopupPrefab = tmp;
    }

    IEnumerator AnimatePopup(TMP_Text txt)
    {
        float t = 0f;
        var startPos = txt.transform.position;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            txt.transform.position = startPos + Vector3.up * (t * 0.5f);
            var c = txt.color;
            txt.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, Mathf.Pow(t / 1.2f, 2f)));
            yield return null;
        }
        Destroy(txt.gameObject);
    }

    // fase6-arte section 5 "Efecto de Nuevo Récord": PB line pulses 3x then moves,
    // 8 gold sparkles emerge, ~1.5s total.
    public void TriggerNewRecordEffect(float dist)
    {
        if (_pbPulseRoutine != null) StopCoroutine(_pbPulseRoutine);
        _pbPulseRoutine = StartCoroutine(PulseRecordLine(dist));

        if (_sparkleSystem == null) return;
        var pos = new Vector3(dist, 0.1f, 0f);
        var particles = new ParticleSystem.EmitParams { position = pos };
        for (int i = 0; i < 8; i++)
        {
            float angle = i / 8f * Mathf.PI * 2f;
            particles.velocity = new Vector3(Mathf.Cos(angle) * 0.8f, Random.Range(1.5f, 2.5f), Mathf.Sin(angle) * 0.8f);
            particles.startLifetime = 1.2f;
            particles.startSize = 0.05f;
            _sparkleSystem.Emit(particles, 1);
        }
    }

    IEnumerator PulseRecordLine(float newDist)
    {
        if (_pbLine == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            yield return FadeLine(0.6f, 1f, 0.15f);
            yield return FadeLine(1f, 0.6f, 0.15f);
        }
        UpdatePBLine(newDist);
    }

    IEnumerator FadeLine(float from, float to, float duration)
    {
        float t = 0f;
        var baseColor = new Color(0.96f, 0.82f, 0.25f);
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            var c = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            _pbLine.startColor = c;
            _pbLine.endColor = c;
            yield return null;
        }
    }

    // fase6-arte section 5 "MAX (×5+)": the ocean briefly brightens on a chord
    // resolution — implemented as a pulse of the water's crest bioluminescence.
    public void TriggerChordResolutionPulse()
    {
        if (!ServiceLocator.TryGet<IOceanSystem>(out var ocean)) return;
        if (_chordPulseRoutine != null) StopCoroutine(_chordPulseRoutine);
        _chordPulseRoutine = StartCoroutine(PulseCoroutine(ocean));
    }

    IEnumerator PulseCoroutine(IOceanSystem ocean)
    {
        ocean.PulseCrestBoost(1f, 0.15f);
        yield return new WaitForSeconds(0.15f);
        ocean.PulseCrestBoost(0.35f, 0.45f);
    }

    // fase6-arte section 5 "Glow de Combo": trail + point light scale with skip
    // count. comboLevel: 0 = none (×1-2), 1 = ×3-4, 2 = ×5-6, 3 = ×7+.
    public void UpdateComboTrail(Transform stoneTransform, int comboLevel)
    {
        if (_comboTrail == null || _comboLight == null) return;

        if (comboLevel <= 0 || stoneTransform == null)
        {
            ClearComboTrail();
            return;
        }

        _comboTrail.transform.SetParent(stoneTransform, false);
        _comboTrail.transform.localPosition = Vector3.zero;
        _comboTrail.emitting = true;

        (float intensity, float range, float width) = comboLevel switch
        {
            1 => (0.3f, 0.3f, 0.03f),
            2 => (0.6f, 0.5f, 0.05f),
            _ => (1.0f, 0.8f, 0.08f),
        };

        _comboLight.enabled = true;
        _comboLight.intensity = intensity;
        _comboLight.range = range;
        _comboTrail.widthMultiplier = width;
    }

    public void ClearComboTrail()
    {
        if (_comboTrail != null)
        {
            _comboTrail.emitting = false;
            _comboTrail.widthMultiplier = 0f;
            _comboTrail.transform.SetParent(transform, false);
        }
        if (_comboLight != null) _comboLight.enabled = false;
    }

    public void ClearSessionRings()
    {
        foreach (var r in new List<GameObject>(_activeRings)) ReturnRingToPool(r);
        _activeRings.Clear();
    }

    public void UpdatePBLine(float dist)
    {
        if (_pbLine == null) return;
        _pbLine.SetPosition(0, new Vector3(dist, 0.05f, -5f));
        _pbLine.SetPosition(1, new Vector3(dist, 0.05f, 5f));
    }
}
