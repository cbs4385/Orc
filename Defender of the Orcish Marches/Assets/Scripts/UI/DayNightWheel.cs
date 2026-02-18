using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayNightWheel : MonoBehaviour
{
    [SerializeField] private Image wheelImage;
    [SerializeField] private TextMeshProUGUI dayNumberText;

    private Sprite wheelSprite;

    private void Awake()
    {
        GenerateWheelTexture();
        Debug.Log("[DayNightWheel] Wheel texture generated and applied.");
    }

    private void Update()
    {
        if (DayNightCycle.Instance == null) return;

        float progress = DayNightCycle.Instance.PhaseProgress;
        float rotation;

        if (DayNightCycle.Instance.IsDay)
        {
            // Day: sun at top at noon (progress=0.5 → rotation=360°)
            rotation = 270f + progress * 180f;
        }
        else
        {
            // Night: moon at top at midnight (progress=0.5 → rotation=180°)
            rotation = 90f + progress * 180f;
        }

        wheelImage.transform.localEulerAngles = new Vector3(0f, 0f, rotation);

        UpdateDayText();
    }

    private void UpdateDayText()
    {
        if (dayNumberText == null) return;
        dayNumberText.text = "Day " + DayNightCycle.Instance.DayNumber;
    }

    private void GenerateWheelTexture()
    {
        int size = 128;
        float center = size / 2f;
        float radius = center - 1f; // 1px padding for clean edges

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color dayColor = new Color(1f, 0.816f, 0.251f);       // #FFD040
        Color daySunColor = new Color(1f, 0.92f, 0.5f);        // lighter yellow sun
        Color nightColor = new Color(0.102f, 0.125f, 0.251f);  // #1A2040
        Color moonColor = new Color(0.7f, 0.75f, 0.9f);        // pale moon
        Color starColor = new Color(0.9f, 0.9f, 1f);           // white-ish stars
        Color transparent = new Color(0, 0, 0, 0);

        // Star positions (normalized 0..1 within the night half)
        Vector2[] starPositions = new Vector2[]
        {
            new Vector2(0.2f, 0.25f),
            new Vector2(0.75f, 0.15f),
            new Vector2(0.5f, 0.35f),
            new Vector2(0.35f, 0.1f),
            new Vector2(0.8f, 0.3f),
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outside circle = transparent
                if (dist > radius)
                {
                    tex.SetPixel(x, y, transparent);
                    continue;
                }

                // Top half = day (y >= center), bottom half = night (y < center)
                if (y >= center)
                {
                    // Day half - check if pixel is within sun circle
                    float sunCX = center;
                    float sunCY = center + radius * 0.4f;
                    float sunR = radius * 0.25f;
                    float sunDist = Mathf.Sqrt((x - sunCX) * (x - sunCX) + (y - sunCY) * (y - sunCY));

                    if (sunDist < sunR)
                        tex.SetPixel(x, y, daySunColor);
                    else
                        tex.SetPixel(x, y, dayColor);
                }
                else
                {
                    // Night half - check for crescent moon and stars
                    float moonCX = center;
                    float moonCY = center - radius * 0.35f;
                    float moonR = radius * 0.22f;
                    float moonDist = Mathf.Sqrt((x - moonCX) * (x - moonCX) + (y - moonCY) * (y - moonCY));

                    // Crescent: main circle minus offset circle
                    float moonCutCX = moonCX + moonR * 0.6f;
                    float moonCutCY = moonCY + moonR * 0.3f;
                    float moonCutDist = Mathf.Sqrt((x - moonCutCX) * (x - moonCutCX) + (y - moonCutCY) * (y - moonCutCY));

                    if (moonDist < moonR && moonCutDist >= moonR * 0.9f)
                    {
                        tex.SetPixel(x, y, moonColor);
                    }
                    else
                    {
                        // Check stars
                        bool isStar = false;
                        float nightHalfHeight = center;
                        for (int s = 0; s < starPositions.Length; s++)
                        {
                            float sx = starPositions[s].x * size;
                            float sy = starPositions[s].y * nightHalfHeight;
                            float sd = Mathf.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy));
                            if (sd < 1.5f)
                            {
                                isStar = true;
                                break;
                            }
                        }

                        tex.SetPixel(x, y, isStar ? starColor : nightColor);
                    }
                }
            }
        }

        tex.Apply();

        wheelSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        if (wheelImage != null)
            wheelImage.sprite = wheelSprite;
    }
}
