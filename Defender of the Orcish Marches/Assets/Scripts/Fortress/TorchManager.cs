using System.Collections.Generic;
using UnityEngine;

public class TorchManager : MonoBehaviour
{
    public static TorchManager Instance { get; private set; }

    private const int COURTYARD_TORCH_COUNT = 6;
    private const float COURTYARD_TORCH_RADIUS = 4.0f;
    private const float COURTYARD_TORCH_HEIGHT = 2.0f;
    private const float TOWER_TORCH_OFFSET = 0.5f;
    private const float TOWER_TORCH_HEIGHT = 2.5f;

    private List<GameObject> torches = new List<GameObject>();
    private Material flameMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;

        var dnc = FindAnyObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            dnc.OnNightStarted += HandleNightStarted;
            dnc.OnDayStarted += HandleDayStarted;
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;

        var dnc = FindAnyObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            dnc.OnNightStarted -= HandleNightStarted;
            dnc.OnDayStarted -= HandleDayStarted;
        }
    }

    private void Start()
    {
        CreateFlameMaterial();
        CreateTorches();

        // If it's already night when we start, enable immediately
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
        {
            SetTorchesActive(true);
            Debug.Log("[TorchManager] Started during night — torches enabled immediately.");
        }
        else
        {
            SetTorchesActive(false);
            Debug.Log($"[TorchManager] Initialized with {torches.Count} torches (disabled until night).");
        }
    }

    private void CreateFlameMaterial()
    {
        flameMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        flameMaterial.EnableKeyword("_EMISSION");
        flameMaterial.SetColor("_BaseColor", new Color(1f, 0.6f, 0.2f));
        flameMaterial.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 3f);
        flameMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    private void CreateTorches()
    {
        Vector3 fc = GameManager.FortressCenter;

        // Courtyard torches — evenly spaced around the wall ring
        for (int i = 0; i < COURTYARD_TORCH_COUNT; i++)
        {
            float angleDeg = i * (360f / COURTYARD_TORCH_COUNT);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 pos = fc + new Vector3(
                Mathf.Cos(angleRad) * COURTYARD_TORCH_RADIUS,
                COURTYARD_TORCH_HEIGHT,
                Mathf.Sin(angleRad) * COURTYARD_TORCH_RADIUS
            );
            CreateTorch($"CourtyardTorch_{i}", pos, 7f, 2f);
        }

        // Tower torches — flanking the tower
        CreateTorch("TowerTorch_Left", fc + new Vector3(-TOWER_TORCH_OFFSET, TOWER_TORCH_HEIGHT, TOWER_TORCH_OFFSET), 6f, 1.5f);
        CreateTorch("TowerTorch_Right", fc + new Vector3(TOWER_TORCH_OFFSET, TOWER_TORCH_HEIGHT, -TOWER_TORCH_OFFSET), 6f, 1.5f);

        Debug.Log($"[TorchManager] Created {torches.Count} torches ({COURTYARD_TORCH_COUNT} courtyard + 2 tower).");
    }

    private void CreateTorch(string torchName, Vector3 position, float lightRange, float lightIntensity)
    {
        var torchGO = new GameObject(torchName);
        torchGO.transform.SetParent(transform);
        torchGO.transform.position = position;

        // Point light
        var light = torchGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.2f);
        light.range = lightRange;
        light.intensity = lightIntensity;
        light.shadows = LightShadows.None;

        // Flame sphere (small emissive orb)
        var flameGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flameGO.name = "Flame";
        flameGO.transform.SetParent(torchGO.transform, false);
        flameGO.transform.localPosition = Vector3.zero;
        flameGO.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);

        // Remove collider — flame shouldn't block anything
        var col = flameGO.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = flameGO.GetComponent<Renderer>();
        if (rend != null && flameMaterial != null)
        {
            rend.material = flameMaterial;
        }

        torches.Add(torchGO);
    }

    private void HandleNightStarted()
    {
        SetTorchesActive(true);
        Debug.Log("[TorchManager] Night started — torches enabled.");
    }

    private void HandleDayStarted()
    {
        SetTorchesActive(false);
        Debug.Log("[TorchManager] Day started — torches disabled.");
    }

    private void SetTorchesActive(bool active)
    {
        // Remove destroyed torches (e.g. from walls that were destroyed)
        torches.RemoveAll(t => t == null);

        foreach (var torch in torches)
        {
            torch.SetActive(active);
        }
    }

    /// <summary>
    /// Creates torches at both tower endpoints of a dynamically placed wall.
    /// Torches are parented to the wall so they auto-destroy when the wall is removed.
    /// </summary>
    public void AddWallTorches(Wall wall)
    {
        if (wall == null) return;

        var corners = wall.GetComponent<WallCorners>();
        if (corners == null)
        {
            Debug.LogWarning($"[TorchManager] Wall {wall.name} has no WallCorners — skipping torches.");
            return;
        }

        // Create flame material if not yet initialized (possible if called before Start)
        if (flameMaterial == null) CreateFlameMaterial();

        Vector3 nearEnd = corners.GetEndCenter(-1);
        Vector3 farEnd = corners.GetEndCenter(1);

        var torchNear = CreateTorchOnWall($"WallTorch_{wall.name}_Near",
            nearEnd + Vector3.up * TOWER_TORCH_HEIGHT, 6f, 1.5f, wall.transform);
        var torchFar = CreateTorchOnWall($"WallTorch_{wall.name}_Far",
            farEnd + Vector3.up * TOWER_TORCH_HEIGHT, 6f, 1.5f, wall.transform);

        torches.Add(torchNear);
        torches.Add(torchFar);

        // Match current day/night state
        bool isNight = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        torchNear.SetActive(isNight);
        torchFar.SetActive(isNight);

        Debug.Log($"[TorchManager] Added 2 torches to wall {wall.name} at {nearEnd}, {farEnd}. Night={isNight}.");
    }

    private GameObject CreateTorchOnWall(string torchName, Vector3 worldPosition, float lightRange, float lightIntensity, Transform parent)
    {
        var torchGO = new GameObject(torchName);
        torchGO.transform.SetParent(parent);
        torchGO.transform.position = worldPosition;

        // Point light
        var light = torchGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.2f);
        light.range = lightRange;
        light.intensity = lightIntensity;
        light.shadows = LightShadows.None;

        // Flame sphere (small emissive orb)
        var flameGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flameGO.name = "Flame";
        flameGO.transform.SetParent(torchGO.transform, false);
        flameGO.transform.localPosition = Vector3.zero;
        flameGO.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);

        // Remove collider — flame shouldn't block anything
        var col = flameGO.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = flameGO.GetComponent<Renderer>();
        if (rend != null && flameMaterial != null)
        {
            rend.material = flameMaterial;
        }

        return torchGO;
    }
}
