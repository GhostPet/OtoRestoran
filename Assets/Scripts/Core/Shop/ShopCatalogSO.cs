using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shop'un tüm sekmelerini ve o sekmelerdeki ürünleri tutan ana katalog verisidir.
/// Yeni sekme eklemek için kod değiştirmek yerine bu asset üzerinden veri girmek yeterlidir.
/// </summary>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "OtoRestoran/Shop/Catalog")]
public class ShopCatalogSO : ScriptableObject {
	[SerializeField] private List<ShopTabDefinition> tabs = new List<ShopTabDefinition>();

	public IReadOnlyList<ShopTabDefinition> Tabs => tabs;

	public bool TryGetTab(string tabId, out ShopTabDefinition tab) {
		for (int i = 0; i < tabs.Count; i++) {
			ShopTabDefinition currentTab = tabs[i];
			if (currentTab == null) {
				continue;
			}

			if (string.Equals(currentTab.TabId, tabId, StringComparison.OrdinalIgnoreCase)) {
				tab = currentTab;
				return true;
			}
		}

		tab = null;
		return false;
	}

	public IReadOnlyList<ShopProductDefinitionSO> GetProducts(string tabId) {
		ShopTabDefinition tab;
		if (TryGetTab(tabId, out tab)) {
			return tab.Products;
		}

		return Array.Empty<ShopProductDefinitionSO>();
	}

	public bool ContainsProduct(ShopProductDefinitionSO product) {
		if (product == null) {
			return false;
		}

		for (int i = 0; i < tabs.Count; i++) {
			ShopTabDefinition currentTab = tabs[i];
			if (currentTab == null) {
				continue;
			}

			if (currentTab.ContainsProduct(product)) {
				return true;
			}
		}

		return false;
	}

	public string GetFirstTabId() {
		for (int i = 0; i < tabs.Count; i++) {
			ShopTabDefinition currentTab = tabs[i];
			if (currentTab == null) {
				continue;
			}

			if (!string.IsNullOrWhiteSpace(currentTab.TabId)) {
				return currentTab.TabId;
			}
		}

		return string.Empty;
	}
}
