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
            DayNightCycle.Instance.OnNewDay -= OnNewDay;
        subscribed = false;
    }

    private void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnNewDay -= OnNewDay;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (DayNightCycle.Instance != null)
        {
            // Subscribe to OnNewDay (fires after OnDayStarted) so DailyEventManager
            // has already picked the event by the time we read it.
            DayNightCycle.Instance.OnNewDay += OnNewDay;
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

    private void OnNewDay(int dayNumber)
    {
        bool hasEvent = DailyEventManager.Instance != null && DailyEventManager.Instance.HasActiveEvent;
        bool hasWavePreview = dayNumber > 1 && EnemySpawnManager.Instance != null;

        if (!hasEvent && !hasWavePreview) return;

        WavePreviewData? preview = null;
        if (hasWavePreview)
            preview = EnemySpawnManager.Instance.GetWavePreview(dayNumber);

        ShowDayBanner(dayNumber, preview);
    }

    private void ShowDayBanner(int dayNumber, WavePreviewData? waveData)
    {
        if (panelRoot == null) return;

        // Header
        if (headerText != null)
        {
            if (waveData.HasValue && waveData.Value.hasBoss)
                headerText.text = string.Format("DAY {0} - BOSS WAVE", dayNumber);
            else
                headerText.text = string.Format("DAY {0}", dayNumber);
        }

        // Body
        if (bodyText != null)
        {
            var sb = new System.Text.StringBuilder();

            // Daily event announcement
            var dem = DailyEventManager.Instance;
            if (dem != null && dem.HasActiveEvent)
            {
                string eventColor;
                switch (dem.CurrentEventCategory)
                {
                    case DailyEventCategory.Beneficial: eventColor = "#66DD66"; break;
                    case DailyEventCategory.Detrimental: eventColor = "#FF6644"; break;
                    default: eventColor = "#FFCC44"; break;
                }
                sb.AppendFormat("<color={0}>{1}</color>", eventColor, dem.CurrentEventName);
                sb.AppendLine();
                sb.AppendFormat("<color={0}><size=80%>{1}</size></color>", eventColor, dem.CurrentEventDescription);
                sb.AppendLine();
            }

            // Wave preview info (day 2+)
            if (waveData.HasValue)
            {
                var data = waveData.Value;
                sb.AppendLine();

                // Spawn direction
                sb.AppendLine(data.spawnDirection);

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

                // Boss warning
                if (data.hasBoss && !string.IsNullOrEmpty(data.bossName))
                {
                    sb.AppendLine();
                    sb.AppendFormat("<color=#FF3333>{0} approaches!</color>", data.bossName);
                }
            }

            bodyText.text = sb.ToString();
        }

        panelRoot.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        hideTimer = displayDuration;

        var eventName = DailyEventManager.Instance != null ? DailyEventManager.Instance.CurrentEventName : "none";
        Debug.Log($"[WavePreviewUI] Day {dayNumber} banner: event={eventName}, hasWave={waveData.HasValue}");
    }
}
