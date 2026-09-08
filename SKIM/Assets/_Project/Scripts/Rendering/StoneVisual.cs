using System.Collections;
using UnityEngine;

// Syncs the Stone GameObject position/rotation to the physics state each frame,
// and gives each StoneData its own silhouette and palette per the art direction.
[DefaultExecutionOrder(10)]
public class StoneVisual : MonoBehaviour
{
    const float SQUISH_DURATION = 0.1f;
    const float SQUISH_AMOUNT = 0.2f;

    IStoneSimulator _sim;
    Renderer _rend;
    MeshFilter _filter;
    Mesh _sphereMesh;
    StoneData _appliedStone;
    Vector3 _baseScale = Vector3.one;
    float _squishTimer = -1f;

    void Start()
    {
        _rend = GetComponent<Renderer>();
        _filter = GetComponent<MeshFilter>();
        _sphereMesh = _filter != null ? _filter.sharedMesh : null;
        StartCoroutine(WaitForSim());
    }

    IEnumerator WaitForSim()
    {
        while (!ServiceLocator.TryGet<IStoneSimulator>(out _sim))
            yield return null;

        _sim.OnImpact += _ => _squishTimer = 0f;
    }

    void LateUpdate()
    {
        if (_sim == null) return;
        var state = _sim.CurrentState;

        bool visible = state.Phase != StoneState.StonePhase.Sunk;
        if (_rend) _rend.enabled = visible;
        if (!visible) return;

        ApplyEquippedStone();

        transform.position = state.Position;
        // Spin around Z axis proportional to angular velocity
        transform.Rotate(0f, 0f, state.AngularVelocity * Time.deltaTime * 60f, Space.Self);
        // Tumble around X proportional to how fast it's travelling
        transform.Rotate(new Vector2(state.Velocity.x, state.Velocity.z).magnitude * Time.deltaTime * 90f,
                         0f, 0f, Space.Self);

        UpdateSquish();
    }

    // Squish 20% on the Y axis for 0.1s on every skip, then spring back.
    void UpdateSquish()
    {
        if (_squishTimer < 0f) { transform.localScale = _baseScale; return; }

        _squishTimer += Time.deltaTime;
        if (_squishTimer >= SQUISH_DURATION)
        {
            _squishTimer = -1f;
            transform.localScale = _baseScale;
            return;
        }

        float t = _squishTimer / SQUISH_DURATION;
        float squash = 1f - Mathf.Sin(t * Mathf.PI) * SQUISH_AMOUNT;
        transform.localScale = new Vector3(_baseScale.x / squash, _baseScale.y * squash, _baseScale.z / squash);
    }

    void ApplyEquippedStone()
    {
        if (!ServiceLocator.TryGet<IProgressionSystem>(out var prog)) return;
        var stone = prog.SelectedStone;
        if (stone == null || stone == _appliedStone) return;

        _appliedStone = stone;
        float r = stone.Radius;

        switch (stone.StoneName)
        {
            case "Esquisto":
                SetMesh(StoneMeshFactory.BuildHexPrism(1f, 0.3f), Vector3.one * (r * 2.2f));
                SetColor(new Color(0.290f, 0.416f, 0.353f), 1f); // #4A6A5A pizarra
                break;

            case "Basalto":
                SetMesh(_sphereMesh, Vector3.one * (r * 2f));
                SetColor(new Color(0.118f, 0.157f, 0.188f), 1f); // #1E2830 negro azulado
                break;

            case "Cuarzo":
                SetMesh(StoneMeshFactory.BuildCrystal(1f, stone.StoneName.GetHashCode()), Vector3.one * (r * 2f));
                SetColor(new Color(0.753f, 0.847f, 0.941f), 0.75f); // #C0D8F0 cristal
                break;

            default: // Guijarro — flattened sphere, 60% on Y
                SetMesh(_sphereMesh, new Vector3(r * 2f, r * 1.2f, r * 2f));
                SetColor(new Color(0.722f, 0.773f, 0.816f), 1f); // #B8C5D0 granito de río
                break;
        }
    }

    void SetMesh(Mesh mesh, Vector3 scale)
    {
        if (_filter != null && mesh != null) _filter.mesh = mesh;
        _baseScale = scale;
        transform.localScale = scale;
    }

    void SetColor(Color color, float alpha)
    {
        if (_rend == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader == null) return;

        var mat = new Material(shader);
        var c = new Color(color.r, color.g, color.b, alpha);

        if (shader.name.StartsWith("Universal"))
        {
            mat.SetColor("_BaseColor", c);
            if (alpha < 1f)
            {
                mat.SetFloat("_Surface", 1f); // transparent
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
        else mat.color = c;

        _rend.material = mat;
    }
}
