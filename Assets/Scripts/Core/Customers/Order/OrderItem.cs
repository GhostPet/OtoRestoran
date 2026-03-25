using System;
using UnityEngine;

[Serializable]
public class OrderItem {
	[SerializeField] private ItemSO item;
	[SerializeField] private int quantity = 1;

	public OrderItem() {
	}

	public OrderItem(ItemSO item, int quantity) {
		this.item = item;
		this.quantity = Mathf.Max(1, quantity);
	}

	public ItemSO Item => item;

	public int Quantity => quantity;

	public bool IsValid => item != null && quantity > 0;

	public OrderItem Clone() {
		return new OrderItem(item, quantity);
	}

	public bool TryConsume(int amount) {
		if (amount <= 0 || quantity < amount) {
			return false;
		}

		quantity -= amount;
		return true;
	}

	public void ClampQuantity() {
		if (quantity < 1) {
			quantity = 1;
		}
	}
}
