using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour {
	public Camera sceneCamera;
	public Material previewMaterial;

	[HideInInspector]
	public bool editMode = false;

	private PlaceableData currentData;
	private GameObject previewInstance;
	private int rotationIndex = 0; // 0..3
	private bool manualRotation = false;
	private Vector2Int lastBaseCell = new(-999, -999);

	private GridManager grid;

	// hover/remove support
	private PlaceableObject lastHovered;
	private readonly Dictionary<Renderer, Material[]> lastHoverOriginalMats = new();

	void Start() {
		grid = GridManager.Instance;
		if (sceneCamera == null) sceneCamera = Camera.main;
	}

	void Update() {
		// only allow placement/previewing when edit mode is enabled
		if (!editMode)
			return;

		// ensure grid and camera references
		if (grid == null) grid = GridManager.Instance;
		if (grid == null) return;
		if (sceneCamera == null) sceneCamera = Camera.main;
		if (sceneCamera == null) return;

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
			int layerIndex = LayerMask.NameToLayer("Objects");
			int layerMask = layerIndex >= 0 ? (1 << layerIndex) : ~0;
			if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f, layerMask)) {
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
			TableBehavior[] allTables = FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
			System.Collections.Generic.List<int> indices = new();
			Vector3 worldPos = grid.GetWorldPositionForCell(baseCell, size);
			for (int i = 0; i < allTables.Length; i++) {
				var table = allTables[i];
				if (table == null) continue;
				if (!table.IsSeatCell(baseCell)) continue;
				Vector3 tpos = table.Location != null ? table.Location.position : table.transform.position;
				Vector3 dir = tpos - worldPos;
				dir.y = 0f;
				if (dir.sqrMagnitude < 0.0001f) continue;
				float y = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
				float snapped = Mathf.Round(y / 90f) * 90f;
				int idx = ((int)Mathf.Round((snapped + 90f) / 90f)) % 4;
				if (!indices.Contains(idx)) indices.Add(idx);
			}
			if (indices.Count > 0) chairAllowedIndices = indices.ToArray();
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
				grid.Place(currentData, baseCell, size, rot);
			}
		}
	}

	public void Select(PlaceableData data) {
		if (!editMode) {
			Debug.Log("Select ignored because edit mode is off.");
			return;
		}
		currentData = data;
		rotationIndex = 0;
		CreatePreviewInstance();
	}

	public void SetEditMode(bool enabled) {
		editMode = enabled;
		if (!editMode) {
			// clear any active selection when leaving edit mode
			ClearSelection();
			// also clear any hover highlight
			ClearHover();
		}
	}

	public void ClearSelection() {
		currentData = null;
		rotationIndex = 0;
		if (previewInstance != null) Destroy(previewInstance);
		previewInstance = null;
	}

	private void CreatePreviewInstance() {
		if (previewInstance != null) Destroy(previewInstance);
		if (currentData == null || currentData.prefab == null) return;
		previewInstance = Instantiate(currentData.prefab);
		// make preview non-interactive
		foreach (var c in previewInstance.GetComponentsInChildren<Collider>()) c.enabled = false;
		foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>()) {
			// disable behaviours so they don't run on preview
			mb.enabled = false;
		}

		// apply preview material if provided
		if (previewMaterial != null) {
			foreach (var r in previewInstance.GetComponentsInChildren<Renderer>()) {
				var mats = new Material[r.sharedMaterials.Length];
				for (int i = 0; i < mats.Length; i++) mats[i] = previewMaterial;
				r.materials = mats;
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
}
