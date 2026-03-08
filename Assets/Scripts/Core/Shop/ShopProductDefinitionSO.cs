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
    }
}
