using UnityEngine;
using UnityEngine.InputSystem;

public class WallPlacement : MonoBehaviour
{
    [SerializeField] private GameObject wallGhostPrefab;

    // Serialized so they survive domain reload (script recompilation in editor)
    [SerializeField, HideInInspector] private bool isPlacing;
    [SerializeField, HideInInspector] private GameObject ghostWall;

    private UnityEngine.Camera mainCam;
    private float ghostRotationY;
    private bool subscribedToBuildMode;
    private bool ghostIsRed; // tracks current ghost tint to avoid per-frame material writes
    private float defaultGhostScaleX = 1f;
    private float currentSnapScaleX = 1f; // scaleX from latest snap result

    public bool IsPlacing => isPlacing;

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;

        if (wallGhostPrefab == null && WallManager.Instance != null)
            wallGhostPrefab = WallManager.Instance.WallPrefab;

        if (wallGhostPrefab == null)
            Debug.LogError("[WallPlacement] wallGhostPrefab is null after Start! Ghost will not appear.");
    }

    private void OnEnable()
    {
        // After domain reload, if we had an orphaned ghost, clean it up
        if (ghostWall != null && !isPlacing)
        {
            Debug.Log("[WallPlacement] OnEnable: cleaning up orphaned ghost from domain reload.");
            Destroy(ghostWall);
            ghostWall = null;
        }

        // Cancel placement if build mode ends while placing
        if (BuildModeManager.Instance != null)
        {
            BuildModeManager.Instance.OnBuildModeEnded += HandleBuildModeEnded;
            subscribedToBuildMode = true;
        }
    }

    private void OnDisable()
    {
        if (BuildModeManager.Instance != null)
            BuildModeManager.Instance.OnBuildModeEnded -= HandleBuildModeEnded;
    }

    private void HandleBuildModeEnded()
    {
        if (isPlacing)
        {
            Debug.Log("[WallPlacement] Build mode ended — stopping placement.");
            StopPlacement();
        }
    }

    private void Update()
    {
        // Late-subscribe to BuildModeManager
        if (!subscribedToBuildMode && BuildModeManager.Instance != null)
        {
            BuildModeManager.Instance.OnBuildModeEnded += HandleBuildModeEnded;
            subscribedToBuildMode = true;
        }

        if (!isPlacing) return;
        if (Mouse.current == null) return;

        // Right-click or exit build mode key to exit
        bool exitPressed = Mouse.current.rightButton.wasPressedThisFrame;
        if (!exitPressed && InputBindingManager.Instance != null)
            exitPressed = InputBindingManager.Instance.WasPressedThisFrame(GameAction.ExitBuildMode);
        if (exitPressed)
        {
            Debug.Log("[WallPlacement] Exit build mode input — exiting build mode.");
            StopPlacement();
            if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode)
                BuildModeManager.Instance.ExitBuildMode();
            return;
        }

        // Rotate wall
        if (InputBindingManager.Instance != null)
        {
            if (InputBindingManager.Instance.WasPressedThisFrame(GameAction.RotateWallLeft))
                ghostRotationY -= 45f;
            if (InputBindingManager.Instance.WasPressedThisFrame(GameAction.RotateWallRight))
                ghostRotationY += 45f;
        }

        // Get mouse world position (free floating, no grid)
        Vector3 worldPos = GetMouseWorldPosition();
        Vector3 finalPos = worldPos;
        Quaternion finalRot = Quaternion.Euler(0, ghostRotationY, 0);

        // Use corner snap solver for wall-to-wall alignment
        currentSnapScaleX = defaultGhostScaleX;
        if (WallManager.Instance != null && ghostWall != null)
        {
            Vector3 ghostScale = ghostWall.transform.localScale;
            var snap = CornerSnapSolver.Solve(worldPos, ghostRotationY, ghostScale,
                                               WallManager.Instance.AllWalls);
            if (snap.didSnap)
            {
                finalPos = snap.position;
                finalRot = snap.rotation;
                if (snap.isRingClose)
                    currentSnapScaleX = snap.scaleX;
            }
        }

        // Update ghost
        if (ghostWall != null)
        {
            ghostWall.transform.position = new Vector3(finalPos.x, 1f, finalPos.z);
            ghostWall.transform.rotation = finalRot;
            // Stretch ghost for diagonal ring-close, reset otherwise
            var gs = ghostWall.transform.localScale;
            ghostWall.transform.localScale = new Vector3(currentSnapScaleX, gs.y, gs.z);

            // Refresh ghost's WallCorners so gizmos draw correctly
            var ghostCorners = ghostWall.GetComponent<WallCorners>();
            if (ghostCorners != null)
                ghostCorners.RefreshCorners();
        }
        else
        {
            Debug.LogError("[WallPlacement] ghostWall is null during placement! Ghost was destroyed externally.");
            isPlacing = false;
            return;
        }

        // Tint ghost red/green based on affordability
        UpdateGhostTint();

        // Left-click to purchase and place wall
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPurchaseAndPlace(new Vector3(finalPos.x, 1f, finalPos.z), finalRot, currentSnapScaleX);
        }
    }

    /// <summary>Called by BuildModeManager when entering build mode.</summary>
    public void StartBuildModeGhost()
    {
        // Clean up any existing ghost
        if (ghostWall != null)
        {
            Destroy(ghostWall);
            ghostWall = null;
        }

        isPlacing = true;
        ghostRotationY = 0f;
        Debug.Log("[WallPlacement] Build mode ghost started.");
        SpawnGhost();
    }

    private void SpawnGhost()
    {
        if (wallGhostPrefab == null)
        {
            if (WallManager.Instance != null)
                wallGhostPrefab = WallManager.Instance.WallPrefab;
        }

        if (wallGhostPrefab != null)
        {
            Vector3 initialPos = new Vector3(0f, 1f, -6f);
            ghostWall = Instantiate(wallGhostPrefab, initialPos, Quaternion.identity);
            // Mark WallCorners as ghost so it skips any collider creation
            var ghostCorners = ghostWall.GetComponent<WallCorners>();
            if (ghostCorners != null)
                ghostCorners.isGhost = true;
            // Disable Wall component and NavMeshObstacle on ghost so it doesn't affect gameplay
            var wallComp = ghostWall.GetComponent<Wall>();
            if (wallComp != null) wallComp.enabled = false;
            var obstacle = ghostWall.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
            // Disable colliders on ghost so it doesn't block raycasts or physics
            foreach (var col in ghostWall.GetComponentsInChildren<Collider>())
                col.enabled = false;
            // Tint ghost green so it's clearly visible as a placement preview
            Color ghostColor = new Color(0.3f, 0.9f, 0.3f, 1f);
            foreach (var rend in ghostWall.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i].SetColor("_BaseColor", ghostColor);
                rend.materials = mats;
            }
            defaultGhostScaleX = ghostWall.transform.localScale.x;
            currentSnapScaleX = defaultGhostScaleX;
            Debug.Log($"[WallPlacement] Ghost spawned at {initialPos}.");
        }
        else
        {
            Debug.LogError("[WallPlacement] wallGhostPrefab is null! No ghost preview.");
        }
    }

    private void TryPurchaseAndPlace(Vector3 position, Quaternion rotation, float scaleX = 1f)
    {
        if (BuildModeManager.Instance == null || !BuildModeManager.Instance.CanAffordWall())
        {
            Debug.Log("[WallPlacement] Cannot afford wall — skipping placement.");
            return;
        }

        int cost = BuildModeManager.Instance.WallCost;

        // Spend gold
        if (!GameManager.Instance.SpendTreasure(cost))
        {
            Debug.Log($"[WallPlacement] SpendTreasure failed for {cost}g.");
            return;
        }

        // Place the wall
        if (WallManager.Instance != null)
        {
            WallManager.Instance.PlaceWall(position, rotation, scaleX);
            Debug.Log($"[WallPlacement] Wall purchased ({cost}g) and placed at {position}, rotation={rotation.eulerAngles}, scaleX={scaleX:F3}.");
        }

        // Notify BuildModeManager (increments purchase count for cost scaling)
        BuildModeManager.Instance.NotifyWallPlaced();

        // Ghost stays active for next placement — no need to destroy/recreate
        // BuildModeManager.Update() will auto-exit if player can't afford more
    }

    private void UpdateGhostTint()
    {
        if (ghostWall == null) return;
        bool canAfford = BuildModeManager.Instance != null && BuildModeManager.Instance.CanAffordWall();
        bool shouldBeRed = !canAfford;
        if (shouldBeRed == ghostIsRed) return; // no change
        ghostIsRed = shouldBeRed;

        Color tint = shouldBeRed ? new Color(0.9f, 0.3f, 0.3f, 1f) : new Color(0.3f, 0.9f, 0.3f, 1f);
        foreach (var rend in ghostWall.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i].SetColor("_BaseColor", tint);
            rend.materials = mats;
        }
    }

    private void StopPlacement()
    {
        isPlacing = false;
        if (ghostWall != null)
        {
            Destroy(ghostWall);
            ghostWall = null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (mainCam == null) mainCam = UnityEngine.Camera.main;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}
