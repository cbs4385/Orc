using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallistaManager : MonoBehaviour
{
    public static BallistaManager Instance { get; private set; }

    [SerializeField] private GameObject ballistaPrefab;
    [SerializeField] private Transform[] ballistaSlots;

    /// <summary>Extra height added to tower and ballistas in Nightmare FPS mode for better downward aiming.</summary>
    private const float NIGHTMARE_HEIGHT_BOOST = 2.0f;

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

        // In nightmare mode, raise tower + ballistas for better downward aiming, then spawn FPS camera
        if (NightmareCamera.IsNightmareMode && ballistas.Count > 0)
        {
            RaiseTowerForNightmare();

            // Raise all ballistas so the FPS viewpoint is higher
            foreach (var b in ballistas)
            {
                var pos = b.transform.position;
                b.transform.position = new Vector3(pos.x, pos.y + NIGHTMARE_HEIGHT_BOOST, pos.z);
                Debug.Log($"[BallistaManager] Raised ballista {b.name} to Y={b.transform.position.y}");
            }

            ballistas[0].transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            Debug.Log($"[BallistaManager] Nightmare mode — raised ballistas by {NIGHTMARE_HEIGHT_BOOST}. First ballista facing west.");

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

    /// <summary>
    /// Finds the central tower model in the scene and scales it taller for Nightmare mode,
    /// so the raised ballista position looks visually correct from the build-mode overhead view.
    /// </summary>
    private void RaiseTowerForNightmare()
    {
        // Try to find the tower by name first
        GameObject tower = GameObject.Find("Tower");

        // Fallback: search for any mesh object near fortress center
        if (tower == null)
        {
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (mr.gameObject.name.IndexOf("Tower", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && Vector3.Distance(mr.transform.position, GameManager.FortressCenter) < 2f)
                {
                    tower = mr.transform.root.gameObject;
                    break;
                }
            }
        }

        if (tower != null)
        {
            // Scale Y to stretch the tower taller — keeps XZ the same
            const float ORIGINAL_TOWER_HEIGHT = 3.3f;
            float scaleMultiplier = (ORIGINAL_TOWER_HEIGHT + NIGHTMARE_HEIGHT_BOOST) / ORIGINAL_TOWER_HEIGHT;
            var scale = tower.transform.localScale;
            scale.y *= scaleMultiplier;
            tower.transform.localScale = scale;
            Debug.Log($"[BallistaManager] Raised tower model '{tower.name}' scale Y to {scale.y:F2} (x{scaleMultiplier:F2}).");
        }
        else
        {
            Debug.LogWarning("[BallistaManager] Could not find tower model to raise for Nightmare mode.");
        }
    }
}
