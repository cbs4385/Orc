using UnityEngine;

/// <summary>
/// Self-contained burst damage explosion effect.
/// Spawns an expanding ring of fiery particles + central flash.
/// Auto-destroys when finished. No prefab or asset references needed.
/// </summary>
public class BurstDamageVFX : MonoBehaviour
{
    private static Material cachedParticleMat;

    public static void Spawn(Vector3 position, float radius)
    {
        var go = new GameObject("BurstDamageVFX");
        go.transform.position = position;
        var vfx = go.AddComponent<BurstDamageVFX>();
        vfx.Setup(radius);
    }

    private void Setup(float radius)
    {
        CreateExplosionBurst(radius);
        CreateShockwaveRing(radius);
        CreateFlash();

        // Auto-destroy after effects finish
        Destroy(gameObject, 2f);
    }

    private Material GetParticleMaterial()
    {
        if (cachedParticleMat != null) return cachedParticleMat;

        // Use the default particle shader for URP compatibility
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        cachedParticleMat = new Material(shader);
        cachedParticleMat.SetFloat("_Surface", 1); // Transparent
        cachedParticleMat.SetFloat("_Blend", 0);   // Alpha blend
        cachedParticleMat.renderQueue = 3000;
        return cachedParticleMat;
    }

    /// <summary>
    /// Main burst: fiery particles exploding outward in a sphere.
    /// </summary>
    private void CreateExplosionBurst(float radius)
    {
        var go = new GameObject("ExplosionBurst");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2f, radius * 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(1f, 0.6f, 0.1f, 1f); // Orange
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 40, 60)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Color over lifetime: orange -> red -> fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.4f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0.0f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size over lifetime: shrink slightly
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    /// <summary>
    /// Expanding ground ring showing the AoE radius.
    /// </summary>
    private void CreateShockwaveRing(float radius)
    {
        var go = new GameObject("ShockwaveRing");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = radius * 3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(1f, 0.7f, 0.2f, 0.9f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 30, 40)
        });

        // Circle shape on ground plane (XZ)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(90, 0, 0); // Flat on ground

        // Fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0.0f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.4f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        // Velocity over lifetime: slow down as ring expands
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.1f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    /// <summary>
    /// Brief bright flash at the impact center.
    /// </summary>
    private void CreateFlash()
    {
        var go = new GameObject("Flash");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.05f;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 0f;
        main.startSize = 1.5f;
        main.startColor = new Color(1f, 0.9f, 0.5f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 1)
        });

        var shape = ps.shape;
        shape.enabled = false;

        // Rapid fade out + shrink
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
