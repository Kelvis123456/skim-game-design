using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static IAudioSystem;

public class VFXSystemImpl : MonoBehaviour, IVFXSystem
{
    Transform _ringPool;
    LineRenderer _pbLine;
    ParticleSystem _splashPS;
    ParticleSystem _starPS;
    [SerializeField] TMP_Text _scorePopupPrefab;
    [SerializeField] int _maxRings = 200;

    readonly Queue<GameObject> _rings = new();
    readonly List<GameObject> _activeRings = new();

    // Combo trail state — lazily attached to whichever stone Transform is passed in.
    Transform _comboStone;
    Light _comboLight;
    TrailRenderer _comboTrail;
    Coroutine _chordPulseRoutine;
    Coroutine _recordPulseRoutine;

    static readonly Color COMBO_GLOW_COLOR = new Color(0f, 0.77f, 0.8f);
    static readonly Color GOLD = new Color(0.96f, 0.82f, 0.25f);

    // Fase4-GDD §2.4: 3 semi-transparent points showing the initial trajectory only (not
    // bounces) while the player is dragging, so the angle/force gesture is perceptible
    // before release. Plain GameObjects, not pooled — there are only ever 3, held for the
    // life of the game scene.
    GameObject[] _trajectoryDots;
    const float TRAJECTORY_DOT_RADIUS = 0.05f;
    static readonly Color TRAJECTORY_DOT_COLOR = new Color(1f, 1f, 1f, 0.45f);

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

        _splashPS = CreateParticleSystem("SplashPS", new Color(0.67f, 0.87f, 0.93f), 40, 1f);
        _starPS = CreateParticleSystem("StarPS", GOLD, 16, 0.3f);

        PrewarmRingPool();
        CreateTrajectoryDots();
    }

    void CreateTrajectoryDots()
    {
        _trajectoryDots = new GameObject[3];
        for (int i = 0; i < _trajectoryDots.Length; i++)
        {
            var go = new GameObject($"TrajectoryDot{i}");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 16;
            lr.widthMultiplier = 0.025f;
            lr.material = CreateRingMaterial(TRAJECTORY_DOT_COLOR);
            lr.startColor = TRAJECTORY_DOT_COLOR;
            lr.endColor = TRAJECTORY_DOT_COLOR;
            for (int p = 0; p < 16; p++)
            {
                float a = p / 16f * Mathf.PI * 2f;
                lr.SetPosition(p, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * TRAJECTORY_DOT_RADIUS);
            }

            go.SetActive(false);
            _trajectoryDots[i] = go;
        }
    }

    ParticleSystem CreateParticleSystem(string name, Color color, int maxParticles, float gravityModifier)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.7f;
        main.startSpeed = 2f;
        main.startSize = 0.05f;
        main.startColor = color;
        main.gravityModifier = gravityModifier;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false; // emitted manually via Emit()

        var shape = ps.shape;
        shape.enabled = false; // we set velocity per-particle via EmitParams

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = CreateRingMaterial(color);
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        return ps;
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

    static bool ReduceEffects => ServiceLocator.TryGet<IProgressionSystem>(out var p) && p.ReduceEffects;

    public void SpawnImpactSplash(Vector3 pos, float force)
    {
        if (ReduceEffects) return;
        int count = Mathf.RoundToInt(Mathf.Lerp(3f, 8f, Mathf.Clamp01(force / 12f)));
        var emitParams = new ParticleSystem.EmitParams { position = pos };
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            emitParams.velocity = new Vector3(Mathf.Cos(angle) * Random.Range(1f, 3f),
                                              Random.Range(2f, 5f),
                                              Mathf.Sin(angle) * Random.Range(1f, 3f));
            emitParams.startSize = Random.Range(0.03f, 0.06f);
            _splashPS.Emit(emitParams, 1);
        }
    }

    public void SpawnScorePopup(Vector3 worldPos, int score, bool isCombo)
    {
        if (_scorePopupPrefab == null) return;
        var txt = Instantiate(_scorePopupPrefab, worldPos, Quaternion.identity);
        txt.text = isCombo ? $"×COMBO +{score}" : $"+{score}";
        txt.color = isCombo ? new Color(0.96f, 0.82f, 0.25f) : new Color(0f, 0.77f, 0.8f);
        StartCoroutine(AnimatePopup(txt));
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

    // Combo glow — Fase 6 §5: no glow below lvl2, then rising point-light intensity/radius + a trail.
    public void UpdateComboTrail(Transform stoneTransform, int comboLevel)
    {
        if (stoneTransform == null) return;

        if (_comboStone != stoneTransform)
        {
            ClearComboTrail();
            _comboStone = stoneTransform;
            var lightGO = new GameObject("ComboLight");
            lightGO.transform.SetParent(stoneTransform, false);
            _comboLight = lightGO.AddComponent<Light>();
            _comboLight.type = LightType.Point;
            _comboLight.color = COMBO_GLOW_COLOR;

            _comboTrail = stoneTransform.gameObject.AddComponent<TrailRenderer>();
            _comboTrail.material = CreateRingMaterial(COMBO_GLOW_COLOR);
            _comboTrail.widthMultiplier = 0.03f;
            _comboTrail.time = 0.35f;
            _comboTrail.startColor = new Color(COMBO_GLOW_COLOR.r, COMBO_GLOW_COLOR.g, COMBO_GLOW_COLOR.b, 0.8f);
            _comboTrail.endColor = new Color(COMBO_GLOW_COLOR.r, COMBO_GLOW_COLOR.g, COMBO_GLOW_COLOR.b, 0f);
        }

        bool glowing = comboLevel >= 2;
        _comboLight.enabled = glowing;
        _comboTrail.emitting = comboLevel >= 3;

        _comboLight.intensity = comboLevel switch { 2 => 0.3f, 3 => 0.6f, >= 4 => 1.0f, _ => 0f };
        _comboLight.range = comboLevel switch { 2 => 0.3f, 3 => 0.5f, >= 4 => 0.8f, _ => 0f };
    }

    public void ClearComboTrail()
    {
        if (_comboLight != null) { Destroy(_comboLight.gameObject); _comboLight = null; }
        if (_comboTrail != null) { Destroy(_comboTrail); _comboTrail = null; }
        _comboStone = null;
    }

    // Combo ≥5 (multiplier ≥2.8): every ring visible in the session pulses teal→white→teal.
    public void TriggerChordResolutionPulse()
    {
        if (_chordPulseRoutine != null) StopCoroutine(_chordPulseRoutine);
        _chordPulseRoutine = StartCoroutine(ChordPulseRoutine());
    }

    IEnumerator ChordPulseRoutine()
    {
        const float duration = 0.3f;
        var rings = new List<GameObject>(_activeRings);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float e = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI); // teal -> white -> teal
            foreach (var ring in rings)
            {
                if (ring == null || !ring.activeSelf) continue;
                var lr = ring.GetComponent<LineRenderer>();
                var pulsed = Color.Lerp(lr.startColor, Color.white, e);
                lr.startColor = pulsed;
            }
            yield return null;
        }
        _chordPulseRoutine = null;
    }

    // New session-best distance: the PB line pulses gold 3 times, then 8 gold stars burst upward.
    public void TriggerNewRecordEffect(float dist)
    {
        if (_recordPulseRoutine != null) StopCoroutine(_recordPulseRoutine);
        _recordPulseRoutine = StartCoroutine(RecordEffectRoutine(dist));
    }

    IEnumerator RecordEffectRoutine(float dist)
    {
        if (!ReduceEffects)
        {
            var spawnPos = new Vector3(dist, 0.1f, 0f);
            var emitParams = new ParticleSystem.EmitParams { position = spawnPos };
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                emitParams.velocity = new Vector3(Mathf.Cos(angle) * 1.2f, Random.Range(2f, 3.5f), Mathf.Sin(angle) * 1.2f);
                emitParams.startSize = 0.08f;
                _starPS.Emit(emitParams, 1);
            }
        }

        if (_pbLine != null)
        {
            for (int pulse = 0; pulse < 3; pulse++)
            {
                float t = 0f;
                while (t < 0.2f)
                {
                    t += Time.deltaTime;
                    float e = Mathf.Sin(Mathf.Clamp01(t / 0.2f) * Mathf.PI);
                    _pbLine.startColor = Color.Lerp(new Color(0.96f, 0.82f, 0.25f, 0.6f), Color.white, e);
                    _pbLine.endColor = _pbLine.startColor;
                    yield return null;
                }
            }
            _pbLine.startColor = new Color(0.96f, 0.82f, 0.25f, 0.6f);
            _pbLine.endColor = _pbLine.startColor;
        }
        _recordPulseRoutine = null;
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

    public void ShowTrajectoryArc(Vector3[] points)
    {
        for (int i = 0; i < _trajectoryDots.Length; i++)
        {
            if (i >= points.Length) { _trajectoryDots[i].SetActive(false); continue; }
            _trajectoryDots[i].transform.position = points[i] + Vector3.up * 0.02f;
            _trajectoryDots[i].SetActive(true);
        }
    }

    public void HideTrajectoryArc()
    {
        foreach (var dot in _trajectoryDots) dot.SetActive(false);
    }
}
