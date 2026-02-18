using UnityEngine;
using UnityEngine.AI;

public enum RefugeePowerUp
{
    None,
    DoubleShot,
    BurstDamage
}

[RequireComponent(typeof(NavMeshAgent))]
public class Refugee : MonoBehaviour
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private float moveSpeed = 3.5f;

    private NavMeshAgent agent;
    private int currentHP;
    private bool isDead;
    private bool arrived;

    private RefugeePowerUp carriedPowerUp = RefugeePowerUp.None;
    private Renderer bodyRenderer;

    public RefugeePowerUp CarriedPowerUp => carriedPowerUp;
    public bool HasPowerUp => carriedPowerUp != RefugeePowerUp.None;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        currentHP = maxHP;
        bodyRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        // Navigate to tower center
        if (agent.isOnNavMesh)
            agent.SetDestination(Vector3.zero);
    }

    public void SetPowerUp(RefugeePowerUp powerUp)
    {
        carriedPowerUp = powerUp;

        // Visual indicator - power-up refugees glow a different color
        if (bodyRenderer != null && powerUp != RefugeePowerUp.None)
        {
            switch (powerUp)
            {
                case RefugeePowerUp.DoubleShot:
                    bodyRenderer.material.color = new Color(1f, 0.85f, 0f); // Gold
                    break;
                case RefugeePowerUp.BurstDamage:
                    bodyRenderer.material.color = new Color(1f, 0.4f, 0.1f); // Orange-red
                    break;
            }
        }
    }

    private void Update()
    {
        if (isDead || arrived) return;

        if (!agent.pathPending && agent.remainingDistance < 2f)
        {
            arrived = true;

            // Apply power-up to the active ballista
            if (carriedPowerUp != RefugeePowerUp.None && BallistaManager.Instance != null)
            {
                var ballista = BallistaManager.Instance.ActiveBallista;
                if (ballista != null)
                {
                    ApplyPowerUp(ballista);
                }
            }

            // Also add a menial
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMenial();
            }
            if (MenialManager.Instance != null)
            {
                MenialManager.Instance.SpawnMenial();
            }

            Destroy(gameObject);
        }
    }

    private void ApplyPowerUp(Ballista ballista)
    {
        switch (carriedPowerUp)
        {
            case RefugeePowerUp.DoubleShot:
                ballista.EnableDoubleShot();
                Debug.Log("Power-up acquired: Double Shot!");
                break;
            case RefugeePowerUp.BurstDamage:
                ballista.EnableBurstDamage();
                Debug.Log("Power-up acquired: Burst Damage!");
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHP -= damage;
        if (currentHP <= 0)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }
}
