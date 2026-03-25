using System;
using UnityEngine;

[Serializable]
public class RecipeItemStack {
	[SerializeField] private ItemSO item;
	[SerializeField] private int quantity = 1;

	public ItemSO Item => item;

	public int Quantity => quantity;

	public bool IsValid => item != null && quantity > 0;

	public void ClampQuantity() {
		if (quantity < 1) {
			quantity = 1;
		}
	}
}
