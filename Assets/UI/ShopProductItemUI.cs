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

        int quantity = GetRequestedQuantity();

        string purchaseReason;
        bool canPurchase = false;
        if (controller != null)
        {
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                canPurchase = shopManager.CanPurchase(product, quantity, out purchaseReason);
            }
        }

        string sellReason;
        bool canSell = false;
        if (controller != null)
        {
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                canSell = shopManager.CanSell(product, quantity, out sellReason);
            }
        }

        SetButtonState(buyButton, canPurchase);
        SetButtonState(sellButton, canSell);

        if (availabilityText != null)
        {
            if (canPurchase)
            {
                availabilityText.text = "Satın alınabilir";
            }
            else
            {
                availabilityText.text = "Şu anda satın alınamaz";
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

        if (buyPriceText != null)
        {
            buyPriceText.text = product != null ? "Alış: " + product.BuyPrice : "Alış: -";
        }

        if (sellPriceText != null)
        {
            sellPriceText.text = product != null ? "Satış: " + product.SellPrice : "Satış: -";
        }

        if (quantityInfoText != null)
        {
            quantityInfoText.text = product != null ? "Maks: " + product.MaxTransactionQuantity : "Maks: -";
        }

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
            quantityInputField.text = "1";
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

    private void OnBuyClicked()
    {
        if (controller == null || product == null)
        {
            return;
        }

        controller.HandleBuyClicked(product, GetRequestedQuantity());
    }

    private void OnSellClicked()
    {
        if (controller == null || product == null)
        {
            return;
        }

        controller.HandleSellClicked(product, GetRequestedQuantity());
    }

    private int GetRequestedQuantity()
    {
        if (product == null)
        {
            return 1;
        }

        if (quantityInputField == null)
        {
            return 1;
        }

        int quantity;
        bool parseSucceeded = int.TryParse(quantityInputField.text, out quantity);
        if (!parseSucceeded)
        {
            return 1;
        }

        if (quantity < 1)
        {
            quantity = 1;
        }

        if (quantity > product.MaxTransactionQuantity)
        {
            quantity = product.MaxTransactionQuantity;
        }

        return quantity;
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
