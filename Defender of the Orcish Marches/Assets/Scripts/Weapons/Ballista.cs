using UnityEngine;
using UnityEngine.InputSystem;

public class Ballista : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float maxRange = 30f;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    private float fireCooldown;
    private UnityEngine.Camera mainCam;

    // Power-up state
    private bool hasDoubleShot;
    private bool hasBurstDamage;
    private float doubleShotSpreadAngle = 8f;
    private float burstDamageRadius = 3f;

    public int Damage => damage;
    public float FireRate => fireRate;
    public bool HasDoubleShot => hasDoubleShot;
    public bool HasBurstDamage => hasBurstDamage;

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;
        if (firePoint == null) firePoint = transform;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (mainCam == null) { mainCam = UnityEngine.Camera.main; return; }

        fireCooldown -= Time.deltaTime;

        RotateTowardsMouse();

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && fireCooldown <= 0f)
        {
            Fire();
        }
    }

    private void RotateTowardsMouse()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 direction = mouseWorld - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    private void Fire()
    {
        fireCooldown = 1f / fireRate;

        if (projectilePrefab == null) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 direction = (mouseWorld - firePoint.position);
        direction.y = 0;
        direction.Normalize();

        Vector3 spawnPos = new Vector3(firePoint.position.x, 0.5f, firePoint.position.z);

        // Fire main projectile
        SpawnProjectile(spawnPos, direction);

        // Double shot: fire a second projectile at a slight angle
        if (hasDoubleShot)
        {
            Vector3 spreadDir = Quaternion.Euler(0, doubleShotSpreadAngle, 0) * direction;
            SpawnProjectile(spawnPos, spreadDir);
        }
    }

    private void SpawnProjectile(Vector3 position, Vector3 direction)
    {
        GameObject proj = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        var projectile = proj.GetComponent<BallistaProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, projectileSpeed, damage, maxRange, hasBurstDamage, burstDamageRadius);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position + transform.forward * 10f;
    }

    public void UpgradeDamage(int amount) => damage += amount;
    public void UpgradeFireRate(float amount) => fireRate += amount;
    public void EnableDoubleShot() => hasDoubleShot = true;
    public void EnableBurstDamage() => hasBurstDamage = true;
}
