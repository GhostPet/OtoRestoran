using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour {
	[Header("Bağlantılar")]
	[SerializeField] private EconomyManager economyManager;
 [SerializeField] private GameScoreManager gameScoreManager;
	[SerializeField] private InventoryManager inventoryManager;
	[SerializeField] private BuildInventoryManager buildInventoryManager;
	[SerializeField] private GridManager gridManager;
	[SerializeField] private ShopCatalogSO catalog;

	[Header("Sekme Ayarları")]
	[SerializeField] private string defaultTabId;
	[SerializeField] private bool autoSelectFirstTabIfDefaultMissing = true;

	private string selectedTabId;

	/// <summary>
	/// Aktif sekme değiştiğinde UI tarafının yeni listeyi çizmesini sağlar.
	/// </summary>
	public event Action<string> SelectedTabChanged;

	/// <summary>
	/// Satın alma başarılı olduğunda çağrılır.
	/// Build inventory veya placement sistemi bu event'e abone olup ilgili nesneyi ekleyebilir.
	/// </summary>
	public event Action<ShopTransactionEventArgs> PurchaseCompleted;

	/// <summary>
	/// Satış başarılı olduğunda çağrılır.
	/// İleride inventory sistemi eldeki ürünü düşmek için bu event'i dinleyebilir.
	/// </summary>
	public event Action<ShopTransactionEventArgs> SaleCompleted;

	/// <summary>
	/// Shop işlemlerinin kullanıcıya mesaj vermesi gerektiğinde kullanılabilir.
	/// Örneğin yetersiz para, yanlış sekme ya da başarılı işlem mesajları burada yayınlanır.
	/// </summary>
	public event Action<string> ShopMessageRaised;

	/// <summary>
	/// Her shop işleminin özet sonucunu yayınlar.
	/// UI, log veya analytics sistemi bu tek event ile tüm shop akışını takip edebilir.
	/// </summary>
	public event Action<ShopOperationResult> OperationProcessed;

	public string SelectedTabId => selectedTabId;

	public ShopCatalogSO Catalog => catalog;

	private void Awake() {
		if (economyManager == null) {
			economyManager = FindAnyObjectByType<EconomyManager>();
		}

		if (gameScoreManager == null) {
			gameScoreManager = FindAnyObjectByType<GameScoreManager>();
		}

		if (inventoryManager == null) {
			inventoryManager = FindAnyObjectByType<InventoryManager>();
		}

		if (buildInventoryManager == null) {
			buildInventoryManager = FindAnyObjectByType<BuildInventoryManager>();
		}

		if (gridManager == null) {
			gridManager = FindAnyObjectByType<GridManager>();
		}

		InitializeSelectedTab();
	}

	/// <summary>
	/// Shop'taki tüm sekmeleri dışarıya okunur şekilde verir.
	/// Bu sayede UI tarafı sekme butonlarını veri odaklı şekilde oluşturabilir.
	/// </summary>
	public IReadOnlyList<ShopTabDefinition> GetTabs() {
		if (catalog == null) {
			return Array.Empty<ShopTabDefinition>();
		}

		return catalog.Tabs;
	}

	/// <summary>
	/// UI bir sekmeye tıkladığında bu metod çağrılır.
	/// Sekme bulunursa aktif sekme değiştirilir ve event yayınlanır.
	/// </summary>
	public bool TrySelectTab(string tabId) {
		if (catalog == null) {
			PublishMessage("Shop kataloğu atanmadığı için sekme seçilemedi.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(tabId)) {
			PublishMessage("Geçerli bir sekme kimliği verilmedi.");
			return false;
		}

		ShopTabDefinition tab;
		if (!catalog.TryGetTab(tabId, out tab)) {
			PublishMessage("İstenen shop sekmesi bulunamadı.");
			return false;
		}

		selectedTabId = tab.TabId;
		SelectedTabChanged?.Invoke(selectedTabId);
		return true;
	}

	/// <summary>
	/// O anda seçili olan sekmedeki ürünleri döndürür.
	/// Sekme sistemini veri odaklı kurduğumuz için yeni sekme eklemek sadece katalog verisini güncellemeyi gerektirir.
	/// </summary>
	public IReadOnlyList<ShopProductDefinitionSO> GetProductsForSelectedTab() {
		return GetProductsForTab(selectedTabId);
	}

	/// <summary>
	/// İstenen sekmedeki ürünleri döndürür.
	/// UI'nin sekmeye geçmeden önizleme hazırlaması gerekirse bu metod kullanılabilir.
	/// </summary>
	public IReadOnlyList<ShopProductDefinitionSO> GetProductsForTab(string tabId) {
		if (catalog == null) {
			return Array.Empty<ShopProductDefinitionSO>();
		}

		return catalog.GetProducts(tabId);
	}

	/// <summary>
	/// Satın alma işleminin kurallarını kontrol eder ve para düşümünü economy üzerinden yapar.
	/// Burada envantere ürün eklenmez; sadece onay verilir ve event yayınlanır.
	/// </summary>
	public bool TryPurchase(ShopProductDefinitionSO product, int quantity, out ShopOperationResult result) {
		quantity = ResolveTransactionQuantity(product, quantity);

		string validationMessage;
		if (!ValidatePurchaseRequest(product, quantity, out validationMessage)) {
			result = ShopOperationResult.CreateFailure(ShopTransactionType.Purchase, 0, validationMessage);
			PublishOperation(result);
			return false;
		}

		int totalCost = product.GetBuyTotalPrice(quantity);
		EconomyTransactionResult economyResult;
		bool spendSucceeded = economyManager.TrySpend(totalCost, "Shop purchase: " + product.DisplayName, out economyResult);

		if (!spendSucceeded) {
			result = ShopOperationResult.CreateFailure(ShopTransactionType.Purchase, totalCost, economyResult.Message);
			PublishOperation(result);
			return false;
		}

		result = ShopOperationResult.CreateSuccess(ShopTransactionType.Purchase, totalCost, "Satın alma işlemi başarılı.");

		var eventArgs = new ShopTransactionEventArgs(product, quantity, totalCost, economyResult.CurrentBalance, selectedTabId, ShopTransactionType.Purchase);
		PurchaseCompleted?.Invoke(eventArgs);
		PublishOperation(result);
		return true;
	}

	/// <summary>
	/// Satış işlemini economy tarafına gelir olarak işler.
	/// Bu metod ürün sahipliği kontrolü yapmaz; o sorumluluk daha sonra build inventory sistemine bırakılmıştır.
	/// Yani bu metod çağrılmadan önce oyuncunun gerçekten o ürüne sahip olduğu dış sistem tarafından doğrulanmalıdır.
	/// </summary>
	public bool TrySell(ShopProductDefinitionSO product, int quantity, out ShopOperationResult result) {
		quantity = ResolveTransactionQuantity(product, quantity);

		string validationMessage;
		if (!ValidateSellRequest(product, quantity, out validationMessage)) {
			result = ShopOperationResult.CreateFailure(ShopTransactionType.Sale, 0, validationMessage);
			PublishOperation(result);
			return false;
		}

		int totalRevenue = product.GetSellTotalPrice(quantity);
		EconomyTransactionResult economyResult;
		bool earnSucceeded = economyManager.TryEarn(totalRevenue, "Shop sale: " + product.DisplayName, out economyResult);

		if (!earnSucceeded) {
			result = ShopOperationResult.CreateFailure(ShopTransactionType.Sale, totalRevenue, economyResult.Message);
			PublishOperation(result);
			return false;
		}

		result = ShopOperationResult.CreateSuccess(ShopTransactionType.Sale, totalRevenue, "Satış işlemi başarılı.");

		var eventArgs = new ShopTransactionEventArgs(product, quantity, totalRevenue, economyResult.CurrentBalance, selectedTabId, ShopTransactionType.Sale);
		SaleCompleted?.Invoke(eventArgs);
		PublishOperation(result);
		return true;
	}

	/// <summary>
	/// UI'nin satın alma butonunu pasif yapmak için kullanabileceği hızlı kontroldür.
	/// İşlemi gerçekleştirmez, sadece mümkün mü sorusuna cevap verir.
	/// </summary>
	public bool CanPurchase(ShopProductDefinitionSO product, int quantity, out string reason) {
		quantity = ResolveTransactionQuantity(product, quantity);
		return ValidatePurchaseRequest(product, quantity, out reason);
	}

	/// <summary>
	/// UI'nin satış butonunu pasif yapmak için kullanabileceği hızlı kontroldür.
	/// Envanter sahipliği burada kontrol edilmez; sadece shop seviyesindeki satış kuralları incelenir.
	/// </summary>
	public bool CanSell(ShopProductDefinitionSO product, int quantity, out string reason) {
		quantity = ResolveTransactionQuantity(product, quantity);
		return ValidateSellRequest(product, quantity, out reason);
	}

	/// <summary>
	/// UI tarafının üründen oyuncuda kaç adet olduğunu gösterebilmesi için yazıldı.
	/// Malzeme ve build envanteri arasında doğru kaynağı otomatik seçer.
	/// </summary>
	public int GetOwnedQuantity(ShopProductDefinitionSO product) {
		if (product == null) {
			return 0;
		}

		switch (product.StorageType) {
			case ShopProductStorageType.ConsumableInventory:
				if (inventoryManager == null || product.ConsumableItem == null) {
					return 0;
				}

				return inventoryManager.GetQuantity(product.ConsumableItem);

			case ShopProductStorageType.BuildInventory:
				if (buildInventoryManager == null || product.BuildPlaceableData == null) {
					return 0;
				}

				return buildInventoryManager.GetQuantity(product.BuildPlaceableData);

			default:
				return 0;
		}
	}

	public int GetPlacedQuantity(ShopProductDefinitionSO product) {
		if (product == null || product.StorageType != ShopProductStorageType.BuildInventory || product.BuildPlaceableData == null) {
			return 0;
		}

		if (gridManager == null) {
			gridManager = FindAnyObjectByType<GridManager>();
		}

		if (gridManager == null) {
			return 0;
		}

		return gridManager.GetPlacedCount(product.BuildPlaceableData);
	}

	public int GetTotalOwnedQuantity(ShopProductDefinitionSO product) {
		return GetOwnedQuantity(product) + GetPlacedQuantity(product);
	}

	public int GetOwnershipLimit(ShopProductDefinitionSO product) {
		if (product == null || product.StorageType != ShopProductStorageType.BuildInventory || product.BuildPlaceableData == null) {
			return 0;
		}

		return product.BuildPlaceableData.maxOwnedCount;
	}

	public int GetCurrentGameScore() {
		if (gameScoreManager == null) {
			return 0;
		}

		return gameScoreManager.CurrentScore;
	}

	private void InitializeSelectedTab() {
		if (catalog == null) {
			selectedTabId = string.Empty;
			return;
		}

		if (!string.IsNullOrWhiteSpace(defaultTabId)) {
			ShopTabDefinition defaultTab;
			if (catalog.TryGetTab(defaultTabId, out defaultTab)) {
				selectedTabId = defaultTab.TabId;
				return;
			}
		}

		if (autoSelectFirstTabIfDefaultMissing) {
			selectedTabId = catalog.GetFirstTabId();
			return;
		}

		selectedTabId = string.Empty;
	}

	private bool ValidatePurchaseRequest(ShopProductDefinitionSO product, int quantity, out string message) {
		if (!ValidateCommonRequest(product, quantity, out message)) {
			return false;
		}

		if (!product.CanBePurchased) {
			message = "Bu ürün şu anda satın alınamaz durumda.";
			return false;
		}

		if (!HasRequiredGameScore(product, out message)) {
			return false;
		}

		if (!CanStorePurchasedProduct(product, quantity, out message)) {
			return false;
		}

		int totalCost = product.GetBuyTotalPrice(quantity);
		if (!economyManager.CanAfford(totalCost)) {
			message = "Bu ürün için yeterli paranız yok.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	private bool ValidateSellRequest(ShopProductDefinitionSO product, int quantity, out string message) {
		if (!ValidateCommonRequest(product, quantity, out message)) {
			return false;
		}

		if (!product.CanBeSold) {
			message = "Bu ürün shop üzerinden satılamaz durumda.";
			return false;
		}

		if (product.GetSellUnitPrice(quantity) <= 0) {
			message = "Bu ürünün satış değeri tanımlanmamış.";
			return false;
		}

		if (!HasEnoughInventoryForSale(product, quantity, out message)) {
			return false;
		}

		message = string.Empty;
		return true;
	}

	private bool ValidateCommonRequest(ShopProductDefinitionSO product, int quantity, out string message) {
		if (catalog == null) {
			message = "Shop kataloğu atanmadığı için işlem yapılamaz.";
			return false;
		}

		if (economyManager == null) {
			message = "EconomyManager bulunamadığı için işlem yapılamaz.";
			return false;
		}

		if (product == null) {
			message = "İşlem yapılacak ürün bilgisi boş.";
			return false;
		}

		if (!catalog.ContainsProduct(product)) {
			message = "Bu ürün aktif shop kataloğunda yer almıyor.";
			return false;
		}

		if (quantity <= 0) {
			message = "Adet bilgisi 0'dan büyük olmalıdır.";
			return false;
		}

		if (quantity <= 0) {
			message = "Geçerli bir ürün adedi tanımlanmamış.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	private void PublishOperation(ShopOperationResult result) {
		PublishMessage(result.Message);
		OperationProcessed?.Invoke(result);
	}

	private bool HasRequiredGameScore(ShopProductDefinitionSO product, out string message) {
		if (product == null) {
			message = "Ürün bilgisi bulunamadı.";
			return false;
		}

		if (product.RequiredGameScore <= 0) {
			message = string.Empty;
			return true;
		}

		if (gameScoreManager == null) {
			message = "GameScoreManager bulunamadığı için ürün kilidi kontrol edilemedi.";
			return false;
		}

		if (!gameScoreManager.HasRequiredScore(product.RequiredGameScore)) {
			message = "Bu ürünü açmak için en az " + product.RequiredGameScore + " game score gerekiyor.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	private int ResolveTransactionQuantity(ShopProductDefinitionSO product, int requestedQuantity) {
		if (product == null) {
			return requestedQuantity;
		}

		if (product.TransactionQuantity > 0) {
			return product.TransactionQuantity;
		}

		if (requestedQuantity > 0) {
			return requestedQuantity;
		}

		return 1;
	}

	private bool CanStorePurchasedProduct(ShopProductDefinitionSO product, int quantity, out string message) {
		switch (product.StorageType) {
			case ShopProductStorageType.None:
				message = string.Empty;
				return true;

			case ShopProductStorageType.ConsumableInventory:
				if (inventoryManager == null) {
					message = "Malzeme envanteri bulunamadı.";
					return false;
				}

				if (product.ConsumableItem == null) {
					message = "Bu shop ürünü için malzeme item eşlemesi yapılmamış.";
					return false;
				}

				if (!inventoryManager.CanAddItem(product.ConsumableItem, quantity)) {
					message = "Bu paketi almak envanter limitini aşıyor.";
					return false;
				}

				message = string.Empty;
				return true;

			case ShopProductStorageType.BuildInventory:
				if (buildInventoryManager == null) {
					message = "Build envanteri bulunamadı.";
					return false;
				}

				if (product.BuildPlaceableData == null) {
					message = "Bu shop ürünü için build placeable eşlemesi yapılmamış.";
					return false;
				}

				if (!CanOwnMoreBuildItems(product.BuildPlaceableData, quantity, out message)) {
					return false;
				}

				message = string.Empty;
				return true;

			default:
				message = "Bilinmeyen shop ürün tipi.";
				return false;
		}
	}

	private bool HasEnoughInventoryForSale(ShopProductDefinitionSO product, int quantity, out string message) {
		switch (product.StorageType) {
			case ShopProductStorageType.None:
				message = string.Empty;
				return true;

			case ShopProductStorageType.ConsumableInventory:
				if (inventoryManager == null) {
					message = "Malzeme envanteri bulunamadı.";
					return false;
				}

				if (product.ConsumableItem == null) {
					message = "Bu shop ürünü için malzeme item eşlemesi yapılmamış.";
					return false;
				}

				if (!inventoryManager.HasEnough(product.ConsumableItem, quantity)) {
					message = "Satmak için yeterli malzeme bulunmuyor.";
					return false;
				}

				message = string.Empty;
				return true;

			case ShopProductStorageType.BuildInventory:
				if (buildInventoryManager == null) {
					message = "Build envanteri bulunamadı.";
					return false;
				}

				if (product.BuildPlaceableData == null) {
					message = "Bu shop ürünü için build placeable eşlemesi yapılmamış.";
					return false;
				}

				if (!buildInventoryManager.HasEnough(product.BuildPlaceableData, quantity)) {
					message = "Satmak için yeterli build objesi bulunmuyor.";
					return false;
				}

				message = string.Empty;
				return true;

			default:
				message = "Bilinmeyen shop ürün tipi.";
				return false;
		}
	}

	private bool CanOwnMoreBuildItems(PlaceableObjectSO placeableData, int quantity, out string message) {
		if (placeableData == null) {
			message = "Build objesi bulunamadı.";
			return false;
		}

		if (!placeableData.HasOwnershipLimit) {
			message = string.Empty;
			return true;
		}

		int storedQuantity = buildInventoryManager != null ? buildInventoryManager.GetQuantity(placeableData) : 0;

		if (gridManager == null) {
			gridManager = FindAnyObjectByType<GridManager>();
		}

		int placedQuantity = gridManager != null ? gridManager.GetPlacedCount(placeableData) : 0;
		if (storedQuantity + placedQuantity + quantity > placeableData.maxOwnedCount) {
			message = "Bu objeden en fazla " + placeableData.maxOwnedCount + " adet sahip olabilirsiniz.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	private void PublishMessage(string message) {
		if (string.IsNullOrWhiteSpace(message)) {
			return;
		}

		ShopMessageRaised?.Invoke(message);
	}
}
