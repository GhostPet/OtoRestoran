using UnityEngine;

/// <summary>
/// Oyun sırasında tüketilen veya kullanılan temel malzeme verisini tutar.
/// Örnek: ekmek, köfte, sos, peynir gibi mutfak malzemeleri.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "OtoRestoran/Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] [TextArea(2, 4)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private int maxStack = 9999;

    public string ItemId => itemId;

    public string DisplayName => displayName;

    public string Description => description;

    public Sprite Icon => icon;

    public int MaxStack => maxStack;

    private void OnValidate()
    {
        if (maxStack < 1)
        {
            maxStack = 1;
        }
    }
}
