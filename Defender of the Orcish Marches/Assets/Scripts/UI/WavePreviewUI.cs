using UnityEngine;
using TMPro;

public class WavePreviewUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [SerializeField] private float displayDuration = 5f;

    private float hideTimer;
    private bool subscribed;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        if (panelRoot != null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            panelRoot.SetActive(false);
        }
        TrySubscribe();
    }

    private void OnEnable()
    {
        subscribed = false;
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDayStarted -= OnDayStarted;
        subscribed = false;
    }

    private void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDayStarted -= OnDayStarted;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted += OnDayStarted;
            subscribed = true;
        }
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;

            // Fade out in the last second
            if (canvasGroup != null && hideTimer < 1f)
            {
                canvasGroup.alpha = Mathf.Max(0f, hideTimer);
            }

            if (hideTimer <= 0f)
            {
                if (panelRoot != null) panelRoot.SetActive(false);
            }
        }
    }

    private void OnDayStarted()
    {
        if (EnemySpawnManager.Instance == null) return;
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;

        // Skip day 1 — nothing to preview yet
        if (dayNumber <= 1) return;

        var preview = EnemySpawnManager.Instance.GetWavePreview(dayNumber);
        ShowPreview(preview);
    }

    private void ShowPreview(WavePreviewData data)
    {
        if (panelRoot == null) return;

        // Header
        if (headerText != null)
        {
            headerText.text = data.hasBoss
                ? string.Format("DAY {0} - BOSS WAVE", data.dayNumber)
                : string.Format("DAY {0}", data.dayNumber);
        }

        // Body
        if (bodyText != null)
        {
            var sb = new System.Text.StringBuilder();

            // Spawn direction
            sb.AppendLine(data.spawnDirection);
            sb.AppendLine();

            // Enemy types
            sb.Append("Enemies: ");
            sb.AppendLine(string.Join(", ", data.enemyTypes));

            // Highlight new enemy types
            if (data.newEnemyTypes != null && data.newEnemyTypes.Length > 0)
            {
                sb.Append("<color=#FF6644>NEW: ");
                sb.Append(string.Join(", ", data.newEnemyTypes));
                sb.AppendLine("</color>");
            }

            // Stat scaling (only show if meaningful)
            if (data.hpMultiplier > 1.05f)
            {
                sb.AppendLine();
                sb.AppendFormat("Enemy strength: HP x{0:F1}  DMG x{1:F1}", data.hpMultiplier, data.damageMultiplier);
            }

            // Boss warning
            if (data.hasBoss && !string.IsNullOrEmpty(data.bossName))
            {
                sb.AppendLine();
                sb.AppendFormat("<color=#FF3333>{0} approaches!</color>", data.bossName);
            }

            bodyText.text = sb.ToString();
        }

        panelRoot.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        hideTimer = displayDuration;

        Debug.Log($"[WavePreviewUI] Showing preview for Day {data.dayNumber}: {data.enemyTypes.Length} enemy types, direction={data.spawnDirection}, boss={data.hasBoss}");
    }
}
