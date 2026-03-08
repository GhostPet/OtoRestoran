using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop sistemini sahnedeki UI ile bağlayan ana controller sınıfıdır.
/// Bu sınıfın görevi veriyi üretmek değil, mevcut ShopManager verisini ekrana taşımaktır.
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Header("Sistem Referansları")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private EconomyManager economyManager;

    [Header("Sekme UI")]
    [SerializeField] private Transform tabContainer;
    [SerializeField] private ShopTabButtonUI tabButtonPrefab;

    [Header("Ürün Liste UI")]
    [SerializeField] private Transform productContainer;
    [SerializeField] private ShopProductItemUI productItemPrefab;

    [Header("Bilgi Alanları")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text selectedTabTitleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private ScrollRect productScrollRect;

    private readonly List<ShopTabButtonUI> spawnedTabButtons = new List<ShopTabButtonUI>();
    private readonly List<ShopProductItemUI> spawnedProductItems = new List<ShopProductItemUI>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RebuildAllUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// Inspector üzerinden referans verilmediyse sahneden otomatik bulmayı dener.
    /// Küçük projelerde hız kazandırır; yine de inspector ataması yapmak daha güvenlidir.
    /// </summary>
    public void ResolveReferences()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }

        if (economyManager == null)
        {
            economyManager = FindObjectOfType<EconomyManager>();
        }
    }

    /// <summary>
    /// Sekmeleri, ürünleri ve bakiye yazısını baştan kurar.
    /// UI ilk açıldığında veya katalog değiştiğinde tekrar çağrılabilir.
    /// </summary>
    public void RebuildAllUI()
    {
        BuildTabButtons();
        RefreshProducts();
        RefreshBalance();
        RefreshSelectedTabTitle();
    }

    /// <summary>
    /// Sekme butonlarını shop kataloğundan dinamik olarak üretir.
    /// Böylece yeni sekme eklemek için sadece katalog asset'ini düzenlemek yeterli olur.
    /// </summary>
    public void BuildTabButtons()
    {
        ClearSpawnedTabButtons();

        if (shopManager == null)
        {
            SetMessage("ShopManager bulunamadı.");
            return;
        }

        if (tabContainer == null)
        {
            SetMessage("Tab container referansı atanmadı.");
            return;
        }

        if (tabButtonPrefab == null)
        {
            SetMessage("Tab button prefab referansı atanmadı.");
            return;
        }

        IReadOnlyList<ShopTabDefinition> tabs = shopManager.GetTabs();
        for (int i = 0; i < tabs.Count; i++)
        {
            ShopTabDefinition tab = tabs[i];
            if (tab == null)
            {
                continue;
            }

            ShopTabButtonUI tabButton = Instantiate(tabButtonPrefab, tabContainer);
            tabButton.Bind(this, tab, shopManager.SelectedTabId == tab.TabId);
            spawnedTabButtons.Add(tabButton);
        }
    }

    /// <summary>
    /// O an seçili sekmeye ait ürün kartlarını yeniden üretir.
    /// Sekme değiştiğinde genelde çağrılan ana yenileme metodudur.
    /// </summary>
    public void RefreshProducts()
    {
        ClearSpawnedProductItems();

        if (shopManager == null)
        {
            SetMessage("ShopManager bulunamadı.");
            return;
        }

        if (productContainer == null)
        {
            SetMessage("Product container referansı atanmadı.");
            return;
        }

        if (productItemPrefab == null)
        {
            SetMessage("Product item prefab referansı atanmadı.");
            return;
        }

        IReadOnlyList<ShopProductDefinitionSO> products = shopManager.GetProductsForSelectedTab();
        for (int i = 0; i < products.Count; i++)
        {
            ShopProductDefinitionSO product = products[i];
            if (product == null)
            {
                continue;
            }

            ShopProductItemUI itemUI = Instantiate(productItemPrefab, productContainer);
            itemUI.Bind(this, product);
            spawnedProductItems.Add(itemUI);
        }

        RefreshSelectedTabTitle();
        RefreshTabSelectionVisuals();
        ResetScrollPosition();
    }

    /// <summary>
    /// Sekme butonları bu metodu çağırır.
    /// Burada seçim ShopManager'a iletilir; başarı olursa ürün listesi yenilenir.
    /// </summary>
    public void HandleTabSelected(string tabId)
    {
        if (shopManager == null)
        {
            SetMessage("ShopManager bulunamadı.");
            return;
        }

        bool selectionSucceeded = shopManager.TrySelectTab(tabId);
        if (!selectionSucceeded)
        {
            return;
        }

        RefreshProducts();
    }

    /// <summary>
    /// Ürün kartı içindeki satın al butonu bu metodu çağırır.
    /// ShopManager işlemi yaptıktan sonra mesaj ve buton durumları yenilenir.
    /// </summary>
    public void HandleBuyClicked(ShopProductDefinitionSO product, int quantity)
    {
        if (shopManager == null)
        {
            SetMessage("ShopManager bulunamadı.");
            return;
        }

        ShopOperationResult result;
        shopManager.TryPurchase(product, quantity, out result);
        RefreshProducts();
    }

    /// <summary>
    /// Ürün kartı içindeki sat butonu bu metodu çağırır.
    /// Envanter sahipliği bu sınıfta kontrol edilmez; sadece mevcut shop akışı tetiklenir.
    /// </summary>
    public void HandleSellClicked(ShopProductDefinitionSO product, int quantity)
    {
        if (shopManager == null)
        {
            SetMessage("ShopManager bulunamadı.");
            return;
        }

        ShopOperationResult result;
        shopManager.TrySell(product, quantity, out result);
        RefreshProducts();
    }

    private void SubscribeEvents()
    {
        if (shopManager != null)
        {
            shopManager.SelectedTabChanged += HandleSelectedTabChanged;
            shopManager.ShopMessageRaised += HandleShopMessageRaised;
            shopManager.OperationProcessed += HandleOperationProcessed;
        }

        if (economyManager != null)
        {
            economyManager.BalanceChanged += HandleBalanceChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (shopManager != null)
        {
            shopManager.SelectedTabChanged -= HandleSelectedTabChanged;
            shopManager.ShopMessageRaised -= HandleShopMessageRaised;
            shopManager.OperationProcessed -= HandleOperationProcessed;
        }

        if (economyManager != null)
        {
            economyManager.BalanceChanged -= HandleBalanceChanged;
        }
    }

    private void HandleSelectedTabChanged(string tabId)
    {
        RefreshProducts();
    }

    private void HandleShopMessageRaised(string message)
    {
        SetMessage(message);
    }

    private void HandleOperationProcessed(ShopOperationResult result)
    {
        SetMessage(result.Message);
    }

    private void HandleBalanceChanged(int newBalance)
    {
        RefreshBalance();
        RefreshProductButtonStates();
    }

    private void RefreshBalance()
    {
        if (balanceText == null)
        {
            return;
        }

        if (economyManager == null)
        {
            balanceText.text = "Bakiye: -";
            return;
        }

        balanceText.text = "Bakiye: " + economyManager.CurrentBalance;
    }

    private void RefreshSelectedTabTitle()
    {
        if (selectedTabTitleText == null)
        {
            return;
        }

        if (shopManager == null)
        {
            selectedTabTitleText.text = "Sekme Yok";
            return;
        }

        IReadOnlyList<ShopTabDefinition> tabs = shopManager.GetTabs();
        for (int i = 0; i < tabs.Count; i++)
        {
            ShopTabDefinition tab = tabs[i];
            if (tab == null)
            {
                continue;
            }

            if (tab.TabId == shopManager.SelectedTabId)
            {
                selectedTabTitleText.text = tab.DisplayName;
                return;
            }
        }

        selectedTabTitleText.text = "Sekme Yok";
    }

    private void RefreshTabSelectionVisuals()
    {
        for (int i = 0; i < spawnedTabButtons.Count; i++)
        {
            ShopTabButtonUI tabButton = spawnedTabButtons[i];
            if (tabButton == null)
            {
                continue;
            }

            tabButton.SetSelected(shopManager != null && tabButton.TabId == shopManager.SelectedTabId);
        }
    }

    private void RefreshProductButtonStates()
    {
        for (int i = 0; i < spawnedProductItems.Count; i++)
        {
            ShopProductItemUI itemUI = spawnedProductItems[i];
            if (itemUI == null)
            {
                continue;
            }

            itemUI.RefreshInteractableState();
        }
    }

    private void ResetScrollPosition()
    {
        if (productScrollRect == null)
        {
            return;
        }

        productScrollRect.verticalNormalizedPosition = 1f;
    }

    private void SetMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = message;
    }

    private void ClearSpawnedTabButtons()
    {
        for (int i = 0; i < spawnedTabButtons.Count; i++)
        {
            ShopTabButtonUI tabButton = spawnedTabButtons[i];
            if (tabButton == null)
            {
                continue;
            }

            Destroy(tabButton.gameObject);
        }

        spawnedTabButtons.Clear();
    }

    private void ClearSpawnedProductItems()
    {
        for (int i = 0; i < spawnedProductItems.Count; i++)
        {
            ShopProductItemUI itemUI = spawnedProductItems[i];
            if (itemUI == null)
            {
                continue;
            }

            Destroy(itemUI.gameObject);
        }

        spawnedProductItems.Clear();
    }
}
