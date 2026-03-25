using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StorageInventory))]
public class FridgeBehavior : BaseRestaurantObject, IPlaceableLifecycle {
	[SerializeField] private StorageInventory storageInventory;

	private void Awake() {
		if (storageInventory == null) {
			storageInventory = GetComponent<StorageInventory>();
		}
	}

	public StorageInventory Inventory => storageInventory;

	public int GetQuantity(ItemSO item) {
		if (storageInventory == null) {
			return 0;
		}

		return storageInventory.GetQuantity(item);
	}

	public bool HasEnough(ItemSO item, int quantity) {
		if (storageInventory == null) {
			return false;
		}

		return storageInventory.HasEnough(item, quantity);
	}

	public bool TryTake(ItemSO item, int quantity, RobotInventory robotInventory) {
		if (storageInventory == null) {
			return false;
		}

		return storageInventory.TryTake(item, quantity, robotInventory);
	}

	public List<InventoryItemEntry> CreateSnapshot() {
		if (storageInventory == null) {
			return new List<InventoryItemEntry>();
		}

		return storageInventory.CreateSnapshot();
	}

	public void OnPlaced(PlaceableObject placedObject) {
	}

	public void OnRemoved(PlaceableObject placedObject) {
	}
}
