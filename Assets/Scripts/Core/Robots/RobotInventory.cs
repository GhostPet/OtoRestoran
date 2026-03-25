using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RobotInventorySlot {
	[SerializeField] private ItemSO item;
	[SerializeField] private int quantity;

	public ItemSO Item => item;

	public int Quantity => quantity;

	public bool IsEmpty => item == null || quantity <= 0;

	public void Set(ItemSO newItem, int newQuantity) {
		item = newItem;
		quantity = Mathf.Max(0, newQuantity);

		if (quantity == 0) {
			item = null;
		}
	}

	public void Clear() {
		item = null;
		quantity = 0;
	}
}

public class RobotInventory : MonoBehaviour {
	private const int FixedSlotCount = 4;

	[SerializeField] private List<RobotInventorySlot> slots = new List<RobotInventorySlot>(FixedSlotCount);

	public int SlotCount => FixedSlotCount;

	public IReadOnlyList<RobotInventorySlot> Slots => slots;

	private void Awake() {
		EnsureSlots();
	}

	private void OnValidate() {
		EnsureSlots();
	}

	public int GetQuantity(ItemSO item) {
		if (item == null) {
			return 0;
		}

		int total = 0;
		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || slot.IsEmpty || slot.Item != item) {
				continue;
			}

			total += slot.Quantity;
		}

		return total;
	}

	public bool HasEnough(ItemSO item, int quantity) {
		if (quantity <= 0) {
			return true;
		}

		return GetQuantity(item) >= quantity;
	}

	public bool HasIngredients(IReadOnlyList<RecipeItemStack> entries) {
		if (entries == null || entries.Count == 0) {
			return false;
		}

		bool hasValidEntry = false;

		for (int i = 0; i < entries.Count; i++) {
			RecipeItemStack entry = entries[i];
			if (entry == null || !entry.IsValid) {
				continue;
			}

			hasValidEntry = true;

			if (!HasEnough(entry.Item, entry.Quantity)) {
				return false;
			}
		}

		return hasValidEntry;
	}

	public bool TryConsumeIngredients(IReadOnlyList<RecipeItemStack> entries) {
		if (!HasIngredients(entries)) {
			return false;
		}

		for (int i = 0; i < entries.Count; i++) {
			RecipeItemStack entry = entries[i];
			if (entry == null || !entry.IsValid) {
				continue;
			}

			TryRemoveItem(entry.Item, entry.Quantity);
		}

		return true;
	}

	public bool CanAddItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0) {
			return false;
		}

		int remaining = quantity;

		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || slot.IsEmpty || slot.Item != item) {
				continue;
			}

			remaining -= Mathf.Max(0, item.MaxStack - slot.Quantity);
			if (remaining <= 0) {
				return true;
			}
		}

		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || !slot.IsEmpty) {
				continue;
			}

			remaining -= item.MaxStack;
			if (remaining <= 0) {
				return true;
			}
		}

		return false;
	}

	public bool TryAddItem(ItemSO item, int quantity) {
		if (!CanAddItem(item, quantity)) {
			return false;
		}

		int remaining = quantity;

		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || slot.IsEmpty || slot.Item != item) {
				continue;
			}

			int capacity = Mathf.Max(0, item.MaxStack - slot.Quantity);
			if (capacity <= 0) {
				continue;
			}

			int amountToAdd = Mathf.Min(remaining, capacity);
			slot.Set(item, slot.Quantity + amountToAdd);
			remaining -= amountToAdd;

			if (remaining <= 0) {
				return true;
			}
		}

		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || !slot.IsEmpty) {
				continue;
			}

			int amountToAdd = Mathf.Min(remaining, item.MaxStack);
			slot.Set(item, amountToAdd);
			remaining -= amountToAdd;

			if (remaining <= 0) {
				return true;
			}
		}

		return false;
	}

	public bool TryAddRecipeOutputs(IReadOnlyList<RecipeItemStack> entries) {
		if (entries == null) {
			return false;
		}

		List<RobotInventorySlotSnapshot> snapshot = CaptureSnapshot();

		for (int i = 0; i < entries.Count; i++) {
			RecipeItemStack entry = entries[i];
			if (entry == null || !entry.IsValid) {
				continue;
			}

			if (!TryAddItem(entry.Item, entry.Quantity)) {
				RestoreSnapshot(snapshot);
				return false;
			}
		}

		return true;
	}

	public bool TryRemoveItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0) {
			return false;
		}

		if (!HasEnough(item, quantity)) {
			return false;
		}

		int remaining = quantity;
		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null || slot.IsEmpty || slot.Item != item) {
				continue;
			}

			int amountToRemove = Mathf.Min(remaining, slot.Quantity);
			int newQuantity = slot.Quantity - amountToRemove;
			if (newQuantity <= 0) {
				slot.Clear();
			} else {
				slot.Set(item, newQuantity);
			}

			remaining -= amountToRemove;
			if (remaining <= 0) {
				return true;
			}
		}

		return false;
	}

	private void EnsureSlots() {
		if (slots == null) {
			slots = new List<RobotInventorySlot>(FixedSlotCount);
		}

		while (slots.Count < FixedSlotCount) {
			slots.Add(new RobotInventorySlot());
		}

		while (slots.Count > FixedSlotCount) {
			slots.RemoveAt(slots.Count - 1);
		}
		for (int i = 0; i < slots.Count; i++) {
			if (slots[i] == null) {
				slots[i] = new RobotInventorySlot();
			}
		}
	}

	private List<RobotInventorySlotSnapshot> CaptureSnapshot() {
		var snapshot = new List<RobotInventorySlotSnapshot>(slots.Count);
		for (int i = 0; i < slots.Count; i++) {
			RobotInventorySlot slot = slots[i];
			if (slot == null) {
				snapshot.Add(new RobotInventorySlotSnapshot(null, 0));
				continue;
			}

			snapshot.Add(new RobotInventorySlotSnapshot(slot.Item, slot.Quantity));
		}

		return snapshot;
	}

	private void RestoreSnapshot(List<RobotInventorySlotSnapshot> snapshot) {
		if (snapshot == null) {
			return;
		}

		EnsureSlots();
		for (int i = 0; i < slots.Count && i < snapshot.Count; i++) {
			RobotInventorySlot slot = slots[i];
			RobotInventorySlotSnapshot slotSnapshot = snapshot[i];
			if (slot == null) {
				continue;
			}

			slot.Set(slotSnapshot.Item, slotSnapshot.Quantity);
		}
	}

	private readonly struct RobotInventorySlotSnapshot {
		public RobotInventorySlotSnapshot(ItemSO item, int quantity) {
			Item = item;
			Quantity = quantity;
		}

		public ItemSO Item { get; }

		public int Quantity { get; }
	}
}
