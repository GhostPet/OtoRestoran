using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;

// Small helper to wire UI buttons to the PlacementController.
// Attach this to a Canvas or UI container. Assign matching arrays of Buttons and PlaceableObjectSO
// or leave `controller` empty so it will find the PlacementController in the scene.
public class PlacementUI : MonoBehaviour {
	public Button[] buttons;
	public PlaceableObjectSO[] placeables;
	public PlacementController controller; // optional, auto-find if null
	public BuildInventoryManager buildInventoryManager;

	private readonly Dictionary<Button, TextMeshProUGUI> buttonLabels = new();
	private readonly Dictionary<Button, string> buttonBaseTexts = new();

	void Awake() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (buildInventoryManager == null) buildInventoryManager = FindAnyObjectByType<BuildInventoryManager>();
	}

	[Header("Edit Mode UI")]
	public Button editButton;
	public TextMeshProUGUI editButtonText;
	public GameObject itemList;

	[Header("NavMesh")]
	public NavMeshSurface navMeshSurface; // optional, assign in inspector

	void Start() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (buildInventoryManager == null) buildInventoryManager = FindAnyObjectByType<BuildInventoryManager>();

		// wire edit button if provided
		if (editButton != null) {
			if (controller == null) controller = FindAnyObjectByType<PlacementController>();
			editButton.onClick.AddListener(ToggleEditMode);
			UpdateEditButtonVisuals();
		}

		if (buttons != null && placeables != null) {
			int count = Mathf.Min(buttons.Length, placeables.Length);
			for (int i = 0; i < count; i++) {
				var btn = buttons[i];
				var data = placeables[i];
				if (btn == null || data == null) continue;
				CacheButtonLabel(btn);
				// capture local variables to avoid closure issue
				btn.onClick.AddListener(() => OnSelect(data));
			}
		}

		RefreshPlaceableButtons();
	}

	void OnEnable() {
		if (buildInventoryManager == null) buildInventoryManager = FindAnyObjectByType<BuildInventoryManager>();
		if (buildInventoryManager != null)
			buildInventoryManager.InventoryChanged += HandleBuildInventoryChanged;

		RefreshPlaceableButtons();
	}

	void OnDisable() {
		if (buildInventoryManager != null)
			buildInventoryManager.InventoryChanged -= HandleBuildInventoryChanged;
	}

	private void ToggleEditMode() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (controller == null) return;
        SetEditMode(!controller.editMode);
	}

	public void SetEditMode(bool enabled) {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (controller == null) return;

		controller.SetEditMode(enabled);

		if (!enabled) {
			if (navMeshSurface != null) {
				navMeshSurface.BuildNavMesh();
			}
		}

		UpdateEditButtonVisuals();
	}

	private void UpdateEditButtonVisuals() {
		if (editButton == null || editButtonText == null) return;
		bool on = controller != null && controller.editMode;
		editButtonText.text = on ? "Düzenleme: Açık" : "Düzenleme: Kapalı";
		editButtonText.color = on ? Color.green : Color.red;
		itemList.SetActive(on);
		if (editButton.TryGetComponent<Image>(out var img)) img.color = on ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
		RefreshPlaceableButtons();
	}

	public void OnSelect(PlaceableObjectSO data) {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (controller == null) {
			Debug.LogWarning("PlacementController not found in scene.");
			return;
		}
		controller.Select(data);
	}

	public void OnCancel() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (controller != null) {
			controller.ClearSelection();
		}
	}

	private void HandleBuildInventoryChanged() {
		RefreshPlaceableButtons();
	}

	private void RefreshPlaceableButtons() {
		if (buttons == null || placeables == null) return;

		int count = Mathf.Min(buttons.Length, placeables.Length);
		bool editModeEnabled = controller != null && controller.editMode;
		for (int i = 0; i < count; i++) {
			var btn = buttons[i];
			var data = placeables[i];
			if (btn == null || data == null) continue;

			int stock = buildInventoryManager != null ? buildInventoryManager.GetQuantity(data) : 0;
			btn.interactable = editModeEnabled && (buildInventoryManager == null || stock > 0);

			if (buttonLabels.TryGetValue(btn, out var label) && label != null && buttonBaseTexts.TryGetValue(btn, out var baseText))
				label.text = buildInventoryManager != null ? baseText + " (" + stock + ")" : baseText;
		}
	}

	private void CacheButtonLabel(Button button) {
		if (button == null || buttonLabels.ContainsKey(button)) return;

		var label = button.GetComponentInChildren<TextMeshProUGUI>();
		buttonLabels[button] = label;
		buttonBaseTexts[button] = label != null ? label.text : button.name;
	}
}
