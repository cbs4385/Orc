using UnityEngine;

/// <summary>
/// Small splash effect spawned when a scorpio bolt hits a target or the ground.
/// Code-generated particles, no prefab needed. Auto-destroys after effect finishes.
/// </summary>
public class BoltImpactVFX : MonoBehaviour
{
    private static Material cachedMat;
    private static Texture2D cachedTex;

    /// <summary>
    /// Spawn an impact splash at the given position.
    /// If hitEnemy is true, uses a red splash; otherwise a dirt/dust splash.
    /// </summary>
    public static void Spawn(Vector3 position, bool hitEnemy)
    {
        var go = new GameObject("BoltImpactVFX");
        go.transform.position = position;
        var vfx = go.AddComponent<BoltImpactVFX>();
        vfx.Setup(hitEnemy);
    }

    private void Setup(bool hitEnemy)
    {
        CreateSplash(hitEnemy);
        CreateFlash(hitEnemy);

        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            ps.Play();

        Destroy(gameObject, 1.5f);
    }

    private static Texture2D GetCircleTexture()
    {
        if (cachedTex != null) return cachedTex;

        const int size = 16;
        cachedTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        cachedTex.filterMode = FilterMode.Bilinear;
        cachedTex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist / center);
                alpha *= alpha;
                byte a = (byte)(alpha * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        cachedTex.SetPixels32(pixels);
        cachedTex.Apply();
        return cachedTex;
    }

    private Material GetMaterial()
    {
        if (cachedMat != null) return cachedMat;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        cachedMat = new Material(shader);
        cachedMat.SetFloat("_Surface", 1);
        cachedMat.SetFloat("_Blend", 0);
        cachedMat.mainTexture = GetCircleTexture();
        cachedMat.renderQueue = 3000;
        cachedMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        cachedMat.EnableKeyword("_BLENDMODE_ALPHA");
        cachedMat.SetOverrideTag("RenderType", "Transparent");
        cachedMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        cachedMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        cachedMat.SetInt("_ZWrite", 0);
        return cachedMat;
    }

    private void CreateSplash(bool hitEnemy)
    {
        var go = new GameObject("Splash");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.gravityModifier = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        if (hitEnemy)
            main.startColor = new Color(0.8f, 0.1f, 0.1f, 1f); // Red blood splash
        else
            main.startColor = new Color(0.6f, 0.5f, 0.3f, 1f); // Brown dirt splash

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 12, 20)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        if (hitEnemy)
        {
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.2f, 0.1f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.05f, 0.05f), 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
        }
        else
        {
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.7f, 0.6f, 0.4f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.35f, 0.2f), 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
        }
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private void CreateFlash(bool hitEnemy)
    {
        var go = new GameObject("Flash");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.05f;
        main.loop = false;
        main.startLifetime = 0.15f;
        main.startSpeed = 0f;
        main.startSize = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1;

        if (hitEnemy)
            main.startColor = new Color(1f, 0.4f, 0.3f, 0.7f);
        else
            main.startColor = new Color(0.9f, 0.8f, 0.5f, 0.6f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 1)
        });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(hitEnemy ? new Color(1f, 0.2f, 0.1f) : new Color(0.7f, 0.6f, 0.3f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 1.5f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
