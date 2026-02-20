using System;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    public enum Phase { Day, Night }

    [Header("Timing")]
    [SerializeField] private float dayDuration = 60f;
    [SerializeField] private float nightDuration = 30f;
    [SerializeField] private float firstDayDuration = 60f;

    [Header("Lighting")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float dayIntensity = 1f;
    [SerializeField] private float nightIntensity = 0.15f;
    [SerializeField] private Color dayLightColor = Color.white;
    [SerializeField] private Color nightLightColor = new Color(0.4f, 0.5f, 0.8f); // blue moonlight
    [SerializeField] private Color dayAmbient = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color nightAmbient = new Color(0.05f, 0.05f, 0.1f);

    public Phase CurrentPhase { get; private set; } = Phase.Day;
    public int DayNumber { get; private set; } = 1;
    public bool IsDay => CurrentPhase == Phase.Day;
    public bool IsNight => CurrentPhase == Phase.Night;

    /// <summary>Duration of one full day+night cycle in seconds.</summary>
    public float FullCycleDuration => dayDuration + nightDuration;

    /// <summary>How far through the current phase (0..1).</summary>
    public float PhaseProgress => phaseTimer / CurrentPhaseDuration;

    /// <summary>Seconds remaining in the current phase.</summary>
    public float PhaseTimeRemaining => Mathf.Max(0f, CurrentPhaseDuration - phaseTimer);

    public event Action OnDayStarted;
    public event Action OnNightStarted;
    public event Action<int> OnNewDay;

    private float phaseTimer;
    private bool isFirstDay = true;

    private float CurrentPhaseDuration
    {
        get
        {
            if (CurrentPhase == Phase.Day)
                return (isFirstDay ? firstDayDuration : dayDuration) * GameSettings.GetDayDurationMultiplier();
            return nightDuration * GameSettings.GetNightDurationMultiplier();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[DayNightCycle] Instance set in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[DayNightCycle] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        CurrentPhase = Phase.Day;
        DayNumber = 1;
        phaseTimer = 0f;
        isFirstDay = true;
        Debug.Log($"[DayNightCycle] Day {DayNumber} started (first day, {firstDayDuration}s).");
        if (SoundManager.Instance != null) SoundManager.Instance.PlayAttackStart(Vector3.zero);
        OnDayStarted?.Invoke();
        OnNewDay?.Invoke(DayNumber);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        phaseTimer += Time.deltaTime;

        if (phaseTimer >= CurrentPhaseDuration)
        {
            if (CurrentPhase == Phase.Day)
                TransitionToNight();
            else
                TransitionToDay();
        }

        UpdateLighting();
    }

    private void TransitionToNight()
    {
        CurrentPhase = Phase.Night;
        phaseTimer = 0f;
        if (isFirstDay) isFirstDay = false;
        Debug.Log($"[DayNightCycle] Night started after Day {DayNumber}. Duration={nightDuration}s.");
        OnNightStarted?.Invoke();
    }

    private void TransitionToDay()
    {
        CurrentPhase = Phase.Day;
        DayNumber++;
        phaseTimer = 0f;
        Debug.Log($"[DayNightCycle] Day {DayNumber} started. Duration={dayDuration}s.");
        if (SoundManager.Instance != null) SoundManager.Instance.PlayAttackStart(Vector3.zero);
        OnDayStarted?.Invoke();
        OnNewDay?.Invoke(DayNumber);
    }

    private void UpdateLighting()
    {
        if (directionalLight == null) return;

        if (CurrentPhase == Phase.Day)
        {
            // Rotate light X from 10 (dawn/east) to 170 (dusk/west)
            float angle = Mathf.Lerp(10f, 170f, PhaseProgress);
            directionalLight.transform.rotation = Quaternion.Euler(angle, -30f, 0f);
            directionalLight.intensity = dayIntensity;
            directionalLight.color = dayLightColor;
            RenderSettings.ambientLight = dayAmbient;
        }
        else
        {
            // Night: dim moonlight, fixed overhead angle
            directionalLight.transform.rotation = Quaternion.Euler(90f, -30f, 0f);
            directionalLight.intensity = nightIntensity;
            directionalLight.color = nightLightColor;
            RenderSettings.ambientLight = nightAmbient;
        }
    }
}
