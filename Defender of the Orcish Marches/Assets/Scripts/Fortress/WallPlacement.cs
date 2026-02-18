using UnityEngine;
using UnityEngine.InputSystem;

public class WallPlacement : MonoBehaviour
{
    [SerializeField] private int wallCost = 20;
    [SerializeField] private GameObject wallGhostPrefab;

    private bool isPlacing;
    private GameObject ghostWall;
    private UnityEngine.Camera mainCam;
    private float ghostRotationY;
    private bool skipNextFrame; // Skip the frame StartPlacement was called to avoid button-click placing wall

    // Wall segment half-extents (used for ghost creation reference)
    private const float WALL_HALF_WIDTH = 1.25f; // half-length along local X
    private const float WALL_HALF_DEPTH = 0.25f; // half-depth along local Z

    public bool IsPlacing => isPlacing;

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;

        if (wallGhostPrefab == null && WallManager.Instance != null)
        {
            wallGhostPrefab = WallManager.Instance.WallPrefab;
            if (wallGhostPrefab != null)
                Debug.Log("[WallPlacement] Using WallManager.WallPrefab as ghost prefab.");
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

        // Left-click to place
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceWall(new Vector3(finalPos.x, 0.5f, finalPos.z), finalRot);
        }
    }

    public void StartPlacement()
    {
        isPlacing = true;
        ghostRotationY = 0f;
        skipNextFrame = true;
        Debug.Log("[WallPlacement] Entering placement mode. A/D to rotate, left-click to place, right-click to cancel.");
        if (wallGhostPrefab != null)
        {
            ghostWall = Instantiate(wallGhostPrefab);
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
            // Make ghost semi-transparent
            var rend = ghostWall.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var mat = rend.material;
                var color = mat.color;
                color.a = 0.5f;
                mat.color = color;
                // Enable transparency
                mat.SetFloat("_Surface", 1); // URP Lit: 0=Opaque, 1=Transparent
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
        }
        else
        {
            Debug.LogWarning("[WallPlacement] wallGhostPrefab is null! No ghost preview.");
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
}
