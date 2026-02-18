using UnityEngine;

public class LootBanner : MonoBehaviour
{
    public float lifetime = 4f;

    private float timer;
    private Renderer[] renderers;
    private LineRenderer lineRenderer;

    private void Start()
    {
        timer = lifetime;
        renderers = GetComponentsInChildren<Renderer>();
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Fade out in the last second
        if (timer < 1f)
        {
            float alpha = timer;
            foreach (var r in renderers)
            {
                if (r == lineRenderer) continue;
                var c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
            if (lineRenderer != null)
            {
                var startC = lineRenderer.startColor;
                var endC = lineRenderer.endColor;
                startC.a = alpha * 0.7f;
                endC.a = alpha * 0.7f;
                lineRenderer.startColor = startC;
                lineRenderer.endColor = endC;
            }
        }
    }
}
