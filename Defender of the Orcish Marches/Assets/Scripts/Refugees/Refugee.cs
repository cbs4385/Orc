using System.Collections;
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
    [Header("Model Override")]
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private RuntimeAnimatorController animatorController;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private bool isDead;
    private bool arrived;

    private RefugeePowerUp carriedPowerUp = RefugeePowerUp.None;
    private Renderer[] bodyRenderers;
    private Color[] originalColors;

    public RefugeePowerUp CarriedPowerUp => carriedPowerUp;
    public bool HasPowerUp => carriedPowerUp != RefugeePowerUp.None;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        currentHP = maxHP;

        // Swap model if a custom model is assigned
        if (modelPrefab != null)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var newModel = Instantiate(modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = newModel.AddComponent<Animator>();
            if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;

            Debug.Log("[Refugee] Custom model loaded");
        }

        // Cache all renderers and their original colors for power-up tinting
        bodyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            originalColors[i] = bodyRenderers[i].material.color;
        }
    }

    private void Start()
    {
        // Navigate to tower center
        if (agent.isOnNavMesh)
            agent.SetDestination(GameManager.FortressCenter);
    }

    public void SetPowerUp(RefugeePowerUp powerUp)
    {
        carriedPowerUp = powerUp;

        // Visual indicator - power-up refugees tint all renderers
        if (bodyRenderers != null && powerUp != RefugeePowerUp.None)
        {
            Color tint = Color.white;
            switch (powerUp)
            {
                case RefugeePowerUp.DoubleShot:
                    tint = new Color(1f, 0.85f, 0f); // Gold
                    break;
                case RefugeePowerUp.BurstDamage:
                    tint = new Color(1f, 0.4f, 0.1f); // Orange-red
                    break;
            }
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].material.color = tint;
            }
        }
    }

    private void Update()
    {
        if (isDead || arrived) return;

        if (!agent.pathPending && agent.remainingDistance < 2f)
        {
            arrived = true;
            Debug.Log($"[Refugee] Arrived at fortress center. Position={transform.position}");

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
                var newMenial = MenialManager.Instance.SpawnMenial();
                if (newMenial == null)
                    Debug.LogError("[Refugee] SpawnMenial returned null! Count incremented but no menial created.");
            }
            else
            {
                Debug.LogError("[Refugee] MenialManager.Instance is null! Count incremented but no menial spawned.");
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
        FloatingDamageNumber.Spawn(transform.position, damage, false);
        if (currentHP <= 0)
        {
            isDead = true;
            Debug.Log($"[Refugee] Died at {transform.position}");

            if (animator != null)
            {
                animator.SetTrigger("Die");
                if (agent != null) agent.enabled = false;
                StartCoroutine(DestroyAfterAnimation());
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
