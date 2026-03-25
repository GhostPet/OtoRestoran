using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopWindowUI : WindowContentUI {
	[SerializeField] private ShopManager shopManager;
	[SerializeField] private Transform tabContainer;
	[SerializeField] private ShopTabButtonUI tabButtonPrefab;
	[SerializeField] private Transform productContainer;
	[SerializeField] private ShopProductItemUI productItemPrefab;
	[SerializeField] private TMP_Text selectedTabTitleText;
	[SerializeField] private TMP_Text messageText;

	private readonly List<ShopTabButtonUI> spawnedTabs = new List<ShopTabButtonUI>();
	private readonly List<ShopProductItemUI> spawnedProducts = new List<ShopProductItemUI>();

	public ShopManager ShopManager => shopManager;

	public void SetReference(ShopManager manager) {
		shopManager = manager;
		RefreshAll();
	}

	protected override void OnWindowBound(GameWindowUI ownerWindow) {
      ResolveShopManager();
		if (ownerWindow != null) {
			ownerWindow.SetTitle("Shop");
		}

		RefreshAll();
	}

	public void RefreshAll() {
        ResolveShopManager();
		BuildTabs();
		RefreshProducts();
		RefreshSelectedTabTitle();
	}

	private void ResolveShopManager() {
		if (shopManager == null) {
			shopManager = FindAnyObjectByType<ShopManager>();
		}
	}

	public void HandleTabSelected(string tabId) {
		if (shopManager == null) {
			SetMessage("ShopManager bulunamadı.");
			return;
		}

		if (!shopManager.TrySelectTab(tabId)) {
			return;
		}

		RefreshProducts();
	}

	public void HandleBuyClicked(ShopProductDefinitionSO product, int quantity) {
		if (shopManager == null) {
			SetMessage("ShopManager bulunamadı.");
			return;
		}

		ShopOperationResult result;
		shopManager.TryPurchase(product, quantity, out result);
		SetMessage(result.Message);
		RefreshProducts();
	}

	public void HandleSellClicked(ShopProductDefinitionSO product, int quantity) {
		if (shopManager == null) {
			SetMessage("ShopManager bulunamadı.");
			return;
		}

		ShopOperationResult result;
		shopManager.TrySell(product, quantity, out result);
		SetMessage(result.Message);
		RefreshProducts();
	}

	private void BuildTabs() {
		ClearTabs();
		if (shopManager == null || tabContainer == null || tabButtonPrefab == null) {
			return;
		}

		IReadOnlyList<ShopTabDefinition> tabs = shopManager.GetTabs();
		for (int i = 0; i < tabs.Count; i++) {
			ShopTabDefinition tab = tabs[i];
			if (tab == null) {
				continue;
			}

			ShopTabButtonUI tabButton = Instantiate(tabButtonPrefab, tabContainer);
			tabButton.Bind(this, tab, shopManager.SelectedTabId == tab.TabId);
			spawnedTabs.Add(tabButton);
		}
	}

	private void RefreshProducts() {
		ClearProducts();
		if (shopManager == null || productContainer == null || productItemPrefab == null) {
			return;
		}

		IReadOnlyList<ShopProductDefinitionSO> products = shopManager.GetProductsForSelectedTab();
		for (int i = 0; i < products.Count; i++) {
			ShopProductDefinitionSO product = products[i];
			if (product == null) {
				continue;
			}

			ShopProductItemUI itemUI = Instantiate(productItemPrefab, productContainer);
			itemUI.Bind(this, product);
			spawnedProducts.Add(itemUI);
		}

		RefreshSelectedTabTitle();
		RefreshTabSelection();
	}

	private void RefreshSelectedTabTitle() {
		if (selectedTabTitleText == null) {
			return;
		}

		if (shopManager == null) {
			selectedTabTitleText.text = "Shop";
			return;
		}

		IReadOnlyList<ShopTabDefinition> tabs = shopManager.GetTabs();
		for (int i = 0; i < tabs.Count; i++) {
			ShopTabDefinition tab = tabs[i];
			if (tab == null) {
				continue;
			}

			if (tab.TabId == shopManager.SelectedTabId) {
				selectedTabTitleText.text = string.IsNullOrWhiteSpace(tab.DisplayName) ? "Shop" : tab.DisplayName;
				return;
			}
		}

		selectedTabTitleText.text = "Shop";
	}

	private void RefreshTabSelection() {
		string selectedId = shopManager != null ? shopManager.SelectedTabId : string.Empty;
		for (int i = 0; i < spawnedTabs.Count; i++) {
			if (spawnedTabs[i] != null) {
				spawnedTabs[i].SetSelected(false);
			}
		}

		IReadOnlyList<ShopTabDefinition> tabs = shopManager != null ? shopManager.GetTabs() : null;
		for (int i = 0; tabs != null && i < tabs.Count && i < spawnedTabs.Count; i++) {
			ShopTabDefinition tab = tabs[i];
			ShopTabButtonUI button = spawnedTabs[i];
			if (tab == null || button == null) {
				continue;
			}

			button.SetSelected(tab.TabId == selectedId);
		}
	}

	private void SetMessage(string message) {
		if (messageText != null) {
			messageText.text = message;
		}
	}

	private void ClearTabs() {
		for (int i = 0; i < spawnedTabs.Count; i++) {
			ShopTabButtonUI item = spawnedTabs[i];
			if (item != null) {
				Destroy(item.gameObject);
			}
		}

		spawnedTabs.Clear();
	}

	private void ClearProducts() {
		for (int i = 0; i < spawnedProducts.Count; i++) {
			ShopProductItemUI item = spawnedProducts[i];
			if (item != null) {
				Destroy(item.gameObject);
			}
		}

		spawnedProducts.Clear();
	}
}
