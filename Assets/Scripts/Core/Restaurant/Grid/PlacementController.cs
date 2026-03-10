using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour {
	public Camera sceneCamera;
	public Material previewMaterial;

	[HideInInspector]
	public bool editMode = false;

	private PlaceableObjectSO currentData;
	private GameObject previewInstance;
	private GameObject autoChairPreviewInstance;
	private int rotationIndex = 0; // 0..3
	private bool manualRotation = false;
	private Vector2Int lastBaseCell = new(-999, -999);
	private Vector2Int autoChairPreviewBaseCell;
	private Vector2Int autoChairPreviewSize;
	private Quaternion autoChairPreviewRotation = Quaternion.identity;
	private bool hasAutoChairPreview;
	private int autoChairRotationIndex = -1;
	private int[] autoChairAllowedIndices;
	private Vector2Int lastAutoChairBaseCell = new(-999, -999);

	private GridManager grid;
	private BuildInventoryManager buildInventoryManager;
	private int objectsLayerMask = ~0;

	// hover/remove support
	private PlaceableObject lastHovered;
	private readonly Dictionary<Renderer, Material[]> lastHoverOriginalMats = new();

	void Start() {
		grid = GridManager.Instance;
		buildInventoryManager = FindFirstObjectByType<BuildInventoryManager>();
		if (sceneCamera == null) sceneCamera = Camera.main;
		int layerIndex = LayerMask.NameToLayer("Objects");
		objectsLayerMask = layerIndex >= 0 ? (1 << layerIndex) : ~0;
	}

	void Update() {
		// only allow placement/previewing when edit mode is enabled
		if (!editMode)
			return;

		// ensure grid and camera references
		if (grid == null) grid = GridManager.Instance;
		if (grid == null) return;
		if (buildInventoryManager == null) buildInventoryManager = FindFirstObjectByType<BuildInventoryManager>();
		if (sceneCamera == null) sceneCamera = Camera.main;
		if (sceneCamera == null) return;

		if (currentData != null && !HasAvailableStock(currentData)) {
			ClearSelection();
			return;
		}

		// cancel: if there's an active selection, right-click cancels it
		if (Input.GetMouseButtonDown(1) && currentData != null) {
			ClearSelection();
			return;
		}

		// raycast to plane at grid.origin.y
		Plane plane = new(Vector3.up, grid.origin);
		Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
		if (!plane.Raycast(ray, out float enter))
			return;
		Vector3 hit = ray.GetPoint(enter);

		// if nothing is selected, allow hovering and removal of placed objects
		if (currentData == null) {
			PlaceableObject hovered = null;
			// prefer a single raycast against the "Objects" layer to avoid scanning all hits
			if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f, objectsLayerMask)) {
				var po = hitInfo.collider.GetComponentInParent<PlaceableObject>();
				if (po != null) {
					// ignore preview instance (ghost) if present
					if (previewInstance != null) {
						if (po.gameObject == previewInstance) po = null;
						else if (po.transform.IsChildOf(previewInstance.transform)) po = null;
					}
					// only consider objects that are children of the GridManager (i.e. actually placed)
					if (po != null && grid != null && !po.transform.IsChildOf(grid.transform)) po = null;
					hovered = po;
				}
			}

			// update hover highlighting
			UpdateHover(hovered);

			if (hovered == null) UpdateAutoChairPreview(hit);
			else ClearAutoChairPreview();

			if (Input.GetKeyDown(KeyCode.R) && hovered == null && hasAutoChairPreview)
				RotateAutoChairPreview();

			if (Input.GetKeyDown(KeyCode.R) && hovered != null) {
				grid.RotatePlacedObject(hovered);
			}

			if (Input.GetMouseButtonDown(0) && hovered == null && hasAutoChairPreview) {
				var autoChairData = grid.AutoChairData;
				if (autoChairData != null && TryConsumeForPlacement(autoChairData)) {
					var placedChair = grid.Place(autoChairData, autoChairPreviewBaseCell, autoChairPreviewSize, autoChairPreviewRotation);
					if (placedChair == null && buildInventoryManager != null)
						buildInventoryManager.AddPlaceable(autoChairData, 1);
				}
			}

			// handle removal on right click when nothing is selected
			if (Input.GetMouseButtonDown(1)) {
				if (lastHovered != null) {
					// capture the hovered object before clearing hover state so we can
					// restore materials and then remove the correct object.
					var toRemove = lastHovered;
					ClearHover();
					grid.Remove(toRemove);
				}
			}

			return;
		} else {
			// when we have an active selection, clear any hover highlight
			ClearHover();
			ClearAutoChairPreview();
		}

		Vector2Int size = (rotationIndex % 2 == 1) ? new Vector2Int(currentData.size.y, currentData.size.x) : currentData.size;
		Vector2Int baseCell = grid.WorldToCell(hit, size);

		// reset manualRotation when base cell changes so auto-orient can apply
		if (baseCell != lastBaseCell) {
			lastBaseCell = baseCell;
			manualRotation = false;
			// if there's exactly one chair candidate, snap rotationIndex to it (handled below when candidates computed)
		}

		// Determine chair-related candidates (if this is a chair)
		int[] chairAllowedIndices = null;
		if (currentData.prefab != null && currentData.prefab.TryGetComponent<ChairBehavior>(out _)) {
			// Delegate the computation to GridManager to avoid duplicating table lookups
			var indices = grid.GetValidChairRotationIndices(currentData, baseCell, size);
			if (indices != null && indices.Length > 0) chairAllowedIndices = indices;
		}

		// handle rotate input now that we know chair candidates
		if (Input.GetKeyDown(KeyCode.R)) {
			manualRotation = true;
			if (chairAllowedIndices != null && chairAllowedIndices.Length > 0) {
				int pos = System.Array.IndexOf(chairAllowedIndices, rotationIndex);
				if (pos == -1) rotationIndex = chairAllowedIndices[0];
				else rotationIndex = chairAllowedIndices[(pos + 1) % chairAllowedIndices.Length];
			} else {
				rotationIndex = (rotationIndex + 1) % 4;
			}
			RefreshPreview();
		}

		// compute rotation (used for preview and placement). Start from -90 degrees base.
		Quaternion rot = Quaternion.Euler(0f, -90f + rotationIndex * 90f, 0f);

		bool canPlace = grid.CanPlace(currentData, baseCell, size, rot);

		// If this is a chair and there's exactly one adjacent table, auto-orient to it unless the user manually rotated
		if (currentData.prefab != null && currentData.prefab.TryGetComponent<ChairBehavior>(out _)) {
			if (!manualRotation && chairAllowedIndices != null && chairAllowedIndices.Length == 1) {
				rotationIndex = chairAllowedIndices[0];
				rot = Quaternion.Euler(0f, -90f + rotationIndex * 90f, 0f);
			}
		}

		// position preview
		if (previewInstance != null) {
			previewInstance.transform.SetPositionAndRotation(grid.GetWorldPositionForCell(baseCell, size), rot);

			// colorize
			var renderers = previewInstance.GetComponentsInChildren<Renderer>();
			foreach (var r in renderers) {
				if (r == null) continue;
				foreach (var m in r.materials) {
					Color c = canPlace ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
					if (m.HasProperty("_Color")) m.color = c;
				}
			}
		}

		if (Input.GetMouseButtonDown(0)) {
			if (canPlace) {
				if (!TryConsumeForPlacement(currentData))
					return;

				var placedObject = grid.Place(currentData, baseCell, size, rot);
				if (placedObject == null) {
					if (buildInventoryManager != null)
						buildInventoryManager.AddPlaceable(currentData, 1);
					return;
				}

				if (!HasAvailableStock(currentData))
					ClearSelection();
			}
		}
	}

	public void Select(PlaceableObjectSO data) {
		if (!editMode) {
			Debug.Log("Select ignored because edit mode is off.");
			return;
		}
		if (!HasAvailableStock(data)) {
			Debug.Log("Select ignored because there is no stock for the selected item.");
			return;
		}
		currentData = data;
		rotationIndex = 0;
		ClearAutoChairPreview();
		CreatePreviewInstance();
	}

	public void SetEditMode(bool enabled) {
		editMode = enabled;
		if (!editMode) {
			// clear any active selection when leaving edit mode
			ClearSelection();
			// also clear any hover highlight
			ClearHover();
			ClearAutoChairPreview();
		}
	}

	public void ClearSelection() {
		currentData = null;
		rotationIndex = 0;
		if (previewInstance != null) Destroy(previewInstance);
		previewInstance = null;
	}

	private void UpdateAutoChairPreview(Vector3 hit) {
		if (grid == null) {
			ClearAutoChairPreview();
			return;
		}

		var autoChairData = grid.AutoChairData;
		if (autoChairData == null || autoChairData.prefab == null) {
			ClearAutoChairPreview();
			return;
		}

		if (!HasAvailableStock(autoChairData)) {
			ClearAutoChairPreview();
			return;
		}

		var baseCell = grid.WorldToCell(hit, autoChairData.size);
		var size = autoChairData.size;
		if (baseCell != lastAutoChairBaseCell) {
			lastAutoChairBaseCell = baseCell;
			autoChairRotationIndex = -1;
		}

		autoChairAllowedIndices = grid.GetValidChairRotationIndices(autoChairData, baseCell, size);
		if (autoChairAllowedIndices == null || autoChairAllowedIndices.Length == 0) {
			ClearAutoChairPreview();
			return;
		}

		if (autoChairRotationIndex < 0 || System.Array.IndexOf(autoChairAllowedIndices, autoChairRotationIndex) < 0)
			autoChairRotationIndex = autoChairAllowedIndices[0];

		var rotation = Quaternion.Euler(0f, -90f + autoChairRotationIndex * 90f, 0f);
		if (!grid.CanPlace(autoChairData, baseCell, size, rotation)) {
			ClearAutoChairPreview();
			return;
		}

		autoChairPreviewBaseCell = baseCell;
		autoChairPreviewSize = size;
		autoChairPreviewRotation = rotation;
		hasAutoChairPreview = true;

		if (autoChairPreviewInstance == null)
			autoChairPreviewInstance = CreatePreviewObject(autoChairData.prefab);

		if (autoChairPreviewInstance != null) {
			autoChairPreviewInstance.transform.SetPositionAndRotation(grid.GetWorldPositionForCell(baseCell, size), rotation);
			ApplyPreviewColor(autoChairPreviewInstance, new Color(0f, 1f, 0f, 0.6f));
		}
	}

	private void RotateAutoChairPreview() {
		if (!hasAutoChairPreview || autoChairAllowedIndices == null || autoChairAllowedIndices.Length <= 1)
			return;

		int pos = System.Array.IndexOf(autoChairAllowedIndices, autoChairRotationIndex);
		if (pos < 0) autoChairRotationIndex = autoChairAllowedIndices[0];
		else autoChairRotationIndex = autoChairAllowedIndices[(pos + 1) % autoChairAllowedIndices.Length];

		autoChairPreviewRotation = Quaternion.Euler(0f, -90f + autoChairRotationIndex * 90f, 0f);
		if (autoChairPreviewInstance != null) {
			autoChairPreviewInstance.transform.SetPositionAndRotation(grid.GetWorldPositionForCell(autoChairPreviewBaseCell, autoChairPreviewSize), autoChairPreviewRotation);
			ApplyPreviewColor(autoChairPreviewInstance, new Color(0f, 1f, 0f, 0.6f));
		}
	}

	private void ClearAutoChairPreview() {
		hasAutoChairPreview = false;
		autoChairRotationIndex = -1;
		autoChairAllowedIndices = null;
		if (autoChairPreviewInstance != null) Destroy(autoChairPreviewInstance);
		autoChairPreviewInstance = null;
	}

	private void CreatePreviewInstance() {
		if (previewInstance != null) Destroy(previewInstance);
		if (currentData == null || currentData.prefab == null) return;
		previewInstance = CreatePreviewObject(currentData.prefab);
	}

	private GameObject CreatePreviewObject(GameObject prefab) {
		if (prefab == null) return null;
		var instance = Instantiate(prefab);
		// make preview non-interactive
		foreach (var c in instance.GetComponentsInChildren<Collider>()) c.enabled = false;
		foreach (var mb in instance.GetComponentsInChildren<MonoBehaviour>()) {
			// disable behaviours so they don't run on preview
			mb.enabled = false;
		}

		// apply preview material if provided
		if (previewMaterial != null) {
			foreach (var r in instance.GetComponentsInChildren<Renderer>()) {
				var mats = new Material[r.sharedMaterials.Length];
				for (int i = 0; i < mats.Length; i++) mats[i] = previewMaterial;
				r.materials = mats;
			}
		}

		return instance;
	}

	private void ApplyPreviewColor(GameObject instance, Color color) {
		if (instance == null) return;
		var renderers = instance.GetComponentsInChildren<Renderer>();
		foreach (var r in renderers) {
			if (r == null) continue;
			var mats = r.materials;
			for (int i = 0; i < mats.Length; i++) {
				if (mats[i] != null && mats[i].HasProperty("_Color"))
					mats[i].color = color;
			}
		}
	}

	private void RefreshPreview() {
		// re-create preview to update orientation/size
		CreatePreviewInstance();
	}

	private void UpdateHover(PlaceableObject hovered) {
		if (hovered == lastHovered) return;
		// clear previous
		if (lastHovered != null) ClearHover();
		if (hovered == null) return;
		// store original materials and apply red tint
		lastHoverOriginalMats.Clear();
		var renderers = hovered.GetComponentsInChildren<Renderer>();
		foreach (var r in renderers) {
			if (r == null) continue;
			var orig = r.sharedMaterials;
			lastHoverOriginalMats[r] = orig;
			var mats = new Material[orig.Length];
			for (int i = 0; i < orig.Length; i++) {
				mats[i] = new Material(orig[i]);
				if (mats[i].HasProperty("_Color")) mats[i].color = new Color(1f, 0.92f, 0f, 0.9f); // yellow
			}
			r.materials = mats;
		}
		lastHovered = hovered;
	}

	private void ClearHover() {
		if (lastHovered == null) return;
		foreach (var kv in lastHoverOriginalMats) {
			var r = kv.Key;
			if (r == null) continue;
			r.materials = kv.Value;
		}
		lastHoverOriginalMats.Clear();
		lastHovered = null;
	}

	private bool HasAvailableStock(PlaceableObjectSO data) {
		if (data == null) return false;
		if (buildInventoryManager == null) return true;
		return buildInventoryManager.GetQuantity(data) > 0;
	}

	private bool TryConsumeForPlacement(PlaceableObjectSO data) {
		if (data == null) return false;
		if (buildInventoryManager == null) return true;
		return buildInventoryManager.TryConsumeForPlacement(data);
	}
}
