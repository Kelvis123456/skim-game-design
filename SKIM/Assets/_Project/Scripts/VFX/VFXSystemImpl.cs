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

    public void SpawnImpactSplash(Vector3 pos, float force)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(3f, 8f, Mathf.Clamp01(force / 12f)));
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var vel = new Vector3(Mathf.Cos(angle) * Random.Range(1f, 3f),
                                  Random.Range(2f, 5f),
                                  Mathf.Sin(angle) * Random.Range(1f, 3f));
            StartCoroutine(SplashDot(pos, vel));
        }
    }

    IEnumerator SplashDot(Vector3 startPos, Vector3 vel)
    {
        // Draw as a debug point — no GameObject allocation
        float lifetime = 0.7f;
        float t = 0f;
        var pos = startPos;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            vel.y -= 9.8f * Time.deltaTime;
            pos += vel * Time.deltaTime;
            Debug.DrawLine(pos, pos + Vector3.up * 0.05f,
                           new Color(0.7f, 0.9f, 1f, 1f - t / lifetime));
            yield return null;
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

    public void TriggerNewRecordEffect(float dist) { }
    public void TriggerChordResolutionPulse() { }
    public void UpdateComboTrail(Transform st, int level) { }
    public void ClearComboTrail() { }

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
