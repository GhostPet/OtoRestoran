using UnityEngine;

/// <summary>
/// Shop'ta satılacak tek bir ürünün tüm temel verisini tutar.
/// Fiyat, prefab, ikon ve satış izinleri gibi bilgiler burada saklanır.
/// </summary>
[CreateAssetMenu(fileName = "ShopProduct", menuName = "OtoRestoran/Shop/Product Definition")]
public class ShopProductDefinitionSO : ScriptableObject
{
    [Header("Kimlik Bilgileri")]
    [SerializeField] private string productId;
    [SerializeField] private string displayName;
    [SerializeField] [TextArea(2, 5)] private string description;

    [Header("Görsel ve Dünya Objesi")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldPrefab;

    [Header("Fiyat Bilgileri")]
    [SerializeField] private int buyPrice = 100;
    [SerializeField] private int sellPrice = 50;

    [Header("İşlem Kuralları")]
    [SerializeField] private bool canBePurchased = true;
    [SerializeField] private bool canBeSold = true;
    [SerializeField] private int transactionQuantity = 1;

    [Header("Envanter Hedefi")]
    [SerializeField] private ShopProductStorageType storageType = ShopProductStorageType.None;
    [SerializeField] private ItemSO consumableItem;
	[SerializeField] private PlaceableObjectSO buildPlaceableData;

    /// <summary>
    /// Kod tarafında güvenilir ve sabit kimlik olarak kullanılabilir.
    /// Save sisteminde string id tutmak istenirse bu alan faydalıdır.
    /// </summary>
    public string ProductId => productId;

    /// <summary>
    /// UI'de kullanıcıya gösterilecek ürün adı.
    /// </summary>
    public string DisplayName => displayName;

    public string Description => description;

    public Sprite Icon => icon;

    /// <summary>
    /// İleride satın alım sonrası sahneye yerleştirilecek obje veya spawn edilecek varlık burada referanslanabilir.
    /// </summary>
    public GameObject WorldPrefab => worldPrefab;

    public int BuyPrice => buyPrice;

    public int SellPrice => sellPrice;

    public bool CanBePurchased => canBePurchased;

    public bool CanBeSold => canBeSold;

    /// <summary>
    /// Bu ürün satın alındığında veya satıldığında kaç adet hareket edeceğini belirtir.
    /// Örnek: 10'lu köfte paketi, 100'lü köfte paketi, 500'lü köfte paketi.
    /// </summary>
    public int TransactionQuantity => transactionQuantity;

    /// <summary>
    /// Ürün satın alındığında hangi sisteme gideceğini belirtir.
    /// Malzemeler normal inventory'e, yerleştirilebilir objeler build inventory'e yönlenir.
    /// </summary>
    public ShopProductStorageType StorageType => storageType;

    /// <summary>
    /// Bu ürün malzeme inventory'sine gidiyorsa karşılık gelen item verisini tutar.
    /// </summary>
    public ItemSO ConsumableItem => consumableItem;

    /// <summary>
    /// Bu ürün build inventory'ye gidiyorsa grid tarafında kullanılacak placeable verisini tutar.
    /// </summary>
	public PlaceableObjectSO BuildPlaceableData => buildPlaceableData;

    public int GetBuyUnitPrice(int quantity)
    {
        return buyPrice;
    }

    public int GetSellUnitPrice(int quantity)
    {
        return sellPrice;
    }

    public int GetBuyTotalPrice(int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return buyPrice;
    }

    public int GetSellTotalPrice(int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return sellPrice;
    }

    private void OnValidate()
    {
        if (buyPrice < 0)
        {
            buyPrice = 0;
        }

        if (sellPrice < 0)
        {
            sellPrice = 0;
        }

        if (transactionQuantity < 1)
        {
            transactionQuantity = 1;
        }
    }
}
