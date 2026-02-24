using UnityEngine;

/// <summary>
/// Adds a flat colored circle beneath a friendly unit to distinguish it from enemies at a distance.
/// Call Create() to attach to any GameObject.
/// </summary>
public static class FriendlyIndicator
{
    private static Mesh quadMesh;
    private static Material indicatorMaterial;

    /// <summary>
    /// Creates a flat circle indicator beneath the given transform.
    /// </summary>
    public static GameObject Create(Transform parent, Color color, float radius = 0.35f)
    {
        var go = new GameObject("FriendlyIndicator");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetQuadMesh();

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = CreateMaterial(color);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return go;
    }

    private static Mesh GetQuadMesh()
    {
        if (quadMesh != null) return quadMesh;

        // Generate a circle mesh (16 segments)
        const int segments = 16;
        var vertices = new Vector3[segments + 1];
        var uv = new Vector2[segments + 1];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            float x = Mathf.Cos(angle) * 0.5f;
            float y = Mathf.Sin(angle) * 0.5f;
            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2(x + 0.5f, y + 0.5f);

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }

        quadMesh = new Mesh();
        quadMesh.name = "FriendlyIndicatorCircle";
        quadMesh.vertices = vertices;
        quadMesh.uv = uv;
        quadMesh.triangles = triangles;
        quadMesh.RecalculateNormals();

        return quadMesh;
    }

    private static Material CreateMaterial(Color color)
    {
        // Try URP unlit first, then built-in fallbacks.
        // Sprites/Default is always included in builds and supports vertex colors + alpha.
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[FriendlyIndicator] No suitable shader found. Indicator will be invisible.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var mat = new Material(shader);
        color.a = 0.5f;
        mat.color = color;

        // Enable transparency (URP properties — harmless no-ops on other shaders)
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }
}
