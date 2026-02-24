using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TooltipSystem : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Vector3 offset = new Vector3(15, -15, 0);

    private UnityEngine.Camera mainCam;

    // Hover magnification
    private const float HOVER_SCALE = 4f;
    private Transform hoveredTransform;
    private Vector3 originalScale;

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (mainCam == null || Mouse.current == null) return;

        // Raycast from mouse to find hoverable objects
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Check for hover magnification on characters
            Transform magnifyTarget = GetMagnifyTarget(hit.collider.gameObject);
            if (magnifyTarget != null)
            {
                if (magnifyTarget != hoveredTransform)
                {
                    RestoreHovered();
                    hoveredTransform = magnifyTarget;
                    originalScale = magnifyTarget.localScale;
                    magnifyTarget.localScale = originalScale * HOVER_SCALE;
                    Debug.Log($"[TooltipSystem] Hover magnify: {magnifyTarget.name} at {magnifyTarget.position}");
                }
            }
            else
            {
                RestoreHovered();
            }

            string tooltip = GetTooltipForObject(hit.collider.gameObject);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShowTooltip(tooltip, mousePos);
                return;
            }
        }
        else
        {
            RestoreHovered();
        }

        HideTooltip();
    }

    private Transform GetMagnifyTarget(GameObject obj)
    {
        var enemy = obj.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead) return enemy.transform;

        var defender = obj.GetComponentInParent<Defender>();
        if (defender != null && !defender.IsDead) return defender.transform;

        var menial = obj.GetComponentInParent<Menial>();
        if (menial != null && !menial.IsDead) return menial.transform;

        var refugee = obj.GetComponentInParent<Refugee>();
        if (refugee != null) return refugee.transform;

        return null;
    }

    private void RestoreHovered()
    {
        if (hoveredTransform != null)
        {
            hoveredTransform.localScale = originalScale;
            hoveredTransform = null;
        }
    }

    private void OnDisable()
    {
        RestoreHovered();
    }

    private string GetTooltipForObject(GameObject obj)
    {
        var enemy = obj.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            return string.Format("{0}\nHP: {1}/{2}", enemy.Data.enemyName, enemy.CurrentHP, enemy.Data.maxHP);
        }

        var wall = obj.GetComponent<Wall>();
        if (wall != null && !wall.IsDestroyed)
        {
            return string.Format("Wall\nHP: {0}/{1}", wall.CurrentHP, wall.MaxHP);
        }

        var loot = obj.GetComponent<TreasurePickup>();
        if (loot != null && !loot.IsCollected)
        {
            return string.Format("Treasure: {0} gold", loot.Value);
        }

        var menial = obj.GetComponent<Menial>();
        if (menial != null && !menial.IsDead)
        {
            return string.Format("Menial ({0})", menial.CurrentState);
        }

        return null;
    }

    private void ShowTooltip(string text, Vector2 mousePos)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        tooltipPanel.SetActive(true);
        tooltipText.text = text;
        tooltipPanel.transform.position = new Vector3(mousePos.x, mousePos.y, 0) + offset;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}
