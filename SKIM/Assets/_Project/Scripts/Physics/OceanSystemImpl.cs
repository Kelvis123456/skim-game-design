using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanSystemImpl : MonoBehaviour, IOceanSystem
{
    [SerializeField] ClimateData _defaultClimate;
    [SerializeField] int _meshCols = 80;
    [SerializeField] int _meshRows = 40;
    [SerializeField] float _meshWidth = 60f;
    [SerializeField] float _meshDepth = 30f;

    ClimateData _currentClimate;
    Mesh _mesh;
    Vector3[] _vertices;
    float _sessionTime;
    Material _mat;
    Material _skyMat;

    public ClimateData CurrentClimate => _currentClimate;
    public float SessionTime => _sessionTime;

    void Awake()
    {
        if (_defaultClimate == null)
            _defaultClimate = Resources.Load<ClimateData>("Data/Climates/Calma");
        _currentClimate = _defaultClimate;

        CreateMaterial();
        BuildMesh();
    }

    void CreateMaterial()
    {
        var shader = Shader.Find("SKIM/Water")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("[Ocean] No compatible shader found. Is URP configured in Project Settings > Graphics?");
            return;
        }

        var mat = new Material(shader);
        GetComponent<MeshRenderer>().material = mat;
        _mat = mat;

        ApplyClimateVisuals(_currentClimate);
        Debug.Log($"[Ocean] Using shader: {shader.name}");
    }

    // Pushes a climate's documented palette into the water material and the sky.
    void ApplyClimateVisuals(ClimateData climate)
    {
        if (climate == null || _mat == null) return;

        if (_mat.shader != null && _mat.shader.name == "SKIM/Water")
        {
            _mat.SetColor("_SurfaceColor", climate.WaterSurfaceColor);
            _mat.SetColor("_DepthColor", climate.WaterDepthColor);
            _mat.SetColor("_CrestColor", climate.WaterCrestColor);
            _mat.SetColor("_HorizonColor", climate.SkyHorizonColor);
            _mat.SetFloat("_WaveAmp", TotalWaveAmplitude(climate));
        }
        else
        {
            _mat.SetColor("_BaseColor", climate.WaterSurfaceColor);
        }

        ApplySky(climate);
    }

    void ApplySky(ClimateData climate)
    {
        var skyShader = Shader.Find("SKIM/GradientSky");
        if (skyShader == null) return;

        if (_skyMat == null || _skyMat.shader != skyShader)
        {
            _skyMat = new Material(skyShader);
            RenderSettings.skybox = _skyMat;
        }

        _skyMat.SetColor("_TopColor", climate.AtmosphereColor);
        _skyMat.SetColor("_HorizonColor", climate.SkyHorizonColor);
    }

    static float TotalWaveAmplitude(ClimateData climate)
    {
        float sum = 0f;
        foreach (var w in climate.Harmonics) sum += w.Amplitude;
        return Mathf.Max(sum, 0.01f);
    }

    void Update()
    {
        _sessionTime += Time.deltaTime;
        UpdateVertices();
        _mat?.SetFloat("_WaveTime", _sessionTime);
    }

    // CRITICAL: identical equation used in shader
    public float GetHeightAt(float x, float time)
    {
        if (_currentClimate == null) return 0f;
        float h = 0f;
        foreach (var w in _currentClimate.Harmonics)
        {
            float k = 2f * Mathf.PI / Mathf.Max(w.WaveLength, 0.01f);
            h += w.Amplitude * Mathf.Sin(k * x + w.AngularFrequency * time + w.InitialPhase);
        }
        return h;
    }

    public float GetSlopeAt(float x, float time)
    {
        if (_currentClimate == null) return 0f;
        float slope = 0f;
        foreach (var w in _currentClimate.Harmonics)
        {
            float k = 2f * Mathf.PI / Mathf.Max(w.WaveLength, 0.01f);
            slope += w.Amplitude * k * Mathf.Cos(k * x + w.AngularFrequency * time + w.InitialPhase);
        }
        return slope;
    }

    public void SetClimate(ClimateData climate, float transitionDuration = 2f)
    {
        StartCoroutine(TransitionTo(climate, transitionDuration));
    }

    Coroutine _crestPulseRoutine;

    public void PulseCrestBoost(float boost, float duration)
    {
        if (_mat == null) return;
        if (_crestPulseRoutine != null) StopCoroutine(_crestPulseRoutine);
        _crestPulseRoutine = StartCoroutine(CrestPulse(boost, duration));
    }

    IEnumerator CrestPulse(float target, float duration)
    {
        float start = _mat.GetFloat("_CrestBoost");
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _mat.SetFloat("_CrestBoost", Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        _mat.SetFloat("_CrestBoost", target);
    }

    IEnumerator TransitionTo(ClimateData target, float duration)
    {
        var from = _currentClimate;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            LerpVisuals(from, target, t);
            yield return null;
        }

        _currentClimate = target;
        ApplyClimateVisuals(target);
        RandomizePhases();
    }

    void LerpVisuals(ClimateData from, ClimateData to, float t)
    {
        if (_mat == null || to == null) return;
        if (from == null) { ApplyClimateVisuals(to); return; }

        _mat.SetColor("_SurfaceColor", Color.Lerp(from.WaterSurfaceColor, to.WaterSurfaceColor, t));
        _mat.SetColor("_DepthColor", Color.Lerp(from.WaterDepthColor, to.WaterDepthColor, t));
        _mat.SetColor("_CrestColor", Color.Lerp(from.WaterCrestColor, to.WaterCrestColor, t));
        _mat.SetColor("_HorizonColor", Color.Lerp(from.SkyHorizonColor, to.SkyHorizonColor, t));
        _mat.SetFloat("_WaveAmp", Mathf.Lerp(TotalWaveAmplitude(from), TotalWaveAmplitude(to), t));

        if (_skyMat != null)
        {
            _skyMat.SetColor("_TopColor", Color.Lerp(from.AtmosphereColor, to.AtmosphereColor, t));
            _skyMat.SetColor("_HorizonColor", Color.Lerp(from.SkyHorizonColor, to.SkyHorizonColor, t));
        }
    }

    void RandomizePhases()
    {
        for (int i = 0; i < _currentClimate.Harmonics.Length; i++)
            _currentClimate.Harmonics[i].InitialPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void BuildMesh()
    {
        _mesh = new Mesh { name = "Ocean" };
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = _mesh;

        int cols = _meshCols + 1, rows = _meshRows + 1;
        _vertices = new Vector3[cols * rows];
        var uvs = new Vector2[_vertices.Length];
        var tris = new int[_meshCols * _meshRows * 6];

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int i = r * cols + c;
            float x = (c / (float)_meshCols) * _meshWidth - _meshWidth * 0.5f;
            float z = (r / (float)_meshRows) * _meshDepth - _meshDepth * 0.5f;
            _vertices[i] = new Vector3(x, 0f, z);
            uvs[i] = new Vector2((float)c / _meshCols, (float)r / _meshRows);
        }

        int ti = 0;
        for (int r = 0; r < _meshRows; r++)
        for (int c = 0; c < _meshCols; c++)
        {
            int bl = r * cols + c, br = bl + 1, tl = bl + cols, tr = tl + 1;
            tris[ti++] = bl; tris[ti++] = tl; tris[ti++] = br;
            tris[ti++] = br; tris[ti++] = tl; tris[ti++] = tr;
        }

        _mesh.vertices = _vertices;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }

    void UpdateVertices()
    {
        float t = _sessionTime;
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i].y = GetHeightAt(_vertices[i].x, t);
        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
    }
}
