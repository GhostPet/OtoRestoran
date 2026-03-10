using System;
using UnityEngine;

/// <summary>
/// Malzeme inventory'sinde tek bir item ve adet bilgisini tutar.
/// Hem başlangıç verisi hem de snapshot üretmek için kullanılabilir.
/// </summary>
[Serializable]
public class InventoryItemEntry {
	[SerializeField] private ItemSO item;
	[SerializeField] private int quantity;

	public InventoryItemEntry(ItemSO item, int quantity) {
		this.item = item;
		this.quantity = quantity;
	}

	public ItemSO Item => item;

	public int Quantity => quantity;
}
