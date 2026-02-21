using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField] private List<UpgradeData> availableUpgrades = new List<UpgradeData>();
    [SerializeField] private GameObject engineerPrefab;
    [SerializeField] private GameObject pikemanPrefab;
    [SerializeField] private GameObject crossbowmanPrefab;
    [SerializeField] private GameObject wizardPrefab;

    public event Action<UpgradeData> OnUpgradePurchased;

    private Dictionary<UpgradeType, int> purchaseCounts = new Dictionary<UpgradeType, int>();

    public IReadOnlyList<UpgradeData> AvailableUpgrades => availableUpgrades;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[UpgradeManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[UpgradeManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    public bool CanPurchase(UpgradeData upgrade)
    {
        if (GameManager.Instance == null) return false;
        int count = GetPurchaseCount(upgrade.upgradeType);
        if (!upgrade.repeatable && count > 0) return false;

        // Walls are purchased via build mode, not the upgrade panel
        if (upgrade.upgradeType == UpgradeType.NewWall) return false;

        int scaledTreasure = upgrade.GetTreasureCost(count);
        int scaledMenial = upgrade.GetMenialCost(count);
        bool canAfford = GameManager.Instance.CanAfford(scaledTreasure, scaledMenial);
        return canAfford;
    }

    /// <summary>
    /// Returns the current scaled costs for the next purchase of this upgrade.
    /// </summary>
    public (int treasure, int menial) GetCurrentCost(UpgradeData upgrade)
    {
        int count = GetPurchaseCount(upgrade.upgradeType);
        return (upgrade.GetTreasureCost(count), upgrade.GetMenialCost(count));
    }

    public bool Purchase(UpgradeData upgrade)
    {
        var (scaledTreasure, scaledMenial) = GetCurrentCost(upgrade);
        Debug.Log($"[UpgradeManager] Purchase called: {upgrade.upgradeName} (type={upgrade.upgradeType}, scaledCost={scaledTreasure}g {scaledMenial}m, purchases={GetPurchaseCount(upgrade.upgradeType)})");

        if (!CanPurchase(upgrade))
        {
            Debug.Log($"[UpgradeManager] CanPurchase returned false for {upgrade.upgradeName}");
            return false;
        }

        if (!GameManager.Instance.SpendTreasure(scaledTreasure))
        {
            Debug.Log($"[UpgradeManager] SpendTreasure failed for {scaledTreasure}");
            return false;
        }

        Debug.Log($"[UpgradeManager] Spent {scaledTreasure}g. IsHire={IsHireUpgrade(upgrade.upgradeType)}, menialCost={scaledMenial}");

        // For hire upgrades, consume menials via walk-to-tower instead of instant removal
        if (scaledMenial > 0 && IsHireUpgrade(upgrade.upgradeType))
        {
            GameObject prefab = GetDefenderPrefab(upgrade.upgradeType);
            if (MenialManager.Instance == null || !MenialManager.Instance.ConsumeMenials(scaledMenial, () =>
            {
                // Callback: all menials entered the tower - spawn the hireling
                SpawnDefenderAtTower(prefab);
            }))
            {
                // Failed to consume menials - refund
                GameManager.Instance.AddTreasure(scaledTreasure);
                return false;
            }

            Debug.Log($"[UpgradeManager] Hired {upgrade.upgradeName}: menials walking to tower.");
        }
        else
        {
            // Non-hire upgrades: spend menials instantly if needed
            if (scaledMenial > 0 && !GameManager.Instance.SpendMenials(scaledMenial))
            {
                GameManager.Instance.AddTreasure(scaledTreasure);
                return false;
            }
            Debug.Log($"[UpgradeManager] Calling ApplyNonHireUpgrade for {upgrade.upgradeName} (type={upgrade.upgradeType})");
            ApplyNonHireUpgrade(upgrade);
        }

        IncrementPurchaseCount(upgrade.upgradeType);
        OnUpgradePurchased?.Invoke(upgrade);
        return true;
    }

    private bool IsHireUpgrade(UpgradeType type)
    {
        return type == UpgradeType.SpawnEngineer || type == UpgradeType.SpawnPikeman ||
               type == UpgradeType.SpawnCrossbowman || type == UpgradeType.SpawnWizard;
    }

    private GameObject GetDefenderPrefab(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.SpawnEngineer: return engineerPrefab;
            case UpgradeType.SpawnPikeman: return pikemanPrefab;
            case UpgradeType.SpawnCrossbowman: return crossbowmanPrefab;
            case UpgradeType.SpawnWizard: return wizardPrefab;
            default: return null;
        }
    }

    private void ApplyNonHireUpgrade(UpgradeData upgrade)
    {
        Debug.Log($"[UpgradeManager] ApplyNonHireUpgrade entered: {upgrade.upgradeName}, type={upgrade.upgradeType} (int={(int)upgrade.upgradeType})");
        switch (upgrade.upgradeType)
        {
            case UpgradeType.NewBallista:
                if (BallistaManager.Instance != null)
                    BallistaManager.Instance.AddBallista();
                break;

            case UpgradeType.BallistaDamage:
                if (BallistaManager.Instance != null && BallistaManager.Instance.ActiveBallista != null)
                    BallistaManager.Instance.ActiveBallista.UpgradeDamage(10);
                break;

            case UpgradeType.BallistaFireRate:
                if (BallistaManager.Instance != null && BallistaManager.Instance.ActiveBallista != null)
                    BallistaManager.Instance.ActiveBallista.UpgradeFireRate(0.5f);
                break;

            default:
                Debug.LogWarning($"[UpgradeManager] ApplyNonHireUpgrade: unhandled upgradeType={upgrade.upgradeType}");
                break;
        }
    }

    private void SpawnDefenderAtTower(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[UpgradeManager] SpawnDefenderAtTower: prefab is null!");
            return;
        }

        // Spawn near the tower but not inside it - offset to a random courtyard position
        Vector3 fc = GameManager.FortressCenter;
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(2f, 3f);
        Vector3 spawnPos = fc + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

        // Ensure valid NavMesh position
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        var defender = go.GetComponent<Defender>();
        if (defender != null && defender.Data != null)
        {
            defender.Initialize(defender.Data);
            Debug.Log($"[UpgradeManager] {defender.Data.defenderName} emerged from the tower at {spawnPos}!");
        }
        else
        {
            Debug.LogWarning($"[UpgradeManager] Spawned defender at tower but no DefenderData assigned!");
        }
    }

    private int GetPurchaseCount(UpgradeType type)
    {
        return purchaseCounts.TryGetValue(type, out int count) ? count : 0;
    }

    public void IncrementPurchaseCountPublic(UpgradeType type)
    {
        IncrementPurchaseCount(type);
    }

    private void IncrementPurchaseCount(UpgradeType type)
    {
        if (!purchaseCounts.ContainsKey(type)) purchaseCounts[type] = 0;
        purchaseCounts[type]++;
    }
}
