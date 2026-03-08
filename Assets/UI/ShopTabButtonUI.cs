using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tek bir shop sekme butonunun görselini ve tıklama davranışını yönetir.
/// Veriyi ShopUIController'dan alır ve tıklanınca tekrar controller'a haber verir.
/// </summary>
public class ShopTabButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic selectedHighlight;
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color normalTextColor = Color.black;

    private ShopUIController controller;
    private ShopTabDefinition tabDefinition;

    public string TabId
    {
        get
        {
            if (tabDefinition == null)
            {
                return string.Empty;
            }

            return tabDefinition.TabId;
        }
    }

    public void Bind(ShopUIController owner, ShopTabDefinition tab, bool isSelected)
    {
        controller = owner;
        tabDefinition = tab;

        if (titleText != null)
        {
            titleText.text = tab != null ? tab.DisplayName : "Sekme";
        }

        if (iconImage != null)
        {
            if (tab != null && tab.Icon != null)
            {
                iconImage.sprite = tab.Icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.gameObject.SetActive(isSelected);
        }

        if (titleText != null)
        {
            titleText.color = isSelected ? selectedTextColor : normalTextColor;
        }
    }

    private void OnClicked()
    {
        if (controller == null)
        {
            return;
        }

        if (tabDefinition == null)
        {
            return;
        }

        controller.HandleTabSelected(tabDefinition.TabId);
    }
}
