using UnityEngine;
using UnityEngine.InputSystem;

public class WallPlacement : MonoBehaviour
{
    [SerializeField] private int wallCost = 20;
    [SerializeField] private GameObject wallGhostPrefab;

    // Serialized so they survive domain reload (script recompilation in editor)
    [SerializeField, HideInInspector] private bool isPlacing;
    [SerializeField, HideInInspector] private GameObject ghostWall;

    private UnityEngine.Camera mainCam;
    private float ghostRotationY;
    private bool skipNextFrame;

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
    }

    private void Update()
    {
        if (!isPlacing) return;
        if (Mouse.current == null) return;

        // Skip the frame when StartPlacement was called (button click would also register as left-click)
        if (skipNextFrame) { skipNextFrame = false; return; }

        // Right-click or Escape to cancel
        if (Mouse.current.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        // A/D to rotate
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
                ghostRotationY -= 45f;
            if (Keyboard.current.dKey.wasPressedThisFrame)
                ghostRotationY += 45f;
        }

        // Get mouse world position (free floating, no grid)
        Vector3 worldPos = GetMouseWorldPosition();
        Vector3 finalPos = worldPos;
        Quaternion finalRot = Quaternion.Euler(0, ghostRotationY, 0);

        // Use corner snap solver for wall-to-wall alignment
        if (WallManager.Instance != null && ghostWall != null)
        {
            Vector3 ghostScale = ghostWall.transform.localScale;
            var snap = CornerSnapSolver.Solve(worldPos, ghostRotationY, ghostScale,
                                               WallManager.Instance.AllWalls);
            if (snap.didSnap)
            {
                finalPos = snap.position;
                finalRot = snap.rotation;
            }
        }

        // Update ghost
        if (ghostWall != null)
        {
            ghostWall.transform.position = new Vector3(finalPos.x, 0.5f, finalPos.z);
            ghostWall.transform.rotation = finalRot;

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

        // Left-click to place
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceWall(new Vector3(finalPos.x, 0.5f, finalPos.z), finalRot);
        }
    }

    public void StartPlacement()
    {
        // Clean up any existing ghost from a previous placement
        if (ghostWall != null)
        {
            Debug.Log("[WallPlacement] Cleaning up previous ghost before new placement.");
            Destroy(ghostWall);
            ghostWall = null;
        }

        isPlacing = true;
        ghostRotationY = 0f;
        skipNextFrame = true;
        Debug.Log("[WallPlacement] StartPlacement called.");
        if (wallGhostPrefab != null)
        {
            // Spawn ghost just outside the wall ring so it's immediately visible
            // (mouse is over the UI button at this point, which projects far off-screen)
            Vector3 initialPos = new Vector3(0f, 0.5f, -6f);
            ghostWall = Instantiate(wallGhostPrefab, initialPos, Quaternion.identity);
            // Ensure the ghost has a mesh (prefab's built-in Cube mesh reference can be lost)
            EnsureWallMesh(ghostWall);
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
            // URP/Lit shader uses _BaseColor, not _Color (which mat.color maps to)
            Color ghostColor = new Color(0.3f, 0.9f, 0.3f, 1f);
            foreach (var rend in ghostWall.GetComponentsInChildren<Renderer>())
            {
                var mat = rend.material;
                mat.SetColor("_BaseColor", ghostColor);
            }
            Debug.Log($"[WallPlacement] Ghost ready at {initialPos}.");
        }
        else
        {
            Debug.LogError("[WallPlacement] wallGhostPrefab is null at StartPlacement! No ghost preview.");
        }
    }

    public void CancelPlacement()
    {
        if (isPlacing)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddTreasure(wallCost);
                Debug.Log($"[WallPlacement] Placement cancelled, refunded {wallCost} gold.");
            }
        }
        isPlacing = false;
        if (ghostWall != null)
        {
            Destroy(ghostWall);
            ghostWall = null;
        }
    }

    private void TryPlaceWall(Vector3 position, Quaternion rotation)
    {
        if (WallManager.Instance != null)
        {
            WallManager.Instance.PlaceWall(position, rotation);
            Debug.Log($"[WallPlacement] Wall placed at {position}, rotation={rotation.eulerAngles}.");
        }
        else
        {
            Debug.LogError("[WallPlacement] WallManager.Instance is null!");
        }
        FinishPlacement();
    }

    private void FinishPlacement()
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

    /// <summary>
    /// Ensures the wall GameObject has a mesh. The WallSegment prefab was created from
    /// CreatePrimitive(Cube) but the built-in mesh reference doesn't survive prefab serialization,
    /// so runtime-instantiated walls get a null mesh. This assigns the Cube mesh as a fallback.
    /// </summary>
    public static void EnsureWallMesh(GameObject wallGO)
    {
        var mf = wallGO.GetComponent<MeshFilter>();
        if (mf == null) return;
        if (mf.sharedMesh != null) return;

        // Create a temporary cube to grab the built-in Cube mesh
        var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempCube);
        Debug.Log($"[WallPlacement] Assigned Cube mesh to {wallGO.name} (prefab mesh was null).");
    }
}
