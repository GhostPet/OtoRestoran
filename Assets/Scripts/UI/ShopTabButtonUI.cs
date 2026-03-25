using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopTabButtonUI : MonoBehaviour {
	[SerializeField] private Button button;
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private Image iconImage;
	[SerializeField] private Graphic selectedHighlight;
	[SerializeField] private Color selectedTextColor = Color.white;
	[SerializeField] private Color normalTextColor = Color.black;

	private ShopWindowUI owner;
	private ShopTabDefinition tabDefinition;

	public void Bind(ShopWindowUI windowOwner, ShopTabDefinition tab, bool isSelected) {
		owner = windowOwner;
		tabDefinition = tab;

		if (titleText != null) {
			titleText.text = tab != null ? tab.DisplayName : "Sekme";
			titleText.color = isSelected ? selectedTextColor : normalTextColor;
		}

		if (iconImage != null) {
			if (tab != null && tab.Icon != null) {
				iconImage.sprite = tab.Icon;
				iconImage.enabled = true;
			} else {
				iconImage.enabled = false;
			}
		}

		if (button != null) {
			button.onClick.RemoveListener(HandleClicked);
			button.onClick.AddListener(HandleClicked);
		}

		SetSelected(isSelected);
	}

	public void SetSelected(bool isSelected) {
		if (selectedHighlight != null) {
			selectedHighlight.gameObject.SetActive(isSelected);
		}

		if (titleText != null) {
			titleText.color = isSelected ? selectedTextColor : normalTextColor;
		}
	}

	private void HandleClicked() {
		if (owner == null || tabDefinition == null) {
			return;
		}

		owner.HandleTabSelected(tabDefinition.TabId);
	}
}
