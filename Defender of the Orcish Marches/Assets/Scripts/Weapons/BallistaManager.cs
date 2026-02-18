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
    }

    private void Update()
    {
        if (ballistas.Count == 0) return;

        // Tab to switch active ballista
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && ballistas.Count > 1)
        {
            activeBallistaIndex = (activeBallistaIndex + 1) % ballistas.Count;
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
