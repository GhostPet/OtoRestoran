using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tek bir ürün kartının UI mantığını yönetir.
/// Görselleri doldurur, adet bilgisini okur ve satın al/sat butonlarını controller'a yönlendirir.
/// </summary>
public class ShopProductItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text productNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text buyPriceText;
    [SerializeField] private TMP_Text sellPriceText;
    [SerializeField] private TMP_Text quantityInfoText;
    [SerializeField] private TMP_Text ownedQuantityText;
    [SerializeField] private TMP_Text availabilityText;
    [SerializeField] private TMP_InputField quantityInputField;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;

    private ShopUIController controller;
    private ShopProductDefinitionSO product;

    public void Bind(ShopUIController owner, ShopProductDefinitionSO productDefinition)
    {
        controller = owner;
        product = productDefinition;

        ApplyVisuals();
        RegisterButtons();
        RefreshInteractableState();
    }

    public void RefreshInteractableState()
    {
        if (controller == null || product == null)
        {
            SetButtonState(buyButton, false);
            SetButtonState(sellButton, false);
            return;
        }

        int quantity = GetTransactionQuantity();

        RefreshPricingTexts(quantity);
        RefreshOwnedQuantityText();

        string purchaseReason = string.Empty;
        bool canPurchase = false;
        if (controller != null && controller.ShopManager != null)
        {
            canPurchase = controller.ShopManager.CanPurchase(product, quantity, out purchaseReason);
        }

        string sellReason = string.Empty;
        bool canSell = false;
        if (controller != null && controller.ShopManager != null)
        {
            canSell = controller.ShopManager.CanSell(product, quantity, out sellReason);
        }

        SetButtonState(buyButton, canPurchase);

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(product.CanBeSold);
            SetButtonState(sellButton, canSell && product.CanBeSold);
        }

        if (sellPriceText != null)
        {
            sellPriceText.gameObject.SetActive(product.CanBeSold);
        }

        if (availabilityText != null)
        {
            if (canPurchase)
            {
                availabilityText.text = "Satın alınabilir";
            }
            else
            {
                availabilityText.text = string.IsNullOrWhiteSpace(purchaseReason) ? "Şu anda satın alınamaz" : purchaseReason;
            }
        }
    }

    private void ApplyVisuals()
    {
        if (productNameText != null)
        {
            productNameText.text = product != null ? product.DisplayName : "Ürün";
        }

        if (descriptionText != null)
        {
            descriptionText.text = product != null ? product.Description : string.Empty;
        }

        RefreshPricingTexts(GetTransactionQuantity());
        RefreshOwnedQuantityText();

        if (iconImage != null)
        {
            if (product != null && product.Icon != null)
            {
                iconImage.sprite = product.Icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        if (quantityInputField != null)
        {
            quantityInputField.gameObject.SetActive(false);
        }
    }

    private void RegisterButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnSellClicked);
            sellButton.onClick.AddListener(OnSellClicked);
        }

        if (quantityInputField != null)
        {
            quantityInputField.onEndEdit.RemoveListener(HandleQuantityEdited);
            quantityInputField.onEndEdit.AddListener(HandleQuantityEdited);
        }
    }

    private void HandleQuantityEdited(string value)
    {
        if (quantityInputField == null)
        {
            return;
        }

        int quantity = GetRequestedQuantity();
        quantityInputField.text = quantity.ToString();
        RefreshInteractableState();
    }

    private void RefreshPricingTexts(int quantity)
    {
        if (product == null)
        {
            if (buyPriceText != null)
            {
                buyPriceText.text = "Alış: -";
            }

            if (sellPriceText != null)
            {
                sellPriceText.text = "Satış: -";
            }

            if (quantityInfoText != null)
            {
                quantityInfoText.text = "Adet: -";
            }

            return;
        }

        int buyUnitPrice = product.GetBuyUnitPrice(quantity);
        int sellUnitPrice = product.GetSellUnitPrice(quantity);
        int buyTotalPrice = product.GetBuyTotalPrice(quantity);
        int sellTotalPrice = product.GetSellTotalPrice(quantity);

        if (quantityInfoText != null)
        {
            quantityInfoText.text = product.CanBeSold ? "Al / Sat Miktarı: " + quantity : "Alım Miktarı: " + quantity;
        }

        if (buyPriceText != null)
        {
            buyPriceText.text = "Alış Fiyatı: " + buyTotalPrice;
        }

        if (sellPriceText != null)
        {
            sellPriceText.text = "Satış Fiyatı: " + sellTotalPrice;
        }
    }

    private void RefreshOwnedQuantityText()
    {
        if (ownedQuantityText == null)
        {
            return;
        }

        if (controller == null || controller.ShopManager == null || product == null)
        {
            ownedQuantityText.text = "Envanter: -";
            return;
        }

        ownedQuantityText.text = "Envanter: " + controller.ShopManager.GetOwnedQuantity(product);
    }

    private void OnBuyClicked()
    {
        if (controller == null || product == null)
        {
            return;
        }

        controller.HandleBuyClicked(product, GetTransactionQuantity());
    }

    private void OnSellClicked()
    {
        if (controller == null || product == null)
        {
            return;
        }

        controller.HandleSellClicked(product, GetTransactionQuantity());
    }

    private int GetRequestedQuantity()
    {
        return GetTransactionQuantity();
    }

    private int GetTransactionQuantity()
    {
        if (product == null)
        {
            return 1;
        }

        return product.TransactionQuantity > 0 ? product.TransactionQuantity : 1;
    }

    private void SetButtonState(Button targetButton, bool isEnabled)
    {
        if (targetButton == null)
        {
            return;
        }

        targetButton.interactable = isEnabled;
    }
}
