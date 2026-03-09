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
    [SerializeField] private int maxTransactionQuantity = 1;

    [Header("Envanter Hedefi")]
    [SerializeField] private ShopProductStorageType storageType = ShopProductStorageType.None;
    [SerializeField] private ItemSO consumableItem;
    [SerializeField] private PlaceableData buildPlaceableData;

    [Header("Toplu Fiyatlandırma")]
    [SerializeField] private ShopPriceTier[] priceTiers;

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
    /// Tek seferde alınabilecek veya satılabilecek maksimum adet.
    /// Şimdiden eklenmesi ileride UI spinner veya adet seçimi eklendiğinde fayda sağlar.
    /// </summary>
    public int MaxTransactionQuantity => maxTransactionQuantity;

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
    public PlaceableData BuildPlaceableData => buildPlaceableData;

    public ShopPriceTier[] PriceTiers => priceTiers;

    /// <summary>
    /// Girilen adede göre hangi fiyat kademesinin geçerli olduğunu hesaplar.
    /// Örneğin 1, 10, 100, 1000 gibi kademe yapıları burada yönetilir.
    /// </summary>
    public ShopPriceTier GetBestPriceTier(int quantity)
    {
        ShopPriceTier bestTier = null;

        if (priceTiers == null)
        {
            return null;
        }

        for (int i = 0; i < priceTiers.Length; i++)
        {
            ShopPriceTier currentTier = priceTiers[i];
            if (currentTier == null)
            {
                continue;
            }

            if (currentTier.MinimumQuantity <= quantity)
            {
                if (bestTier == null || currentTier.MinimumQuantity > bestTier.MinimumQuantity)
                {
                    bestTier = currentTier;
                }
            }
        }

        return bestTier;
    }

    public int GetBuyUnitPrice(int quantity)
    {
        ShopPriceTier tier = GetBestPriceTier(quantity);
        if (tier == null || tier.BuyUnitPrice <= 0)
        {
            return buyPrice;
        }

        return tier.BuyUnitPrice;
    }

    public int GetSellUnitPrice(int quantity)
    {
        ShopPriceTier tier = GetBestPriceTier(quantity);
        if (tier == null || tier.SellUnitPrice < 0)
        {
            return sellPrice;
        }

        return tier.SellUnitPrice;
    }

    public int GetBuyTotalPrice(int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return GetBuyUnitPrice(quantity) * quantity;
    }

    public int GetSellTotalPrice(int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return GetSellUnitPrice(quantity) * quantity;
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

        if (maxTransactionQuantity < 1)
        {
            maxTransactionQuantity = 1;
        }

        if (priceTiers == null)
        {
            return;
        }

        for (int i = 0; i < priceTiers.Length; i++)
        {
            ShopPriceTier tier = priceTiers[i];
            if (tier == null)
            {
                continue;
            }

            tier.ClampValues();
        }
    }
}
