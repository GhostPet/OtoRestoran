using System;
using System.Collections.Generic;

[Serializable]
public class Order {
	public Customer Customer;
	public List<OrderItem> Items = new();

	public bool IsCompleted {
		get {
			if (Items == null || Items.Count == 0) {
				return true;
			}

			for (int i = 0; i < Items.Count; i++) {
				OrderItem entry = Items[i];
				if (entry != null && entry.IsValid) {
					return false;
				}
			}

			return true;
		}
	}

	public int GetRemainingQuantity(ItemSO item) {
		if (item == null || Items == null) {
			return 0;
		}

		int quantity = 0;
		for (int i = 0; i < Items.Count; i++) {
			OrderItem entry = Items[i];
			if (entry == null || !entry.IsValid || entry.Item != item) {
				continue;
			}

			quantity += entry.Quantity;
		}

		return quantity;
	}

	public bool IsFulfilledBy(RobotInventory inventory) {
		if (inventory == null || Items == null || Items.Count == 0) {
			return false;
		}

		for (int i = 0; i < Items.Count; i++) {
			OrderItem entry = Items[i];
			if (entry == null || !entry.IsValid) {
				return false;
			}

			if (!inventory.HasEnough(entry.Item, entry.Quantity)) {
				return false;
			}
		}

		RemoveCompletedItems();

		return true;
	}

	public bool TryConsumeItem(ItemSO item, int quantity) {
		if (item == null || quantity <= 0 || GetRemainingQuantity(item) < quantity) {
			return false;
		}

		int remaining = quantity;
		for (int i = 0; i < Items.Count; i++) {
			OrderItem entry = Items[i];
			if (entry == null || !entry.IsValid || entry.Item != item) {
				continue;
			}

			int amountToConsume = Math.Min(remaining, entry.Quantity);
			if (!entry.TryConsume(amountToConsume)) {
				return false;
			}

			remaining -= amountToConsume;
			if (remaining <= 0) {
				break;
			}
		}

		RemoveCompletedItems();
		return remaining <= 0;
	}

	public bool TryConsumeFrom(RobotInventory inventory) {
		if (!IsFulfilledBy(inventory)) {
			return false;
		}

		for (int i = 0; i < Items.Count; i++) {
			OrderItem entry = Items[i];
			if (entry == null || !entry.IsValid) {
				continue;
			}

			if (!inventory.TryRemoveItem(entry.Item, entry.Quantity)) {
				return false;
			}
		}

		return true;
	}

	public void AddItem(OrderItem entry) {
		if (entry == null || !entry.IsValid) {
			return;
		}

		if (Items == null) {
			Items = new List<OrderItem>();
		}

		Items.Add(entry.Clone());
	}

	private void RemoveCompletedItems() {
		if (Items == null) {
			return;
		}

		for (int i = Items.Count - 1; i >= 0; i--) {
			OrderItem entry = Items[i];
			if (entry == null || !entry.IsValid) {
				Items.RemoveAt(i);
			}
		}
	}
}

public static class ActiveOrders {
	private static readonly List<Order> orders = new();

	public static IReadOnlyList<Order> Orders => orders;

	public static void Remember(Order order) {
		if (order == null) return;
		if (!orders.Contains(order)) orders.Add(order);
	}

	public static void Remove(Order order) {
		if (order == null) return;
		orders.Remove(order);
	}

	public static void RemoveByCustomer(Customer customer) {
		if (customer == null) return;
		for (int i = orders.Count - 1; i >= 0; i--) {
			var order = orders[i];
			if (order == null) {
				orders.RemoveAt(i);
				continue;
			}

			if (order.Customer == customer) orders.RemoveAt(i);
		}
	}

	public static List<Order> Snapshot() {
		return new List<Order>(orders);
	}

	public static void Clear() {
		orders.Clear();
	}
}
