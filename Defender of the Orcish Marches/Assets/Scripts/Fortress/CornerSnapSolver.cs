using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static utility that computes wall snap positions using corner-to-corner alignment.
/// Guarantees seamless outer edges regardless of build direction (CW or CCW).
///
/// For angled joints, exactly ONE corner of the new wall aligns with one corner of the
/// existing wall (the "anchor" corner — the outer edge corner). The other connecting-end
/// corner tucks inside the existing wall's volume. The solver tries all 4 single-corner
/// alignments per connection and picks the one with the best score.
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
    }

    // Search radius for candidate wall ends near the mouse
    private const float BROAD_SEARCH_RADIUS = 3.0f;
    // Max distance from mouse to accept a snap candidate
    private const float MOUSE_ACCEPT_RADIUS = 2.0f;
    // Distance threshold for ring-close detection on the far end
    private const float RING_CLOSE_RADIUS = 1.0f;
    // Corner overlap threshold
    private const float CORNER_OVERLAP_DIST = 0.10f;

    // Scoring weights
    private const float SCORE_OVERLAP = 10f;
    private const float SCORE_INSIDE = 3f;
    private const float SCORE_EXPOSED = -5f;

    /// <summary>
    /// Main entry point. Finds the best snap position for a new wall segment.
    /// </summary>
    public static SnapResult Solve(Vector3 mousePos, float ghostRotationY, Vector3 ghostScale,
                                    IReadOnlyList<Wall> allWalls)
    {
        var best = new SnapResult { didSnap = false, score = float.NegativeInfinity };

        if (allWalls == null || allWalls.Count == 0)
            return best;

        float newHW = ghostScale.x * 0.5f;
        float newHD = ghostScale.z * 0.5f;

        // Broad search: find all existing wall ends within range of mouse
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

        // For each candidate existing wall end, try connecting the new wall
        foreach (var (existingWall, existingEndSign, _) in candidates)
        {
            var existingCorners = existingWall.GetComponent<WallCorners>();
            var (existFront, existBack) = existingCorners.GetEndCorners(existingEndSign);
            Vector3[] existEndPts = { existFront, existBack };

            // Base rotation: existing wall's rotation + user's A/D offset
            float baseRotY = existingWall.transform.rotation.eulerAngles.y + ghostRotationY;
            Quaternion newRot = Quaternion.Euler(0, baseRotY, 0);
            Vector3 newRight = newRot * Vector3.right;
            Vector3 newForward = newRot * Vector3.forward;

            // Try connecting each end of the new wall (left=-1, right=+1)
            for (int newEndSign = -1; newEndSign <= 1; newEndSign += 2)
            {
                // New wall's connecting end corners (offsets from center)
                Vector3 newEndOffset = newRight * (newEndSign * newHW);
                Vector3[] newEndCornerOffsets = {
                    newEndOffset + newForward * newHD,  // front corner of connecting end
                    newEndOffset - newForward * newHD   // back corner of connecting end
                };

                // Try all 4 individual corner-to-corner alignments
                // (2 existing end corners x 2 new connecting end corners)
                for (int ei = 0; ei < 2; ei++)
                {
                    for (int ni = 0; ni < 2; ni++)
                    {
                        // Anchor: align new corner to existing corner
                        Vector3 candidateCenter = existEndPts[ei] - newEndCornerOffsets[ni];

                        // Check if candidate center is within acceptable distance of mouse
                        if (Dist2D(mousePos, candidateCenter) > MOUSE_ACCEPT_RADIUS)
                            continue;

                        // Score this candidate
                        float score = ScoreCandidate(candidateCenter, newRot, newHW, newHD,
                                                      newEndSign, existingCorners, existingEndSign);

                        // Check for ring closing on the far end
                        bool ringClose = false;
                        Vector3 finalPos = candidateCenter;
                        Quaternion finalRot = newRot;

                        int farEndSign = -newEndSign;
                        Vector3 farEndCenter = candidateCenter + newRight * (farEndSign * newHW);

                        var ringResult = TryRingClose(farEndCenter, candidateCenter, newRot,
                                                       newHW, newHD, newEndSign, farEndSign,
                                                       existingWall, existingCorners, existingEndSign,
                                                       allWalls);
                        if (ringResult.HasValue)
                        {
                            ringClose = true;
                            finalPos = ringResult.Value.position;
                            finalRot = ringResult.Value.rotation;
                            score += 15f;
                        }

                        if (score > best.score)
                        {
                            best = new SnapResult
                            {
                                didSnap = true,
                                position = ringClose ? finalPos : candidateCenter,
                                rotation = ringClose ? finalRot : newRot,
                                score = score,
                                isRingClose = ringClose
                            };
                        }
                    }
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Score a candidate snap position. Checks all 4 new wall corners against
    /// the existing wall's connecting end corners and OBB.
    /// </summary>
    private static float ScoreCandidate(Vector3 newCenter, Quaternion newRot,
                                         float newHW, float newHD,
                                         int connectingEndSign,
                                         WallCorners existingCorners, int existingEndSign)
    {
        float score = 0f;
        Vector3 newRight = newRot * Vector3.right;
        Vector3 newForward = newRot * Vector3.forward;

        // Compute new wall's 4 corners
        Vector3[] nc = new Vector3[4];
        nc[0] = newCenter - newRight * newHW + newForward * newHD;  // LeftFront
        nc[1] = newCenter - newRight * newHW - newForward * newHD;  // LeftBack
        nc[2] = newCenter + newRight * newHW + newForward * newHD;  // RightFront
        nc[3] = newCenter + newRight * newHW - newForward * newHD;  // RightBack

        // Existing wall's end corners
        var (existFront, existBack) = existingCorners.GetEndCorners(existingEndSign);
        Vector3[] existPts = { existFront, existBack };

        // Determine which new corners are on the connecting end
        // Left end = indices 0,1; Right end = indices 2,3
        int connectStart = connectingEndSign < 0 ? 0 : 2;

        for (int i = 0; i < 4; i++)
        {
            bool isConnecting = (i == connectStart || i == connectStart + 1);
            bool overlaps = false;

            // Check overlap with existing end corners
            for (int e = 0; e < 2; e++)
            {
                if (Dist2D(nc[i], existPts[e]) < CORNER_OVERLAP_DIST)
                {
                    score += SCORE_OVERLAP;
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps && isConnecting)
            {
                // Connecting-side corner that doesn't overlap — check if it tucks inside
                if (existingCorners.IsInsideOBB(nc[i], 0.05f))
                {
                    score += SCORE_INSIDE;
                }
                else
                {
                    // Exposed corner on connecting side = gap in the wall ring
                    score += SCORE_EXPOSED;
                }
            }
            // Non-connecting side corners don't affect score (they're the "far end")
        }

        return score;
    }

    /// <summary>
    /// Try to close a ring by connecting the far end of the new wall to another existing wall.
    /// Uses corner-to-corner alignment with the bridging wall's rotation derived from the
    /// two target endpoints.
    /// </summary>
    private static (Vector3 position, Quaternion rotation)? TryRingClose(
        Vector3 farEndCenter, Vector3 currentCenter, Quaternion currentRot,
        float newHW, float newHD,
        int nearEndSign, int farEndSign,
        Wall nearWall, WallCorners nearCorners, int nearEndSign2,
        IReadOnlyList<Wall> allWalls)
    {
        // Find nearest existing wall end to the far end
        float bestDist = RING_CLOSE_RADIUS;
        Wall farWall = null;
        int farWallEndSign = 0;

        for (int i = 0; i < allWalls.Count; i++)
        {
            var wall = allWalls[i];
            if (wall == null || wall == nearWall || !wall.gameObject.activeInHierarchy) continue;
            var corners = wall.GetComponent<WallCorners>();
            if (corners == null) continue;

            for (int sign = -1; sign <= 1; sign += 2)
            {
                float dist = Dist2D(farEndCenter, corners.GetEndCenter(sign));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    farWall = wall;
                    farWallEndSign = sign;
                }
            }
        }

        if (farWall == null) return null;

        var farCorners = farWall.GetComponent<WallCorners>();
        if (farCorners == null) return null;

        // Get the two target endpoint centers
        Vector3 nearTarget = nearCorners.GetEndCenter(nearEndSign2);
        Vector3 farTarget = farCorners.GetEndCenter(farWallEndSign);

        // Direction determines wall orientation: wall's localX aligns with this direction
        Vector3 dir = farTarget - nearTarget;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return null;

        // Compute rotation so that transform.right = dir.normalized
        Vector3 dirNorm = dir.normalized;
        Vector3 perpForward = new Vector3(-dirNorm.z, 0, dirNorm.x);
        Quaternion bridgeRot = Quaternion.LookRotation(perpForward, Vector3.up);

        Vector3 bridgeRight = bridgeRot * Vector3.right;
        Vector3 bridgeForward = bridgeRot * Vector3.forward;

        // Get corner pairs from both connecting walls
        var (nearFront, nearBack) = nearCorners.GetEndCorners(nearEndSign2);
        var (farFront, farBack) = farCorners.GetEndCorners(farWallEndSign);

        // The new wall's near end connects to nearWall, far end connects to farWall
        // nearEndSign determines which end of the NEW wall is the near side
        // Try all corner alignments and pick the best-scoring one
        Vector3[] nearTargetCorners = { nearFront, nearBack };
        Vector3[] farTargetCorners = { farFront, farBack };

        float bestScore = float.NegativeInfinity;
        Vector3 bestPos = (nearTarget + farTarget) * 0.5f;

        // New wall corner offsets from center
        Vector3 nearEndOffset = bridgeRight * (nearEndSign * newHW);
        Vector3 farEndOffset = bridgeRight * (farEndSign * newHW);

        Vector3[] nearNewCornerOffsets = {
            nearEndOffset + bridgeForward * newHD,
            nearEndOffset - bridgeForward * newHD
        };
        Vector3[] farNewCornerOffsets = {
            farEndOffset + bridgeForward * newHD,
            farEndOffset - bridgeForward * newHD
        };

        // Try anchoring from near side (4 combos) and far side (4 combos)
        for (int ni = 0; ni < 2; ni++)
        {
            for (int ti = 0; ti < 2; ti++)
            {
                Vector3 centerFromNear = nearTargetCorners[ti] - nearNewCornerOffsets[ni];
                float s = ScoreRingCandidate(centerFromNear, bridgeRot, newHW, newHD,
                                              nearEndSign, farEndSign,
                                              nearCorners, nearEndSign2,
                                              farCorners, farWallEndSign);
                if (s > bestScore) { bestScore = s; bestPos = centerFromNear; }

                Vector3 centerFromFar = farTargetCorners[ti] - farNewCornerOffsets[ni];
                s = ScoreRingCandidate(centerFromFar, bridgeRot, newHW, newHD,
                                        nearEndSign, farEndSign,
                                        nearCorners, nearEndSign2,
                                        farCorners, farWallEndSign);
                if (s > bestScore) { bestScore = s; bestPos = centerFromFar; }
            }
        }

        return (bestPos, bridgeRot);
    }

    /// <summary>
    /// Score a ring-close candidate by checking corners against both connecting walls.
    /// </summary>
    private static float ScoreRingCandidate(Vector3 center, Quaternion rot,
                                             float hw, float hd,
                                             int nearEndSign, int farEndSign,
                                             WallCorners cornersA, int endSignA,
                                             WallCorners cornersB, int endSignB)
    {
        float score = 0f;
        Vector3 right = rot * Vector3.right;
        Vector3 forward = rot * Vector3.forward;

        Vector3[] nc = new Vector3[4];
        nc[0] = center - right * hw + forward * hd;
        nc[1] = center - right * hw - forward * hd;
        nc[2] = center + right * hw + forward * hd;
        nc[3] = center + right * hw - forward * hd;

        var (aFront, aBack) = cornersA.GetEndCorners(endSignA);
        var (bFront, bBack) = cornersB.GetEndCorners(endSignB);
        Vector3[] targets = { aFront, aBack, bFront, bBack };

        for (int i = 0; i < 4; i++)
        {
            bool found = false;
            for (int t = 0; t < targets.Length; t++)
            {
                if (Dist2D(nc[i], targets[t]) < CORNER_OVERLAP_DIST)
                {
                    score += SCORE_OVERLAP;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                // Both ends are connecting in a ring close, so all corners matter
                if (cornersA.IsInsideOBB(nc[i], 0.05f) || cornersB.IsInsideOBB(nc[i], 0.05f))
                    score += SCORE_INSIDE;
                else
                    score += SCORE_EXPOSED;
            }
        }

        return score;
    }

    private static float Dist2D(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
