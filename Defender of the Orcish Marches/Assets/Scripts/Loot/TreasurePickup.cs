using UnityEngine;

public class TreasurePickup : MonoBehaviour
{
    [SerializeField] private int value = 10;
    private bool collected;

    public int Value => value;
    public bool IsCollected => collected;

    public void Initialize(int treasureValue)
    {
        value = treasureValue;
    }

    private void Update()
    {
        // Rotate the loot for visual effect
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        Destroy(gameObject);
    }
}
