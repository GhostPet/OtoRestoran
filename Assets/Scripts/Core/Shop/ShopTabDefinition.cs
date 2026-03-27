using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shop içindeki tek bir sekmenin verisini tutar.
/// Bu yapı sayesinde 'Mutfak Malzemeleri', 'Robotlar', 'Dekorasyon' gibi sekmeler tamamen veri üzerinden tanımlanabilir.
/// </summary>
[Serializable]
public class ShopTabDefinition {
	[SerializeField] private string tabId;
	[SerializeField] private string displayName;
	[SerializeField] private Sprite icon;
	[SerializeField] private List<ShopProductDefinitionSO> products = new List<ShopProductDefinitionSO>();

	public string TabId => tabId;

	public string DisplayName => displayName;

	public Sprite Icon => icon;

	public IReadOnlyList<ShopProductDefinitionSO> Products => products;

	public bool ContainsProduct(ShopProductDefinitionSO product) {
		if (product == null) {
			return false;
		}

		for (int i = 0; i < products.Count; i++) {
			if (products[i] == product) {
				return true;
			}
		}

		return false;
	}
}
