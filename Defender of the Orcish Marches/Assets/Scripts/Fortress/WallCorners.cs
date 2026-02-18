using UnityEngine;

/// <summary>
/// Tracks the 4 XZ corner positions of a wall segment's two ends.
/// Each end (left = -X, right = +X) has a front (+Z) and back (-Z) corner.
/// Attach to every wall segment alongside the Wall component.
/// </summary>
public class WallCorners : MonoBehaviour
{
    /// <summary>
    /// Corner indices: [0]=LeftFront, [1]=LeftBack, [2]=RightFront, [3]=RightBack
    /// "Left" = -localX end, "Right" = +localX end
    /// "Front" = +localZ face, "Back" = -localZ face
    /// </summary>
    public Vector3[] Corners { get; private set; } = new Vector3[4];

    // Cached half-extents (derived from transform.localScale each refresh)
    public float HalfWidth { get; private set; }
    public float HalfDepth { get; private set; }

    // If true, skip creating child collider objects (used for ghost preview walls)
    [HideInInspector] public bool isGhost;

    private void Awake()
    {
        RefreshCorners();
    }

    private void Start()
    {
        RefreshCorners();
    }

    /// <summary>
    /// Recompute the 4 corner world positions from the current transform.
    /// Call after changing position/rotation/scale.
    /// </summary>
    public void RefreshCorners()
    {
        Vector3 pos = transform.position;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector3 scale = transform.localScale;

        HalfWidth = scale.x * 0.5f;
        HalfDepth = scale.z * 0.5f;

        Vector3 toRight = right * HalfWidth;
        Vector3 toFront = forward * HalfDepth;

        // Left end (-X) corners
        Corners[0] = pos - toRight + toFront;  // LeftFront
        Corners[1] = pos - toRight - toFront;  // LeftBack

        // Right end (+X) corners
        Corners[2] = pos + toRight + toFront;  // RightFront
        Corners[3] = pos + toRight - toFront;  // RightBack
    }

    /// <summary>
    /// Get the 2 corners for a specific end.
    /// endSign: -1 for left end, +1 for right end.
    /// Returns (front, back) corner pair.
    /// </summary>
    public (Vector3 front, Vector3 back) GetEndCorners(int endSign)
    {
        if (endSign < 0)
            return (Corners[0], Corners[1]); // LeftFront, LeftBack
        else
            return (Corners[2], Corners[3]); // RightFront, RightBack
    }

    /// <summary>
    /// Get the center point of a specific end.
    /// </summary>
    public Vector3 GetEndCenter(int endSign)
    {
        Vector3 right = transform.right;
        return transform.position + right * (endSign * HalfWidth);
    }

    /// <summary>
    /// Check if a point is inside this wall's OBB (oriented bounding box) in XZ.
    /// Uses a small tolerance to handle floating point precision.
    /// </summary>
    public bool IsInsideOBB(Vector3 point, float tolerance = 0.01f)
    {
        Vector3 local = point - transform.position;
        float dotRight = Vector3.Dot(local, transform.right);
        float dotForward = Vector3.Dot(local, transform.forward);
        return Mathf.Abs(dotRight) <= HalfWidth + tolerance &&
               Mathf.Abs(dotForward) <= HalfDepth + tolerance;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Corners == null || Corners.Length < 4) return;
        RefreshCorners();

        float y = transform.position.y + 1.1f; // Draw slightly above wall top

        // Left end corners in blue
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(new Vector3(Corners[0].x, y, Corners[0].z), 0.06f);
        Gizmos.DrawSphere(new Vector3(Corners[1].x, y, Corners[1].z), 0.06f);
        Gizmos.DrawLine(new Vector3(Corners[0].x, y, Corners[0].z),
                        new Vector3(Corners[1].x, y, Corners[1].z));

        // Right end corners in red
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(Corners[2].x, y, Corners[2].z), 0.06f);
        Gizmos.DrawSphere(new Vector3(Corners[3].x, y, Corners[3].z), 0.06f);
        Gizmos.DrawLine(new Vector3(Corners[2].x, y, Corners[2].z),
                        new Vector3(Corners[3].x, y, Corners[3].z));

        // Outline in yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(Corners[0].x, y, Corners[0].z),
                        new Vector3(Corners[2].x, y, Corners[2].z));
        Gizmos.DrawLine(new Vector3(Corners[1].x, y, Corners[1].z),
                        new Vector3(Corners[3].x, y, Corners[3].z));
    }
#endif
}
