using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildPaletteItemUI : MonoBehaviour {
	[SerializeField] private Button selectButton;
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private TMP_Text quantityText;
	[SerializeField] private Image iconImage;

	private PlaceableObjectSO placeableData;
	private BuildPaletteUI owner;

	public void Bind(BuildPaletteUI paletteOwner, PlaceableObjectSO data, int quantity) {
		owner = paletteOwner;
		placeableData = data;

		if (titleText != null) {
			titleText.text = data != null ? data.Name : "Eşya";
		}

		if (quantityText != null) {
			quantityText.text = "x" + quantity;
		}

		if (iconImage != null) {
			iconImage.enabled = false;
		}

		if (selectButton != null) {
			selectButton.onClick.RemoveListener(HandleClicked);
			selectButton.onClick.AddListener(HandleClicked);
			selectButton.interactable = data != null && quantity > 0;
		}
	}

	private void HandleClicked() {
		if (owner == null || placeableData == null) {
			return;
		}

		owner.HandleItemSelected(placeableData);
	}
}
