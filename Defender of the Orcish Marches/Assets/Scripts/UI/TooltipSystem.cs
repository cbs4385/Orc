using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using static LocalizationManager;

public class TooltipSystem : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Vector3 offset = new Vector3(15, -15, 0);

    private UnityEngine.Camera mainCam;

    // Touch tap-and-hold tracking
    private float pointerHoldTime;
    private Vector2 pointerHoldStartPos;
    private const float TOUCH_HOLD_THRESHOLD = 0.4f;
    private const float TOUCH_MOVE_TOLERANCE = 20f; // pixels

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (mainCam == null) return;

        var pointer = PointerInputManager.Instance;
        if (pointer == null) return;

        Vector2 pointerPos = pointer.PointerPosition;

        // Touch: show tooltips on tap-and-hold only
        if (pointer.IsTouchActive)
        {
            if (pointer.WasPointerPressedThisFrame)
            {
                pointerHoldTime = 0f;
                pointerHoldStartPos = pointerPos;
            }

            if (pointer.IsPointerDown)
            {
                float drift = Vector2.Distance(pointerPos, pointerHoldStartPos);
                if (drift > TOUCH_MOVE_TOLERANCE)
                {
                    // Finger moved too far — reset hold and hide tooltip
                    pointerHoldTime = 0f;
                    HideTooltip();
                    return;
                }

                pointerHoldTime += Time.deltaTime;
                if (pointerHoldTime < TOUCH_HOLD_THRESHOLD)
                {
                    HideTooltip();
                    return;
                }
            }
            else
            {
                // Finger lifted — hide
                pointerHoldTime = 0f;
                HideTooltip();
                return;
            }
        }

        // Raycast from pointer position
        Ray ray = mainCam.ScreenPointToRay(new Vector3(pointerPos.x, pointerPos.y, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            string tooltip = GetTooltipForObject(hit.collider.gameObject);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShowTooltip(tooltip, pointerPos);
                return;
            }
        }

        HideTooltip();
    }

    private string GetTooltipForObject(GameObject obj)
    {
        var enemy = obj.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            return L("tooltip.enemy", BestiaryManager.GetLocalizedEnemyName(enemy.Data.enemyName), enemy.CurrentHP, enemy.Data.maxHP);
        }

        var wall = obj.GetComponent<Wall>();
        if (wall != null && !wall.IsDestroyed)
        {
            return L("tooltip.wall", wall.CurrentHP, wall.MaxHP);
        }

        var loot = obj.GetComponent<TreasurePickup>();
        if (loot != null && !loot.IsCollected)
        {
            return L("tooltip.treasure", loot.Value);
        }

        var menial = obj.GetComponent<Menial>();
        if (menial != null && !menial.IsDead)
        {
            return L("tooltip.menial", L($"menial.state.{menial.CurrentState.ToString().ToLower()}"));
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
