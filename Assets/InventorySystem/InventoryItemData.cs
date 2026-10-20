using UnityEngine;

/// <summary>
/// Read-only definition for an inventory item. Counts are deliberately stored
/// in FarmFund so they survive scene loads and cannot disagree with gameplay.
/// This is the persistent adaptation of the supplied inventory example.
/// </summary>
[CreateAssetMenu(
    fileName = "InventoryItemData",
    menuName = "Farming Game/Inventory Item Data"
)]
public sealed class InventoryItemData : ScriptableObject
{
    [Header("Identity")]
    public FarmInventoryItemId itemId;
    public string itemName;

    [Header("Economy")]
    public int buyPrice;
    public int sellPrice;

    [Header("UI")]
    public Sprite itemSprite;
    public bool stackable = true;
}
