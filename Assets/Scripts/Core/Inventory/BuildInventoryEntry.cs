using System;
using UnityEngine;

/// <summary>
/// Build inventory'de tek bir placeable obje ve adet bilgisini tutar.
/// Grid sistemine bağlanacak yapı bu sınıf üzerinden sade kalır.
/// </summary>
[Serializable]
public class BuildInventoryEntry {
	[SerializeField] private PlaceableObjectSO placeableData;
	[SerializeField] private int quantity;

	public BuildInventoryEntry(PlaceableObjectSO placeableData, int quantity) {
		this.placeableData = placeableData;
		this.quantity = quantity;
	}

	public PlaceableObjectSO PlaceableData => placeableData;

	public int Quantity => quantity;
}
