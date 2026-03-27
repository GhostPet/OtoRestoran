using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildPaletteUI : MonoBehaviour {
	[SerializeField] private BuildInventoryManager buildInventoryManager;
	[SerializeField] private PlacementController placementController;
	[SerializeField] private Transform itemContainer;
	[SerializeField] private BuildPaletteItemUI itemPrefab;
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private Button closeButton;
	[SerializeField] private bool startHidden = true;

	private readonly List<BuildPaletteItemUI> spawnedItems = new List<BuildPaletteItemUI>();
	private bool visibilityInitialized;

	public void SetReferences(BuildInventoryManager buildInventory, PlacementController placement) {
		Unsubscribe();

		buildInventoryManager = buildInventory;
		placementController = placement;

		Subscribe();
		if (gameObject.activeSelf) {
			Refresh();
		}
	}

	private void Start() {
		if (startHidden && !visibilityInitialized) {
			gameObject.SetActive(false);
		}
	}

	private void OnEnable() {
		Subscribe();
		Refresh();
	}

	private void OnDisable() {
		Unsubscribe();
	}

	public void SetVisible(bool isVisible) {
		visibilityInitialized = true;
		gameObject.SetActive(isVisible);
		if (isVisible) {
			Refresh();
		}
	}

	public void Refresh() {
		ClearItems();

		if (titleText != null) {
			titleText.text = "Depo";
		}

		if (closeButton != null) {
			closeButton.onClick.RemoveListener(HandleCloseClicked);
			closeButton.onClick.AddListener(HandleCloseClicked);
		}

		if (buildInventoryManager == null || itemContainer == null || itemPrefab == null) {
			return;
		}

		List<BuildInventoryEntry> snapshot = buildInventoryManager.CreateSnapshot();
		for (int i = 0; i < snapshot.Count; i++) {
			BuildInventoryEntry entry = snapshot[i];
			if (entry == null || entry.PlaceableData == null || entry.Quantity <= 0 || !entry.PlaceableData.ShowInStorage) {
				continue;
			}

			BuildPaletteItemUI itemUI = Instantiate(itemPrefab, itemContainer);
			itemUI.Bind(this, entry.PlaceableData, entry.Quantity);
			spawnedItems.Add(itemUI);
		}
	}

	public void HandleItemSelected(PlaceableObjectSO placeableData) {
		if (placementController == null) {
			return;
		}

		placementController.Select(placeableData);
	}

	private void Subscribe() {
		if (!isActiveAndEnabled) {
			return;
		}

		if (buildInventoryManager != null) {
			buildInventoryManager.InventoryChanged -= HandleInventoryChanged;
			buildInventoryManager.InventoryChanged += HandleInventoryChanged;
		}
	}

	private void Unsubscribe() {
		if (buildInventoryManager != null) {
			buildInventoryManager.InventoryChanged -= HandleInventoryChanged;
		}
	}

	private void HandleInventoryChanged() {
		Refresh();
	}

	private void HandleCloseClicked() {
		if (placementController != null) {
			placementController.SetEditMode(false);
		}

		SetVisible(false);
	}

	private void ClearItems() {
		for (int i = 0; i < spawnedItems.Count; i++) {
			BuildPaletteItemUI item = spawnedItems[i];
			if (item != null) {
				Destroy(item.gameObject);
			}
		}

		spawnedItems.Clear();
	}
}
