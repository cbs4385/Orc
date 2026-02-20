using UnityEngine;
using TMPro;

public class FloatingDamageNumber : MonoBehaviour
{
    private static readonly Color enemyDamageColor = new Color(1f, 1f, 0.4f);    // Yellow — damage dealt to enemies
    private static readonly Color friendlyDamageColor = new Color(1f, 0.3f, 0.3f); // Red — damage taken by friendlies
    private static readonly Color healColor = new Color(0.3f, 1f, 0.4f);          // Green — healing

    private float lifetime;
    private float timer;
    private float riseSpeed;
    private TextMeshPro tmp;
    private Color startColor;
    private Transform cam;

    /// <summary>
    /// Spawn a floating damage number at a world position.
    /// isEnemyDamage=true for damage dealt TO enemies (yellow), false for damage taken BY friendlies (red).
    /// </summary>
    public static void Spawn(Vector3 position, int amount, bool isEnemyDamage)
    {
        var go = new GameObject("DmgNum");
        // Offset upward so it doesn't overlap the entity, plus small random X to avoid stacking
        go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, 0f);

        var fdn = go.AddComponent<FloatingDamageNumber>();
        fdn.lifetime = 0.8f;
        fdn.riseSpeed = 2f;
        fdn.startColor = isEnemyDamage ? enemyDamageColor : friendlyDamageColor;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = amount.ToString();
        tmp.fontSize = 6;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = fdn.startColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;
        // Auto-size the rect to fit text
        tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);
        fdn.tmp = tmp;
    }

    /// <summary>
    /// Spawn a green healing number.
    /// </summary>
    public static void SpawnHeal(Vector3 position, int amount)
    {
        var go = new GameObject("HealNum");
        go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, 0f);

        var fdn = go.AddComponent<FloatingDamageNumber>();
        fdn.lifetime = 0.8f;
        fdn.riseSpeed = 2f;
        fdn.startColor = healColor;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "+" + amount;
        tmp.fontSize = 6;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = fdn.startColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);
        fdn.tmp = tmp;
    }

    private void Start()
    {
        timer = lifetime;
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Rise
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Fade out
        float alpha = timer / lifetime;
        if (tmp != null)
        {
            var c = startColor;
            c.a = alpha;
            tmp.color = c;
        }

        // Billboard — face camera
        if (cam != null)
        {
            transform.rotation = cam.rotation;
        }
    }
}
