using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI treasureText;
    [SerializeField] private TextMeshProUGUI menialText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI killsText;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseButtonIcon;

    private bool subscribed;
    private Sprite pauseSprite;
    private Sprite playSprite;

    private void Start()
    {
        GenerateIcons();
        TrySubscribe();
    }

    private void OnEnable()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
    }

    private void OnDisable()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseClicked);
    }

    private void TrySubscribe()
    {
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged += UpdateTreasure;
            GameManager.Instance.OnMenialsChanged += UpdateMenials;
            GameManager.Instance.OnKillsChanged += UpdateKills;
            GameManager.Instance.OnPauseChanged += UpdatePauseButton;
            subscribed = true;

            // Force initial display update
            UpdateTreasure(GameManager.Instance.Treasure);
            UpdateMenials(GameManager.Instance.MenialCount);
            UpdateKills(GameManager.Instance.EnemyKills);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged -= UpdateTreasure;
            GameManager.Instance.OnMenialsChanged -= UpdateMenials;
            GameManager.Instance.OnKillsChanged -= UpdateKills;
            GameManager.Instance.OnPauseChanged -= UpdatePauseButton;
        }
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        if (GameManager.Instance == null) return;

        // SPACE to toggle pause
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GameManager.Instance.TogglePause();
        }

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

    private void UpdateKills(int amount)
    {
        if (killsText != null)
            killsText.text = "Kills: " + amount;
    }

    private void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
    }

    private void UpdatePauseButton(bool isPaused)
    {
        if (pauseButtonIcon != null && playSprite != null && pauseSprite != null)
            pauseButtonIcon.sprite = isPaused ? playSprite : pauseSprite;
    }

    private void GenerateIcons()
    {
        pauseSprite = CreateIcon(DrawPause);
        playSprite = CreateIcon(DrawPlay);
        // Set initial state
        if (pauseButtonIcon != null)
            pauseButtonIcon.sprite = pauseSprite;
        Debug.Log("[GameHUD] Play/pause icons generated.");
    }

    private static Sprite CreateIcon(System.Action<Color32[], int> drawFunc)
    {
        int size = 32;
        var pixels = new Color32[size * size];
        // Fill transparent
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        drawFunc(pixels, size);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void DrawPause(Color32[] pixels, int size)
    {
        var white = new Color32(255, 255, 255, 255);
        int pad = 8;
        int barW = 4;
        int gap = 4;
        int cx = size / 2;
        int leftX = cx - gap / 2 - barW;
        int rightX = cx + gap / 2;

        for (int y = pad; y < size - pad; y++)
        {
            for (int x = leftX; x < leftX + barW; x++)
                pixels[y * size + x] = white;
            for (int x = rightX; x < rightX + barW; x++)
                pixels[y * size + x] = white;
        }
    }

    private static void DrawPlay(Color32[] pixels, int size)
    {
        var white = new Color32(255, 255, 255, 255);
        int pad = 7;
        int left = 11;
        int right = 23;
        float cy = size / 2f;
        float halfH = (size - 2 * pad) / 2f;

        for (int y = pad; y < size - pad; y++)
        {
            float t = Mathf.Abs(y - cy) / halfH;
            int rowRight = Mathf.RoundToInt(Mathf.Lerp(right, left, t));
            for (int x = left; x <= rowRight; x++)
                pixels[y * size + x] = white;
        }
    }
}
