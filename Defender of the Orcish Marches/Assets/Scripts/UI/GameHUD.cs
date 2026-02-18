using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI treasureText;
    [SerializeField] private TextMeshProUGUI menialText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged += UpdateTreasure;
            GameManager.Instance.OnMenialsChanged += UpdateMenials;
            subscribed = true;

            // Force initial display update
            UpdateTreasure(GameManager.Instance.Treasure);
            UpdateMenials(GameManager.Instance.MenialCount);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged -= UpdateTreasure;
            GameManager.Instance.OnMenialsChanged -= UpdateMenials;
        }
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        if (GameManager.Instance == null) return;

        // Update timer
        if (timerText != null)
        {
            float time = GameManager.Instance.GameTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }

        // Update enemy count
        if (enemyCountText != null && EnemySpawnManager.Instance != null)
        {
            enemyCountText.text = "Enemies: " + EnemySpawnManager.Instance.GetActiveEnemyCount();
        }
    }

    private void UpdateTreasure(int amount)
    {
        if (treasureText != null)
            treasureText.text = "Gold: " + amount;
    }

    private void UpdateMenials(int amount)
    {
        if (menialText != null)
        {
            int idle = GameManager.Instance != null ? GameManager.Instance.IdleMenialCount : 0;
            menialText.text = string.Format("Menials: {0}/{1}", idle, amount);
        }
    }
}
