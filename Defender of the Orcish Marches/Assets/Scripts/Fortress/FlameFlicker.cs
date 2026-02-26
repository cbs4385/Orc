using UnityEngine;

/// <summary>
/// Animates torch flame using layered sine waves for organic flickering.
/// Oscillates the point light intensity and flame mesh scale.
/// Each instance gets random phase offsets so torches don't flicker in sync.
/// </summary>
public class FlameFlicker : MonoBehaviour
{
    private Light flickerLight;
    private float baseIntensity;
    private float phase1, phase2, phase3;

    private void Start()
    {
        flickerLight = GetComponentInChildren<Light>();
        if (flickerLight != null)
            baseIntensity = flickerLight.intensity;

        // Random phase offsets per torch for visual variety
        phase1 = Random.Range(0f, Mathf.PI * 2f);
        phase2 = Random.Range(0f, Mathf.PI * 2f);
        phase3 = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (flickerLight == null) return;

        float t = Time.time;

        // Layered sine waves: slow drift + medium flicker + fast shimmer
        float flicker = 1f
            + 0.10f * Mathf.Sin(t * 5.1f + phase1)
            + 0.06f * Mathf.Sin(t * 11.7f + phase2)
            + 0.04f * Mathf.Sin(t * 23.3f + phase3);

        flickerLight.intensity = baseIntensity * flicker;
    }
}
