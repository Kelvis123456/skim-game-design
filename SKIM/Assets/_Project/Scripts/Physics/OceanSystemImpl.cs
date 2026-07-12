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
        // Try shaders in priority order; first non-null wins
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("[Ocean] No compatible shader found. Is URP configured in Project Settings > Graphics?");
            return;
        }

        var teal = new Color(0f, 0.77f, 0.8f);
        var mat = new Material(shader);

        // URP shaders use _BaseColor; built-in use _Color
        if (shader.name.StartsWith("Universal Render Pipeline"))
            mat.SetColor("_BaseColor", teal);
        else
            mat.color = teal;

        GetComponent<MeshRenderer>().material = mat;
        _mat = mat;
        Debug.Log($"[Ocean] Using shader: {shader.name}");
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

    IEnumerator TransitionTo(ClimateData target, float duration)
    {
        var startWater = _mat?.GetColor("_WaterColor") ?? Color.cyan;
        var startSky = _mat?.GetColor("_SkyColor") ?? Color.blue;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _mat?.SetColor("_WaterColor", Color.Lerp(startWater, target.WaterSurfaceColor, t));
            _mat?.SetColor("_SkyColor", Color.Lerp(startSky, target.SkyHorizonColor, t));
            yield return null;
        }

        _currentClimate = target;
        RandomizePhases();
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
