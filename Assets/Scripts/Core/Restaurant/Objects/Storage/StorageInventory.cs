using System.Collections.Generic;
using UnityEngine;

public class StorageInventory : MonoBehaviour {
	[SerializeField] private InventoryManager inventoryManager;

	private void Awake() {
		if (inventoryManager == null) {
			inventoryManager = FindAnyObjectByType<InventoryManager>();
		}
	}

	public InventoryManager InventoryManager => inventoryManager;

	public int GetQuantity(ItemSO item) {
		if (inventoryManager == null) {
			return 0;
		}

		return inventoryManager.GetQuantity(item);
	}

	public bool HasEnough(ItemSO item, int quantity) {
		if (inventoryManager == null) {
			return false;
		}

		return inventoryManager.HasEnough(item, quantity);
	}

	public bool CanTake(ItemSO item, int quantity, RobotInventory robotInventory) {
		if (inventoryManager == null || robotInventory == null || item == null || quantity <= 0) {
			return false;
		}

		return inventoryManager.HasEnough(item, quantity) && robotInventory.CanAddItem(item, quantity);
	}

	public bool TryTake(ItemSO item, int quantity, RobotInventory robotInventory) {
		if (!CanTake(item, quantity, robotInventory)) {
			return false;
		}

		if (!inventoryManager.TryRemoveItem(item, quantity)) {
			return false;
		}

		if (robotInventory.TryAddItem(item, quantity)) {
			return true;
		}

		inventoryManager.TryAddItem(item, quantity);
		return false;
	}

	public List<InventoryItemEntry> CreateSnapshot() {
		if (inventoryManager == null) {
			return new List<InventoryItemEntry>();
		}

		return inventoryManager.CreateSnapshot();
	}
}
