using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;

// Small helper to wire UI buttons to the PlacementController.
// Attach this to a Canvas or UI container. Assign matching arrays of Buttons and PlaceableData
// or leave `controller` empty so it will find the PlacementController in the scene.
public class PlacementUI : MonoBehaviour {
	public Button[] buttons;
	public PlaceableData[] placeables;
	public PlacementController controller; // optional, auto-find if null

	[Header("Edit Mode UI")]
	public Button editButton;
	public TextMeshProUGUI editButtonText;
	public GameObject itemList;

	[Header("NavMesh")]
	public NavMeshSurface navMeshSurface; // optional, assign in inspector

	void Start() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();

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
				// capture local variables to avoid closure issue
				btn.onClick.AddListener(() => OnSelect(data));
			}
		}
	}

	private void ToggleEditMode() {
		if (controller == null) controller = FindAnyObjectByType<PlacementController>();
		if (controller == null) return;
		bool newMode = !controller.editMode;
		controller.SetEditMode(newMode);

		// If edit mode was turned off, rebuild the NavMeshSurface if assigned
		if (!newMode) {
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
	}

	public void OnSelect(PlaceableData data) {
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
}
