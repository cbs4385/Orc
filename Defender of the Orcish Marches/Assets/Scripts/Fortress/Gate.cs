using UnityEngine;
using UnityEngine.AI;

public class Gate : MonoBehaviour
{
    [SerializeField] private Transform leftDoorPivot;
    [SerializeField] private Transform rightDoorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 5f;
    [SerializeField] private float closeDelay = 1.5f;
    [SerializeField] private float detectionRange = 3f;

    private float currentOpenAmount;
    private float closeTimer;
    private float checkTimer;
    private bool shouldBeOpen;
    private Unity.AI.Navigation.NavMeshLink navMeshLink;
    private const float CHECK_INTERVAL = 0.2f;

    public bool IsOpen => currentOpenAmount > 0.5f;

    private void Awake()
    {
        navMeshLink = GetComponent<Unity.AI.Navigation.NavMeshLink>();
        // Start with link disabled - gate is closed
        if (navMeshLink != null)
            navMeshLink.enabled = false;
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0)
        {
            checkTimer = CHECK_INTERVAL;
            shouldBeOpen = CheckForFriendlies();
        }

        if (shouldBeOpen)
        {
            closeTimer = closeDelay;
        }
        else
        {
            closeTimer -= Time.deltaTime;
        }

        float target = (shouldBeOpen || closeTimer > 0) ? 1f : 0f;
        currentOpenAmount = Mathf.MoveTowards(currentOpenAmount, target, openSpeed * Time.deltaTime);
        ApplyRotation();

        // Enable NavMeshLink only when gate is sufficiently open
        if (navMeshLink != null)
        {
            navMeshLink.enabled = currentOpenAmount > 0.3f;
        }
    }

    private bool CheckForFriendlies()
    {
        Vector3 pos = transform.position;

        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var m in menials)
        {
            if (m.IsDead || m.CurrentState == MenialState.Idle) continue;
            if (Vector3.Distance(pos, m.transform.position) < detectionRange)
                return true;
        }

        var refugees = FindObjectsByType<Refugee>(FindObjectsSortMode.None);
        foreach (var r in refugees)
        {
            if (Vector3.Distance(pos, r.transform.position) < detectionRange)
                return true;
        }

        return false;
    }

    private void ApplyRotation()
    {
        float angle = currentOpenAmount * openAngle;
        if (leftDoorPivot != null)
            leftDoorPivot.localRotation = Quaternion.Euler(0, angle, 0);
        if (rightDoorPivot != null)
            rightDoorPivot.localRotation = Quaternion.Euler(0, -angle, 0);
    }
}
