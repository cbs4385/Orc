using UnityEngine;

/// <summary>
/// Replaces Unity's built-in distance fog with concentric cylinder walls centered on the fortress.
/// Each ring has increasing opacity, creating a gradual volumetric fog effect that works at all heights.
/// Camera background is set to match the fog color so the cylinder edge blends into the sky.
/// </summary>
public class RadialFog : MonoBehaviour
{
    [SerializeField] private float fogStartRadius = 20f;
    [SerializeField] private float fogEndRadius = 55f;
    [SerializeField] private float cylinderHeight = 40f;
    [SerializeField] private float heightOffset = 5f;
    [SerializeField] private float maxAlphaPerRing = 0.15f;

    private static readonly Color DAY_FOG_COLOR = new Color(0.68f, 0.70f, 0.73f, 1f);
    private static readonly Color NIGHT_FOG_COLOR = new Color(0.06f, 0.07f, 0.14f, 1f);

    private Material fogMaterial;
    private GameObject fogCylinder;
    private Mesh fogMesh;
    private Color currentFogColor;
    private Camera mainCam;

    private const int SEGMENTS = 64;
    private const int RINGS = 16;

    private void Start()
    {
        RenderSettings.fog = false;

        mainCam = Camera.main;

        currentFogColor = DAY_FOG_COLOR;
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
            currentFogColor = NIGHT_FOG_COLOR;

        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = currentFogColor;
        }

        CreateFogCylinder();
        Debug.Log($"[RadialFog] Initialized cylinder fog. startRadius={fogStartRadius}, endRadius={fogEndRadius}, height={cylinderHeight}, rings={RINGS}");
    }

    private void CreateFogCylinder()
    {
        fogCylinder = new GameObject("CylinderFog");
        fogCylinder.transform.SetParent(transform);
        fogCylinder.transform.position = GameManager.FortressCenter + Vector3.up * heightOffset;

        var meshFilter = fogCylinder.AddComponent<MeshFilter>();
        var meshRenderer = fogCylinder.AddComponent<MeshRenderer>();

        fogMesh = CreateCylinderMesh();
        meshFilter.mesh = fogMesh;

        var shader = Shader.Find("Custom/RadialFog");
        if (shader == null)
        {
            Debug.LogError("[RadialFog] Custom/RadialFog shader not found! Cannot create cylinder fog.");
            return;
        }

        fogMaterial = new Material(shader);
        fogMaterial.SetColor("_Color", currentFogColor);
        fogMaterial.renderQueue = 3100;

        meshRenderer.material = fogMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void Update()
    {
        if (fogMesh == null || fogMaterial == null) return;

        if (RenderSettings.fog) RenderSettings.fog = false;

        if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode)
        {
            if (fogCylinder.activeSelf) fogCylinder.SetActive(false);
            return;
        }
        else if (!fogCylinder.activeSelf)
        {
            fogCylinder.SetActive(true);
        }

        if (DayNightCycle.Instance != null)
        {
            Color targetColor = DayNightCycle.Instance.IsNight ? NIGHT_FOG_COLOR : DAY_FOG_COLOR;
            Color newColor = Color.Lerp(currentFogColor, targetColor, Time.deltaTime * 2f);

            if (!ColorApproxEqual(newColor, currentFogColor))
            {
                currentFogColor = newColor;
                fogMaterial.SetColor("_Color", currentFogColor);

                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                    mainCam.backgroundColor = currentFogColor;
            }
        }
    }

    private bool ColorApproxEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.001f &&
               Mathf.Abs(a.g - b.g) < 0.001f &&
               Mathf.Abs(a.b - b.b) < 0.001f;
    }

    private Mesh CreateCylinderMesh()
    {
        // Each ring is a cylinder wall at a specific radius with top and bottom vertices.
        // Alpha is baked into UV.x so no per-frame vertex color rebuild is needed.
        int vertCount = RINGS * SEGMENTS * 2;
        var vertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        float halfHeight = cylinderHeight * 0.5f;

        for (int ring = 0; ring < RINGS; ring++)
        {
            float t = (float)(ring + 1) / RINGS;
            float radius = Mathf.Lerp(fogStartRadius, fogEndRadius, t);
            float alpha = Smoothstep(0f, 1f, t) * maxAlphaPerRing;

            for (int seg = 0; seg < SEGMENTS; seg++)
            {
                float angle = (float)seg / SEGMENTS * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                int baseIdx = (ring * SEGMENTS + seg) * 2;
                vertices[baseIdx] = new Vector3(x, -halfHeight, z);
                vertices[baseIdx + 1] = new Vector3(x, halfHeight, z);
                uvs[baseIdx] = new Vector2(alpha, 0f);
                uvs[baseIdx + 1] = new Vector2(alpha, 1f);
            }
        }

        // Each ring has SEGMENTS quads (2 tris each)
        int triCount = RINGS * SEGMENTS * 2;
        var triangles = new int[triCount * 3];
        int ti = 0;

        for (int ring = 0; ring < RINGS; ring++)
        {
            for (int seg = 0; seg < SEGMENTS; seg++)
            {
                int next = (seg + 1) % SEGMENTS;
                int b0 = (ring * SEGMENTS + seg) * 2;
                int t0 = b0 + 1;
                int b1 = (ring * SEGMENTS + next) * 2;
                int t1 = b1 + 1;

                triangles[ti++] = b0;
                triangles[ti++] = t0;
                triangles[ti++] = b1;

                triangles[ti++] = t0;
                triangles[ti++] = t1;
                triangles[ti++] = b1;
            }
        }

        var mesh = new Mesh();
        mesh.name = "CylinderFog";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private void OnDisable()
    {
        if (fogCylinder != null) fogCylinder.SetActive(false);
    }

    private void OnDestroy()
    {
        if (fogCylinder != null) Destroy(fogCylinder);
        if (fogMaterial != null) Destroy(fogMaterial);
        if (fogMesh != null) Destroy(fogMesh);
    }
}
