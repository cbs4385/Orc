using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallistaManager : MonoBehaviour
{
    public static BallistaManager Instance { get; private set; }

    [SerializeField] private GameObject ballistaPrefab;
    [SerializeField] private Transform[] ballistaSlots;

    private List<Ballista> ballistas = new List<Ballista>();
    private int activeBallistaIndex;

    public Ballista ActiveBallista => ballistas.Count > 0 ? ballistas[activeBallistaIndex] : null;

    public event System.Action<Ballista> OnActiveBallistaChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[BallistaManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[BallistaManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Register existing ballistas
        foreach (var b in GetComponentsInChildren<Ballista>())
        {
            ballistas.Add(b);
        }

        // In nightmare mode, face first ballista west and spawn FPS camera
        if (NightmareCamera.IsNightmareMode && ballistas.Count > 0)
        {
            ballistas[0].transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            Debug.Log("[BallistaManager] Nightmare mode — first ballista set to face west.");

            // Spawn NightmareCamera if it doesn't already exist
            if (NightmareCamera.Instance == null)
            {
                var camObj = new GameObject("NightmareCamera");
                camObj.AddComponent<NightmareCamera>();
                Debug.Log("[BallistaManager] Spawned NightmareCamera GameObject.");
            }
        }
    }

    private void Update()
    {
        if (ballistas.Count == 0) return;

        // Tab to switch active ballista
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && ballistas.Count > 1)
        {
            activeBallistaIndex = (activeBallistaIndex + 1) % ballistas.Count;
            Debug.Log($"[BallistaManager] Switched to ballista {activeBallistaIndex}: {ballistas[activeBallistaIndex].name}");
            OnActiveBallistaChanged?.Invoke(ballistas[activeBallistaIndex]);
        }

        // Only the active ballista responds to input
        for (int i = 0; i < ballistas.Count; i++)
        {
            ballistas[i].enabled = (i == activeBallistaIndex);
        }
    }

    public bool AddBallista()
    {
        if (ballistaPrefab == null || ballistaSlots == null) return false;
        if (ballistas.Count >= ballistaSlots.Length) return false;

        var slot = ballistaSlots[ballistas.Count];
        var go = Instantiate(ballistaPrefab, slot.position, slot.rotation, slot);
        var ballista = go.GetComponent<Ballista>();
        if (ballista != null)
        {
            ballistas.Add(ballista);
            return true;
        }
        return false;
    }
}
