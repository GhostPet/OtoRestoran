using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyun sırasında tüketilecek malzemeleri yöneten ana envanterdir.
/// Bu sistem build modundan tamamen bağımsızdır.
/// Örnek içerikler: ekmek, köfte, sos, içecek malzemeleri.
/// </summary>
public class InventoryManager : MonoBehaviour {
	[Header("Bağlantılar")]
	[SerializeField] private ShopManager shopManager;

	private readonly Dictionary<ItemSO, int> itemQuantities = new Dictionary<ItemSO, int>();

	public event Action InventoryChanged;
	public event Action<ItemSO, int> ItemQuantityChanged;

	protected virtual void OnEnable() {
		if (shopManager == null) {
			shopManager = FindAnyObjectByType<ShopManager>();
		}

		if (shopManager != null) {
			shopManager.PurchaseCompleted += HandlePurchaseCompleted;
			shopManager.SaleCompleted += HandleSaleCompleted;
		}
	}

	protected virtual void OnDisable() {
		if (shopManager != null) {
			shopManager.PurchaseCompleted -= HandlePurchaseCompleted;
			shopManager.SaleCompleted -= HandleSaleCompleted;
		}
	}

	public int GetQuantity(ItemSO item) {
		if (item == null) {
			return 0;
		}

		int quantity;
		if (itemQuantities.TryGetValue(item, out quantity)) {
			return quantity;
		}

		return 0;
	}

	public bool HasEnough(ItemSO item, int quantity) {
		if (quantity <= 0) {
			return true;
		}

		return GetQuantity(item) >= quantity;
	}

	/// <summary>
	/// İlgili item için yeni alım yapıldığında envanter limiti aşılacak mı bilgisini verir.
	/// Şu an basit kapasite olarak ItemSO.MaxStack değeri kullanılıyor.
	/// </summary>
	public bool CanAddItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0) {
			return false;
		}

		return GetQuantity(item) + quantity <= item.MaxStack;
	}

	public int GetRemainingCapacity(ItemSO item) {
		if (item == null) {
			return 0;
		}

		int remaining = item.MaxStack - GetQuantity(item);
		return Mathf.Max(0, remaining);
	}

	public bool TryAddItem(ItemSO item, int quantity) {
		if (!CanAddItem(item, quantity)) {
			return false;
		}

		AddItem(item, quantity);
		return true;
	}

	public void AddItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0) {
			return;
		}

		int currentQuantity = GetQuantity(item);
		int newQuantity = Mathf.Min(item.MaxStack, currentQuantity + quantity);
		itemQuantities[item] = newQuantity;

		RaiseInventoryEvents(item, newQuantity);
	}

	public bool TryRemoveItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0) {
			return false;
		}

		int currentQuantity = GetQuantity(item);
		if (currentQuantity < quantity) {
			return false;
		}

		int newQuantity = currentQuantity - quantity;
		if (newQuantity <= 0) {
			itemQuantities.Remove(item);
			newQuantity = 0;
		} else {
			itemQuantities[item] = newQuantity;
		}

		RaiseInventoryEvents(item, newQuantity);
		return true;
	}

	public List<InventoryItemEntry> CreateSnapshot() {
		var snapshot = new List<InventoryItemEntry>();

		foreach (KeyValuePair<ItemSO, int> pair in itemQuantities) {
			snapshot.Add(new InventoryItemEntry(pair.Key, pair.Value));
		}

		return snapshot;
	}

	private void HandlePurchaseCompleted(ShopTransactionEventArgs args) {
		if (args == null || args.Product == null) {
			return;
		}

		if (args.Product.StorageType != ShopProductStorageType.ConsumableInventory) {
			return;
		}

		TryAddItem(args.Product.ConsumableItem, args.Quantity);
	}

	private void HandleSaleCompleted(ShopTransactionEventArgs args) {
		if (args == null || args.Product == null) {
			return;
		}

		if (args.Product.StorageType != ShopProductStorageType.ConsumableInventory) {
			return;
		}

		TryRemoveItem(args.Product.ConsumableItem, args.Quantity);
	}

	private void RaiseInventoryEvents(ItemSO item, int quantity) {
		ItemQuantityChanged?.Invoke(item, quantity);
		InventoryChanged?.Invoke();
	}
}
