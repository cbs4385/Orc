using UnityEngine;
using UnityEngine.InputSystem;

public class RecallManager : MonoBehaviour
{
    public static RecallManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[RecallManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[RecallManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Don't recall during build mode
        if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode) return;

        bool middleClick = Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        bool rKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        if (middleClick || rKey)
        {
            RecallAll();
        }
    }

    private void RecallAll()
    {
        // Play recall sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayRecall();

        // Recall all defenders
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        int defenderCount = 0;
        foreach (var d in defenders)
        {
            if (d != null && !d.IsDead)
            {
                d.Recall();
                defenderCount++;
            }
        }

        // Recall all menials
        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        int menialCount = 0;
        foreach (var m in menials)
        {
            if (m != null && !m.IsDead)
            {
                m.Recall();
                menialCount++;
            }
        }

        Debug.Log($"[RecallManager] Recall triggered! {defenderCount} defenders and {menialCount} menials returning to courtyard.");
    }
}
