using UnityEngine;

/// <summary>
/// Distance fog for Nightmare FPS mode. Creates an obscuring mist that:
/// - Keeps full visibility within scorpio range + 1 unit
/// - Gradually thickens beyond that to full opacity
/// - Hides map boundaries and enemy spawn-in
/// - Shifts color between day (gray) and night (dark blue)
/// Fog is disabled during build mode (overhead camera) so the player can see the full map.
/// </summary>
public class NightmareFog : MonoBehaviour
{
    private const float CLEAR_RADIUS_PADDING = 1.0f;
    private const float FOG_FADE_DISTANCE = 14.0f;

    // Day: pale gray mist. Night: dark blue-gray murk.
    private static readonly Color DAY_FOG_COLOR = new Color(0.68f, 0.70f, 0.73f, 1f);
    private static readonly Color NIGHT_FOG_COLOR = new Color(0.06f, 0.07f, 0.14f, 1f);

    private float fogStart;
    private float fogEnd;
    private bool fogActive;

    // Cache to avoid per-frame Camera.main lookup
    private Camera cachedCam;

    private void Start()
    {
        // Determine clear radius from ballista range
        float maxRange = 30f;
        if (BallistaManager.Instance != null && BallistaManager.Instance.ActiveBallista != null)
            maxRange = BallistaManager.Instance.ActiveBallista.MaxRange;

        fogStart = maxRange + CLEAR_RADIUS_PADDING;
        fogEnd = fogStart + FOG_FADE_DISTANCE;

        EnableFog();
        Debug.Log($"[NightmareFog] Initialized. clearRadius={fogStart}, fogEnd={fogEnd}, ballistaRange={maxRange}");
    }

    private void Update()
    {
        // Disable fog during build mode (overhead ortho view)
        bool inBuildMode = BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode;
        if (inBuildMode && fogActive)
        {
            DisableFog();
            // Restore ortho camera far clip
            var orthoCam = Camera.main;
            if (orthoCam != null && orthoCam.orthographic)
                orthoCam.farClipPlane = 200f;
        }
        else if (!inBuildMode && !fogActive)
        {
            EnableFog();
        }

        if (!fogActive) return;

        // Smoothly transition fog color between day and night
        if (DayNightCycle.Instance != null)
        {
            Color targetColor = DayNightCycle.Instance.IsNight ? NIGHT_FOG_COLOR : DAY_FOG_COLOR;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, Time.deltaTime * 2f);
        }

        // Keep FPS camera far clip tight to fog boundary
        if (cachedCam == null || !cachedCam.enabled)
            cachedCam = Camera.main;

        if (cachedCam != null && !cachedCam.orthographic)
        {
            cachedCam.farClipPlane = fogEnd + 5f;
            cachedCam.clearFlags = CameraClearFlags.SolidColor;
            cachedCam.backgroundColor = RenderSettings.fogColor;
        }
    }

    private void EnableFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        Color initialColor = DAY_FOG_COLOR;
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
            initialColor = NIGHT_FOG_COLOR;
        RenderSettings.fogColor = initialColor;

        fogActive = true;
        Debug.Log($"[NightmareFog] Fog enabled. start={fogStart}, end={fogEnd}");
    }

    private void DisableFog()
    {
        RenderSettings.fog = false;
        fogActive = false;

        // Restore FPS camera settings if it's still around
        if (cachedCam != null && !cachedCam.orthographic)
        {
            cachedCam.farClipPlane = 200f;
            cachedCam.clearFlags = CameraClearFlags.Skybox;
        }
        cachedCam = null;

        Debug.Log("[NightmareFog] Fog disabled (build mode).");
    }

    private void OnDisable()
    {
        if (fogActive) DisableFog();
    }

    private void OnDestroy()
    {
        RenderSettings.fog = false;
    }
}
