using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person camera controller for Nightmare difficulty.
/// Attaches to the active ballista's firePoint and handles pitch via mouse delta.
/// Yaw is handled by Ballista.cs rotating its own transform (camera is a child).
/// </summary>
public class NightmareCamera : MonoBehaviour
{
    public static NightmareCamera Instance { get; private set; }

    [Header("FPS Settings")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 30f;
    [SerializeField] private float fov = 70f;

    private UnityEngine.Camera fpsCamera;
    private GameObject fpsCameraObj;
    private UnityEngine.Camera orthoCamera;
    private float pitch;
    private bool inBuildView;
    private bool subscribedToBuildMode;

    /// <summary>Current pitch angle in degrees (read by Ballista.cs for fire direction).</summary>
    public float Pitch => pitch;

    /// <summary>Check if current difficulty is Nightmare.</summary>
    public static bool IsNightmareMode => GameSettings.CurrentDifficulty == Difficulty.Nightmare;

    private void Awake()
    {
        if (!IsNightmareMode)
        {
            Debug.Log("[NightmareCamera] Not Nightmare difficulty — self-disabling.");
            enabled = false;
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[NightmareCamera] Instance registered.");
    }

    private void OnEnable()
    {
        if (Instance == null && IsNightmareMode)
        {
            Instance = this;
            Debug.Log("[NightmareCamera] Instance re-registered in OnEnable.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Find and store the existing orthographic camera
        orthoCamera = UnityEngine.Camera.main;
        if (orthoCamera != null)
        {
            Debug.Log($"[NightmareCamera] Found ortho camera: {orthoCamera.name}");
        }

        // Create FPS camera as a child GameObject
        fpsCameraObj = new GameObject("FPS Camera");
        fpsCameraObj.transform.SetParent(transform, false);
        fpsCamera = fpsCameraObj.AddComponent<UnityEngine.Camera>();
        fpsCamera.fieldOfView = fov;
        fpsCamera.nearClipPlane = 0.1f;
        fpsCamera.farClipPlane = 200f;
        fpsCamera.clearFlags = CameraClearFlags.Skybox;
        // Add audio listener since we'll disable the ortho camera's
        fpsCameraObj.AddComponent<AudioListener>();

        // Disable ortho camera and enable FPS camera
        if (orthoCamera != null)
        {
            // Remove audio listener from ortho camera to avoid duplicate
            var orthoListener = orthoCamera.GetComponent<AudioListener>();
            if (orthoListener != null) orthoListener.enabled = false;

            orthoCamera.tag = "Untagged";
            orthoCamera.enabled = false;
        }
        fpsCameraObj.tag = "MainCamera";

        // Attach to active ballista
        if (BallistaManager.Instance != null && BallistaManager.Instance.ActiveBallista != null)
        {
            AttachToBallista(BallistaManager.Instance.ActiveBallista);
        }

        // Subscribe to events
        if (BallistaManager.Instance != null)
            BallistaManager.Instance.OnActiveBallistaChanged += AttachToBallista;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPauseChanged += HandlePauseChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }
        SubscribeToBuildMode();

        // Atmospheric fog — obscures map boundaries, enemies loom from the mist
        if (gameObject.GetComponent<NightmareFog>() == null)
            gameObject.AddComponent<NightmareFog>();

        // Lock cursor for FPS gameplay
        LockCursor();
        Debug.Log("[NightmareCamera] FPS camera initialized and active.");
    }

    private void OnDestroy()
    {
        if (BallistaManager.Instance != null)
            BallistaManager.Instance.OnActiveBallistaChanged -= AttachToBallista;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPauseChanged -= HandlePauseChanged;
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
        if (BuildModeManager.Instance != null)
        {
            BuildModeManager.Instance.OnBuildModeStarted -= EnterBuildView;
            BuildModeManager.Instance.OnBuildModeEnded -= ExitBuildView;
        }
    }

    private void SubscribeToBuildMode()
    {
        if (subscribedToBuildMode) return;
        if (BuildModeManager.Instance == null) return;
        BuildModeManager.Instance.OnBuildModeStarted += EnterBuildView;
        BuildModeManager.Instance.OnBuildModeEnded += ExitBuildView;
        subscribedToBuildMode = true;
        Debug.Log("[NightmareCamera] Subscribed to BuildModeManager events.");
    }

    private void Update()
    {
        // Late-subscribe to BuildModeManager (it may init after us)
        if (!subscribedToBuildMode) SubscribeToBuildMode();

        if (inBuildView) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Mouse.current == null) return;

        // Read mouse delta Y for pitch
        Vector2 delta = Mouse.current.delta.ReadValue();
        pitch -= delta.y * sensitivity * 0.1f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Pitch is applied by Ballista.RotateFPS() on the ballista root transform.
        // Camera inherits pitch from parent chain (Ballista → NightmareCamera → FPS Camera),
        // so no local rotation needed here.
    }

    /// <summary>Reparent camera to a ballista's firePoint.</summary>
    public void AttachToBallista(Ballista ballista)
    {
        if (ballista == null) return;

        // Find firePoint (serialized field on Ballista — look for child named "FirePoint" as fallback)
        Transform firePoint = ballista.transform.Find("FirePoint");
        if (firePoint == null) firePoint = ballista.transform;

        // Parent our root to the ballista so yaw rotation is inherited
        transform.SetParent(ballista.transform, false);
        // Offset: above and behind the barrel (raised for Nightmare tower height boost)
        transform.localPosition = new Vector3(0f, 0.5f, -0.3f);
        transform.localRotation = Quaternion.identity;

        pitch = 0f;

        Debug.Log($"[NightmareCamera] Attached to ballista: {ballista.name} at {ballista.transform.position}");
    }

    /// <summary>Switch to ortho camera for build mode wall placement.</summary>
    public void EnterBuildView()
    {
        if (inBuildView) return;
        inBuildView = true;

        // Enable ortho camera, disable FPS camera
        if (fpsCamera != null) fpsCamera.enabled = false;
        if (orthoCamera != null)
        {
            orthoCamera.enabled = true;
            orthoCamera.tag = "MainCamera";
            var orthoListener = orthoCamera.GetComponent<AudioListener>();
            if (orthoListener != null) orthoListener.enabled = true;
        }
        if (fpsCameraObj != null)
        {
            fpsCameraObj.tag = "Untagged";
            var fpsListener = fpsCameraObj.GetComponent<AudioListener>();
            if (fpsListener != null) fpsListener.enabled = false;
        }

        UnlockCursor();
        Debug.Log("[NightmareCamera] Entered build view — ortho camera active.");
    }

    /// <summary>Return to FPS camera from build mode.</summary>
    public void ExitBuildView()
    {
        if (!inBuildView) return;
        inBuildView = false;

        // Enable FPS camera, disable ortho camera
        if (orthoCamera != null)
        {
            orthoCamera.tag = "Untagged";
            orthoCamera.enabled = false;
            var orthoListener = orthoCamera.GetComponent<AudioListener>();
            if (orthoListener != null) orthoListener.enabled = false;
        }
        if (fpsCamera != null) fpsCamera.enabled = true;
        if (fpsCameraObj != null)
        {
            fpsCameraObj.tag = "MainCamera";
            var fpsListener = fpsCameraObj.GetComponent<AudioListener>();
            if (fpsListener != null) fpsListener.enabled = true;
        }

        LockCursor();
        Debug.Log("[NightmareCamera] Exited build view — FPS camera active.");
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (isPaused)
            UnlockCursor();
        else if (!inBuildView)
            LockCursor();
    }

    private void HandleGameOver()
    {
        UnlockCursor();
        Debug.Log("[NightmareCamera] Game over — cursor unlocked permanently.");
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
