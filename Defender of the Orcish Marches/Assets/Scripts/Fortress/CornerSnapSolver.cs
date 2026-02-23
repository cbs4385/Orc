using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static utility that computes wall snap positions using tower-center-to-tower-center alignment.
/// Each wall end has an octagonal tower; snapping overlaps tower centers for clean joints
/// at any of the 8 allowed orientations (45-degree increments).
/// </summary>
public static class CornerSnapSolver
{
    public struct SnapResult
    {
        public bool didSnap;
        public Vector3 position;
        public Quaternion rotation;
        public float score;
        public bool isRingClose;
        public float scaleX;
    }

    // Search radius for candidate wall ends near the mouse
    private const float BROAD_SEARCH_RADIUS = 3.0f;
    // Max distance from mouse to accept a snap candidate
    private const float MOUSE_ACCEPT_RADIUS = 2.0f;
    // Distance threshold for ring-close detection on the far end
    // 1.5 to reliably detect diagonal targets (far-end estimate from non-scaled ghost can be off ~0.9 units)
    private const float RING_CLOSE_RADIUS = 1.5f;

    /// <summary>
    /// Main entry point. Finds the best snap position for a new wall segment by aligning
    /// end centers (tower centers) of the new wall to existing wall end centers.
    /// </summary>
    public static SnapResult Solve(Vector3 mousePos, float ghostRotationY, Vector3 ghostScale,
                                    IReadOnlyList<Wall> allWalls)
    {
        var best = new SnapResult { didSnap = false, score = float.NegativeInfinity };

        if (allWalls == null || allWalls.Count == 0)
            return best;

        float newHW = ghostScale.x * WallCorners.TOWER_OFFSET;

        // Broad search: find all existing wall end centers within range of mouse
        var candidates = new List<(Wall wall, int endSign, Vector3 endCenter)>();
        for (int i = 0; i < allWalls.Count; i++)
        {
            var wall = allWalls[i];
            if (wall == null || !wall.gameObject.activeInHierarchy) continue;
            var corners = wall.GetComponent<WallCorners>();
            if (corners == null) continue;
            corners.RefreshCorners();

            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector3 endCenter = corners.GetEndCenter(sign);
                if (Dist2D(mousePos, endCenter) < BROAD_SEARCH_RADIUS)
                    candidates.Add((wall, sign, endCenter));
            }
        }

        if (candidates.Count == 0)
            return best;

        // For each existing end center, try aligning each end of the new wall to it
        Quaternion newRot = Quaternion.Euler(0, ghostRotationY, 0);

        foreach (var (existingWall, existingEndSign, existingEndCenter) in candidates)
        {
            // Base rotation: existing wall's rotation + user's A/D offset
            float baseRotY = existingWall.transform.rotation.eulerAngles.y + ghostRotationY;
            newRot = Quaternion.Euler(0, baseRotY, 0);
            Vector3 newRight = newRot * Vector3.right;

            // Try connecting each end of the new wall (left=-1, right=+1)
            for (int newEndSign = -1; newEndSign <= 1; newEndSign += 2)
            {
                // Position new wall so its end center aligns with existing end center
                Vector3 candidateCenter = existingEndCenter - newRight * (newEndSign * newHW);

                // Check if candidate is within acceptable distance of mouse
                if (Dist2D(mousePos, candidateCenter) > MOUSE_ACCEPT_RADIUS)
                    continue;

                // Score: closer to mouse = better
                float distToMouse = Dist2D(mousePos, candidateCenter);
                float score = MOUSE_ACCEPT_RADIUS - distToMouse;

                // Check for ring closing on the far end
                bool ringClose = false;
                Vector3 finalPos = candidateCenter;
                Quaternion finalRot = newRot;
                float finalScaleX = ghostScale.x;

                int farEndSign = -newEndSign;
                Vector3 farEndCenter = candidateCenter + newRight * (farEndSign * newHW);

                var ringResult = TryRingClose(farEndCenter, existingEndCenter,
                                               newHW, newEndSign,
                                               existingWall, allWalls);
                if (ringResult.HasValue)
                {
                    ringClose = true;
                    finalPos = ringResult.Value.position;
                    finalRot = ringResult.Value.rotation;
                    finalScaleX = ringResult.Value.scaleX;
                    score += 15f; // Ring-close bonus
                }

                if (score > best.score)
                {
                    best = new SnapResult
                    {
                        didSnap = true,
                        position = ringClose ? finalPos : candidateCenter,
                        rotation = ringClose ? finalRot : newRot,
                        score = score,
                        isRingClose = ringClose,
                        scaleX = finalScaleX
                    };
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Try to close a ring by connecting the far end of the new wall to another existing wall's end center.
    /// Computes a bridge rotation from the two target end centers and re-solves position.
    /// Returns scaleX needed to span the actual distance between the two target centers.
    /// </summary>
    private static (Vector3 position, Quaternion rotation, float scaleX)? TryRingClose(
        Vector3 farEndCenter, Vector3 nearTargetCenter,
        float newHW, int nearEndSign,
        Wall nearWall, IReadOnlyList<Wall> allWalls)
    {
        // Find nearest existing wall end center to the far end
        float bestDist = RING_CLOSE_RADIUS;
        Vector3 farTargetCenter = Vector3.zero;
        bool found = false;

        for (int i = 0; i < allWalls.Count; i++)
        {
            var wall = allWalls[i];
            if (wall == null || wall == nearWall || !wall.gameObject.activeInHierarchy) continue;
            var corners = wall.GetComponent<WallCorners>();
            if (corners == null) continue;

            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector3 ec = corners.GetEndCenter(sign);
                float dist = Dist2D(farEndCenter, ec);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    farTargetCenter = ec;
                    found = true;
                }
            }
        }

        if (!found) return null;

        // Direction from near target to far target determines wall's local X axis
        Vector3 dir = farTargetCenter - nearTargetCenter;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return null;

        // Compute actual distance and required scaleX to span it
        float actualDist = dir.magnitude;
        float requiredScaleX = actualDist / (2f * WallCorners.TOWER_OFFSET);

        // Compute rotation so that transform.right = dir.normalized
        Vector3 dirNorm = dir.normalized;
        Vector3 perpForward = new Vector3(-dirNorm.z, 0, dirNorm.x);
        Quaternion bridgeRot = Quaternion.LookRotation(perpForward, Vector3.up);

        // Position: near end center of new wall sits on nearTargetCenter
        // Use half the actual distance (not the default newHW) so both ends align
        float newHW_ring = actualDist / 2f;
        Vector3 bridgeRight = bridgeRot * Vector3.right;
        Vector3 bridgeCenter = nearTargetCenter - bridgeRight * (nearEndSign * newHW_ring);

        return (bridgeCenter, bridgeRot, requiredScaleX);
    }

    private static float Dist2D(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
