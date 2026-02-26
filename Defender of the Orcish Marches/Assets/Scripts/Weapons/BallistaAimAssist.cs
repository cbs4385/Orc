using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nightmare mode aiming assist for the Ballista/Scorpio.
/// Shows a trajectory arc, ground impact marker, and highlights enemies
/// that intersect the projected bolt path.
/// Only active in Nightmare (FPS) mode.
/// </summary>
public class BallistaAimAssist : MonoBehaviour
{
    private const float GRAVITY = 15f;
    private const float SIM_STEP = 0.02f;
    private const float GROUND_Y = 0.05f;
    private const float ENEMY_HIGHLIGHT_RADIUS = 0.8f;
    private const int IMPACT_CIRCLE_SEGMENTS = 32;

    private Ballista ballista;

    private LineRenderer trajectoryLine;
    private LineRenderer impactCircle;

    // Trajectory simulation cache
    private readonly List<Vector3> trajectoryPoints = new List<Vector3>(128);
    private Vector3 impactPoint;
    private bool hasImpact;

    // Enemy highlighting
    private HashSet<Enemy> currentlyHighlighted = new HashSet<Enemy>();
    private HashSet<Enemy> nextHighlighted = new HashSet<Enemy>();
    private static MaterialPropertyBlock highlightBlock;
    private static MaterialPropertyBlock clearBlock;

    public void Initialize(Ballista source)
    {
        ballista = source;

        trajectoryLine = CreateLine("TrajectoryArc",
            new Color(1f, 0.4f, 0.1f, 0.7f), new Color(1f, 0.4f, 0.1f, 0.15f),
            0.04f, 0.02f);

        impactCircle = CreateLine("ImpactCircle",
            new Color(1f, 0.6f, 0.1f, 0.85f), new Color(1f, 0.6f, 0.1f, 0.85f),
            0.06f, 0.06f);
        impactCircle.loop = true;
        impactCircle.positionCount = IMPACT_CIRCLE_SEGMENTS;

        if (highlightBlock == null)
        {
            highlightBlock = new MaterialPropertyBlock();
            highlightBlock.SetColor("_BaseColor", new Color(1f, 0.25f, 0.15f, 1f));
        }
        if (clearBlock == null)
            clearBlock = new MaterialPropertyBlock();

        Debug.Log("[BallistaAimAssist] Initialized.");
    }

    private LineRenderer CreateLine(string lineName, Color startColor, Color endColor,
        float startWidth, float endWidth)
    {
        var go = new GameObject(lineName);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);
        lr.startColor = startColor;
        lr.endColor = endColor;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.positionCount = 0;
        return lr;
    }

    private void Update()
    {
        if (ballista == null) return;

        // Hide during non-play states and build mode
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            SetVisible(false);
            return;
        }
        if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        SimulateTrajectory();
        UpdateTrajectoryLine();
        UpdateImpactCircle();
        UpdateEnemyHighlights();
    }

    private void OnDisable()
    {
        ClearAllHighlights();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (trajectoryLine != null) trajectoryLine.enabled = visible;
        if (impactCircle != null) impactCircle.enabled = visible;
        if (!visible) ClearAllHighlights();
    }

    private void SimulateTrajectory()
    {
        trajectoryPoints.Clear();
        hasImpact = false;

        var cam = Camera.main;
        if (cam == null) return;

        float speed = ballista.ProjectileSpeed;
        float range = ballista.MaxRange;

        Vector3 pos = ballista.FirePoint.position;
        Vector3 vel = cam.transform.forward * speed;
        float totalDist = 0f;

        trajectoryPoints.Add(pos);

        float maxTime = range / speed * 2f;
        for (float t = 0; t < maxTime; t += SIM_STEP)
        {
            vel += Vector3.down * GRAVITY * SIM_STEP;
            Vector3 step = vel * SIM_STEP;
            pos += step;
            totalDist += step.magnitude;

            trajectoryPoints.Add(pos);

            if (pos.y < GROUND_Y)
            {
                impactPoint = new Vector3(pos.x, GROUND_Y, pos.z);
                hasImpact = true;
                break;
            }

            if (totalDist >= range)
            {
                impactPoint = new Vector3(pos.x, Mathf.Max(pos.y, GROUND_Y), pos.z);
                hasImpact = true;
                break;
            }
        }

        if (!hasImpact && trajectoryPoints.Count > 1)
        {
            impactPoint = trajectoryPoints[trajectoryPoints.Count - 1];
            hasImpact = true;
        }
    }

    private void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null) return;
        trajectoryLine.positionCount = trajectoryPoints.Count;
        for (int i = 0; i < trajectoryPoints.Count; i++)
            trajectoryLine.SetPosition(i, trajectoryPoints[i]);
    }

    private void UpdateImpactCircle()
    {
        if (impactCircle == null || !hasImpact)
        {
            if (impactCircle != null) impactCircle.enabled = false;
            return;
        }

        impactCircle.enabled = true;
        float radius = ballista.BurstDamageRadius > 0 ? ballista.BurstDamageRadius : 1.0f;

        for (int i = 0; i < IMPACT_CIRCLE_SEGMENTS; i++)
        {
            float angle = i * 2f * Mathf.PI / IMPACT_CIRCLE_SEGMENTS;
            impactCircle.SetPosition(i, impactPoint + new Vector3(
                Mathf.Cos(angle) * radius,
                0.1f,
                Mathf.Sin(angle) * radius
            ));
        }
    }

    private void UpdateEnemyHighlights()
    {
        nextHighlighted.Clear();

        if (trajectoryPoints.Count < 2) return;

        // Check each active enemy against trajectory path
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            Vector3 enemyPos = enemy.transform.position;

            for (int i = 0; i < trajectoryPoints.Count; i++)
            {
                float dist = Vector3.Distance(enemyPos, trajectoryPoints[i]);
                if (dist <= ENEMY_HIGHLIGHT_RADIUS)
                {
                    nextHighlighted.Add(enemy);
                    break;
                }
            }
        }

        // Unhighlight enemies no longer in path
        foreach (var enemy in currentlyHighlighted)
        {
            if (enemy != null && !nextHighlighted.Contains(enemy))
                UnhighlightEnemy(enemy);
        }

        // Highlight newly targeted enemies
        foreach (var enemy in nextHighlighted)
        {
            if (!currentlyHighlighted.Contains(enemy))
                HighlightEnemy(enemy);
        }

        // Swap sets
        var temp = currentlyHighlighted;
        currentlyHighlighted = nextHighlighted;
        nextHighlighted = temp;
    }

    private void HighlightEnemy(Enemy enemy)
    {
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
            r.SetPropertyBlock(highlightBlock);
    }

    private void UnhighlightEnemy(Enemy enemy)
    {
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
            r.SetPropertyBlock(clearBlock);
    }

    private void ClearAllHighlights()
    {
        foreach (var enemy in currentlyHighlighted)
        {
            if (enemy != null) UnhighlightEnemy(enemy);
        }
        currentlyHighlighted.Clear();
    }
}
