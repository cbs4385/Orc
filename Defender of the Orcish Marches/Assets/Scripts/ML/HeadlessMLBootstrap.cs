using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-configures Unity for headless ML training when launched with -batchmode.
/// Disables cameras, audio, UI, fog, and other rendering-dependent systems.
/// Accelerates time scale and reduces physics overhead for maximum training throughput.
/// Add this component to a GameObject in your training scene, or use the
/// HeadlessMLSetup editor script to configure automatically.
/// </summary>
public class HeadlessMLBootstrap : MonoBehaviour
{
    [Header("Headless Settings")]
    [SerializeField] private float headlessTimeScale = 10f;
    [SerializeField] private float headlessFixedDeltaTime = 0.02f; // 50 Hz physics at 10x speed
    [SerializeField] private bool forceHeadless; // For testing headless path in editor

    /// <summary>True when running in headless/batch mode.</summary>
    public static bool IsHeadless { get; private set; }

    private static HeadlessMLBootstrap instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        IsHeadless = forceHeadless || IsRunningHeadless();

        if (IsHeadless)
        {
            Debug.Log("[HeadlessMLBootstrap] Headless mode detected. Configuring for maximum training throughput.");
            ConfigureHeadlessMode();
        }
        else
        {
            Debug.Log("[HeadlessMLBootstrap] Not in headless mode — skipping headless configuration.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsHeadless)
        {
            // Re-strip rendering components after scene load
            StartCoroutine(StripAfterFrame());
        }
    }

    private System.Collections.IEnumerator StripAfterFrame()
    {
        yield return null;
        yield return null;
        StripRenderingComponents();
    }

    /// <summary>Detects if Unity was launched with -batchmode or -nographics.</summary>
    private static bool IsRunningHeadless()
    {
        // SystemInfo.graphicsDeviceType is Null when -nographics is used
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            return true;

        // Check command-line args for -batchmode
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.ToLowerInvariant() == "-batchmode" || arg.ToLowerInvariant() == "-nographics")
                return true;
        }

        return false;
    }

    private void ConfigureHeadlessMode()
    {
        // --- Time scale ---
        // Override serialized values — high time scale starves the ML-Agents communicator
        // ML-Agents will set its own time scale via --time-scale if provided
        // Don't fight it; just let Academy manage time scale via its own channel
        Time.timeScale = 1f; // Let ML-Agents/Academy manage this
        Time.fixedDeltaTime = 0.02f; // Default 50Hz physics
        Debug.Log($"[HeadlessMLBootstrap] TimeScale=1 (Academy-managed), FixedDeltaTime=0.02");

        // --- Quality & rendering ---
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1; // Uncapped
        Application.runInBackground = true;

        // Disable all quality overhead
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = false;
        QualitySettings.skinWeights = SkinWeights.OneBone;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.particleRaycastBudget = 0;

        // Disable fog
        RenderSettings.fog = false;

        // --- Audio ---
        AudioListener.volume = 0f;
        AudioListener.pause = true;

        // --- Logging: disable stack traces to reduce log I/O ---
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

        // --- Strip rendering from current scene ---
        StripRenderingComponents();

        Debug.Log("[HeadlessMLBootstrap] Headless configuration complete.");
    }

    /// <summary>
    /// Disables cameras, audio listeners, canvas, UI, lights, particle systems,
    /// and other rendering-only components that waste CPU in headless mode.
    /// Preserves physics (colliders, rigidbodies, NavMesh) and game logic.
    /// </summary>
    private void StripRenderingComponents()
    {
        int disabledCount = 0;

        // Disable all cameras
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            cam.enabled = false;
            disabledCount++;
        }

        // Disable all audio listeners
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            listener.enabled = false;
            disabledCount++;
        }

        // Disable all audio sources
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            source.enabled = false;
            disabledCount++;
        }

        // Disable all canvases (UI rendering)
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            canvas.enabled = false;
            disabledCount++;
        }

        // Disable all lights (DayNightCycle will harmlessly write to disabled lights)
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            light.enabled = false;
            disabledCount++;
        }

        // Disable particle systems
        foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            disabledCount++;
        }

        // Disable all renderers (MeshRenderer, SkinnedMeshRenderer, SpriteRenderer, etc.)
        // This is safe because ML observations use transform.position, not visual state
        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            renderer.enabled = false;
            disabledCount++;
        }

        // Disable camera-dependent scripts
        DisableComponentsByType<CameraController>();
        DisableComponentsByType<NightmareCamera>();

        // Disable fog scripts
        DisableScriptByName("RadialFog");
        DisableScriptByName("NightmareFog");

        // Disable UI-only scripts
        DisableScriptByName("FloatingDamageNumber");
        DisableScriptByName("TooltipSystem");
        DisableScriptByName("MobileControlsOverlay");
        DisableScriptByName("WallPlacement"); // Human wall placement uses Camera.main
        DisableScriptByName("BallistaAimAssist"); // Visual aim line

        // Disable SoundManager to prevent "Can not play a disabled audio source" spam
        DisableScriptByName("SoundManager");

        // Disable other non-essential scripts that generate log spam
        DisableScriptByName("TorchManager");
        DisableScriptByName("FriendlyIndicator");
        DisableScriptByName("BurstDamageVFX");
        DisableScriptByName("BoltImpactVFX");

        Debug.Log($"[HeadlessMLBootstrap] Stripped {disabledCount} rendering components from scene.");
    }

    private void DisableComponentsByType<T>() where T : MonoBehaviour
    {
        foreach (var comp in FindObjectsByType<T>(FindObjectsSortMode.None))
            comp.enabled = false;
    }

    private void DisableScriptByName(string typeName)
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb != null && mb.GetType().Name == typeName)
                mb.enabled = false;
        }
    }
}
