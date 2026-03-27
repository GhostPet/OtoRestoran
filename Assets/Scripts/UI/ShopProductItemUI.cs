using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopProductItemUI : MonoBehaviour {
	[SerializeField] private Image iconImage;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private TMP_Text priceText;
	[SerializeField] private TMP_Text sellPriceText;
	[SerializeField] private TMP_Text ownedText;
	[SerializeField] private Button buyButton;
	[SerializeField] private Button sellButton;

	private ShopWindowUI owner;
	private ShopProductDefinitionSO product;

	public void Bind(ShopWindowUI windowOwner, ShopProductDefinitionSO productDefinition) {
		owner = windowOwner;
		product = productDefinition;

		Refresh();

		if (buyButton != null) {
			buyButton.onClick.RemoveListener(HandleBuyClicked);
			buyButton.onClick.AddListener(HandleBuyClicked);
		}

		if (sellButton != null) {
			sellButton.onClick.RemoveListener(HandleSellClicked);
			sellButton.onClick.AddListener(HandleSellClicked);
		}
	}

	public void Refresh() {
		if (nameText != null) {
			nameText.text = product != null ? product.DisplayName : "Ürün";
		}

		if (priceText != null) {
			priceText.text = product != null ? "Alış: " + product.GetBuyTotalPrice(product.TransactionQuantity) : "Alış: -";
		}

		if (sellPriceText != null) {
			sellPriceText.text = product != null ? "Satış: " + product.GetSellTotalPrice(product.TransactionQuantity) : "Satış: -";
			sellPriceText.gameObject.SetActive(product != null && product.CanBeSold);
		}

		if (iconImage != null) {
			if (product != null && product.Icon != null) {
				iconImage.sprite = product.Icon;
				iconImage.enabled = true;
			} else {
				iconImage.enabled = false;
			}
		}

		RefreshOwnership();
		RefreshButtons();
	}

	private void RefreshOwnership() {
		if (ownedText == null) {
			return;
		}

		if (owner == null || owner.ShopManager == null || product == null) {
			ownedText.text = "Envanter: -";
			return;
		}

		if (product.StorageType == ShopProductStorageType.BuildInventory) {
			int stock = owner.ShopManager.GetOwnedQuantity(product);
			int placed = owner.ShopManager.GetPlacedQuantity(product);
			ownedText.text = "Stok: " + stock + " | Kullanımda: " + placed;
			return;
		}

		ownedText.text = "Envanter: " + owner.ShopManager.GetOwnedQuantity(product);
	}

	private void RefreshButtons() {
		if (owner == null || owner.ShopManager == null || product == null) {
			SetButtonState(false, false);
			return;
		}

		string purchaseReason;
		bool canPurchase = owner.ShopManager.CanPurchase(product, product.TransactionQuantity, out purchaseReason);

		string sellReason;
		bool canSell = owner.ShopManager.CanSell(product, product.TransactionQuantity, out sellReason);

		if (buyButton != null) {
			buyButton.interactable = canPurchase;
		}

		if (sellButton != null) {
			sellButton.gameObject.SetActive(product.CanBeSold);
			sellButton.interactable = canSell && product.CanBeSold;
		}
	}

	private void SetButtonState(bool canBuy, bool canSell) {
		if (buyButton != null) {
			buyButton.interactable = canBuy;
		}

		if (sellButton != null) {
			sellButton.interactable = canSell;
		}
	}

	private void HandleBuyClicked() {
		if (owner == null || product == null) {
			return;
		}

		owner.HandleBuyClicked(product, product.TransactionQuantity);
	}

	private void HandleSellClicked() {
		if (owner == null || product == null) {
			return;
		}

		owner.HandleSellClicked(product, product.TransactionQuantity);
	}
}
