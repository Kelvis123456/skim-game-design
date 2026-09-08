using UnityEngine;

// Builds the four stone silhouettes from code. The art direction is explicitly
// geometric — no imported models, no textures — so each shape is generated here.
public static class StoneMeshFactory
{
    // Esquisto: flattened hexagonal slab, thickness 30% of the radius.
    public static Mesh BuildHexPrism(float radius, float thickness)
    {
        var mesh = new Mesh { name = "SkimHexPrism" };
        const int sides = 6;
        float half = thickness * 0.5f;

        var verts = new Vector3[sides * 2 + 2];
        verts[0] = new Vector3(0f, half, 0f);
        verts[1] = new Vector3(0f, -half, 0f);

        for (int i = 0; i < sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            verts[2 + i] = new Vector3(x, half, z);
            verts[2 + sides + i] = new Vector3(x, -half, z);
        }

        var tris = new int[sides * 12];
        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int topA = 2 + i, topB = 2 + next;
            int botA = 2 + sides + i, botB = 2 + sides + next;

            tris[t++] = 0; tris[t++] = topB; tris[t++] = topA;
            tris[t++] = 1; tris[t++] = botA; tris[t++] = botB;
            tris[t++] = topA; tris[t++] = topB; tris[t++] = botA;
            tris[t++] = topB; tris[t++] = botB; tris[t++] = botA;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    // Cuarzo: rhomboid crystal — an octahedron whose vertices are jittered per seed,
    // so no two quartz stones share the same silhouette.
    public static Mesh BuildCrystal(float radius, int seed)
    {
        var mesh = new Mesh { name = "SkimCrystal" };
        var rng = new System.Random(seed);
        float Jitter() => 1f + ((float)rng.NextDouble() - 0.5f) * 0.5f;

        var verts = new[]
        {
            new Vector3(0f, radius * 1.35f * Jitter(), 0f),
            new Vector3(0f, -radius * 1.05f * Jitter(), 0f),
            new Vector3(radius * Jitter(), 0f, 0f),
            new Vector3(0f, 0f, radius * Jitter()),
            new Vector3(-radius * Jitter(), 0f, 0f),
            new Vector3(0f, 0f, -radius * Jitter()),
        };

        var tris = new[]
        {
            0, 2, 3,  0, 3, 4,  0, 4, 5,  0, 5, 2,
            1, 3, 2,  1, 4, 3,  1, 5, 4,  1, 2, 5,
        };

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }
}
