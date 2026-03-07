using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour {
	public int width = 10;
	public int height = 10;
	public float cellSize = 1f;
	public Vector3 origin = Vector3.zero;
	public bool showGizmos = true;
	[Header("Auto Placement")]
	[SerializeField] private PlaceableData autoChairData;

	// map from grid coordinate to occupying PlaceableObject
	private readonly Dictionary<Vector2Int, PlaceableObject> occupied = new();

	public static GridManager Instance { get; private set; }
	public PlaceableData AutoChairData => autoChairData;

	private void Awake() {
		if (Instance != null && Instance != this) {
			Debug.LogWarning("Multiple GridManager instances detected. Keeping the first one.");
			enabled = false;
			return;
		}
		Instance = this;
	}

	public Vector3 GetWorldPositionForCell(Vector2Int cell, Vector2Int size) {
		// place object centered on its occupied cells
		float x = (cell.x + size.x * 0.5f) * cellSize;
		float z = (cell.y + size.y * 0.5f) * cellSize;
		return origin + new Vector3(x, 0f, z);
	}

	/// <summary>
	/// Convert a world position to the base cell (bottom-left / origin cell) for an object with the given size.
	/// The returned cell is the same "baseCell" that is used when placing objects.
	/// </summary>
	public Vector2Int WorldToCell(Vector3 world, Vector2Int size) {
		Vector3 local = world - origin;
		float fx = local.x / cellSize - size.x * 0.5f;
		float fy = local.z / cellSize - size.y * 0.5f;
		int cx = Mathf.RoundToInt(fx);
		int cy = Mathf.RoundToInt(fy);
		return new Vector2Int(cx, cy);
	}

	public bool IsInsideGrid(Vector2Int cell) {
		return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
	}

	public bool CanPlace(PlaceableData data, Vector2Int at) {
		// forward to sized overload using data.size and pivot-converted base cell
		if (data == null) return false;
		Vector2Int baseCell = new(at.x - data.pivot.x, at.y - data.pivot.y);
		return CanPlace(data, baseCell, data.size);
	}

	private static Vector2Int GetRotatedSize(Vector2Int size, int rotationIndex) {
		return rotationIndex % 2 == 1 ? new Vector2Int(size.y, size.x) : size;
	}

	private static Quaternion GetRotationForIndex(int rotationIndex) {
		return Quaternion.Euler(0f, -90f + rotationIndex * 90f, 0f);
	}

	private static int GetRotationIndex(Quaternion rotation) {
		int index = Mathf.RoundToInt((rotation.eulerAngles.y + 90f) / 90f) % 4;
		if (index < 0) index += 4;
		return index;
	}

	private int[] GetChairAllowedRotationIndices(Vector2Int baseCell, Vector2Int size) {
		TableBehavior[] allTables = FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
		var indices = new List<int>();
		Vector3 worldPos = GetWorldPositionForCell(baseCell, size);
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

		return indices.ToArray();
	}

	public int[] GetValidChairRotationIndices(PlaceableData data, Vector2Int baseCell, Vector2Int size) {
		if (data == null || data.prefab == null) return new int[0];
		if (!data.prefab.TryGetComponent<ChairBehavior>(out _)) return new int[0];

		var allowedIndices = GetChairAllowedRotationIndices(baseCell, size);
		var validIndices = new List<int>();
		for (int i = 0; i < allowedIndices.Length; i++) {
			int rotationIndex = allowedIndices[i];
			Quaternion rotation = GetRotationForIndex(rotationIndex);
			if (CanPlaceInternal(data, baseCell, size, rotation, null))
				validIndices.Add(rotationIndex);
		}

		return validIndices.ToArray();
	}

	private bool CanPlaceInternal(PlaceableData data, Vector2Int baseCell, Vector2Int size, Quaternion rotation, PlaceableObject ignoredObject) {
		if (data == null || data.prefab == null) return false;

		for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++) {
				Vector2Int c = new(baseCell.x + x, baseCell.y + y);
				if (!IsInsideGrid(c)) return false;
				if (occupied.TryGetValue(c, out var occupiedObject) && occupiedObject != ignoredObject) return false;
			}

		if (!data.prefab.TryGetComponent<ChairBehavior>(out _)) return true;

		var chairAllowedIndices = GetChairAllowedRotationIndices(baseCell, size);
		if (chairAllowedIndices.Length == 0) return false;

		int rotationIndex = GetRotationIndex(rotation);
		for (int i = 0; i < chairAllowedIndices.Length; i++) {
			if (chairAllowedIndices[i] == rotationIndex) return true;
		}

		return false;
	}

	/// <summary>
	/// Check placement using an explicit base cell and size (size should already account for rotation).
	/// baseCell is the bottom-left / origin cell for the object.
	/// </summary>
	public bool CanPlace(PlaceableData data, Vector2Int baseCell, Vector2Int size) {
		return CanPlaceInternal(data, baseCell, size, Quaternion.Euler(0f, -90f, 0f), null);
	}

	/// <summary>
	/// Rotation-aware placement check. For chairs, ensure the given rotation faces one of the adjacent tables.
	/// </summary>
	public bool CanPlace(PlaceableData data, Vector2Int baseCell, Vector2Int size, Quaternion rotation) {
		return CanPlaceInternal(data, baseCell, size, rotation, null);
	}

	public PlaceableObject Place(PlaceableData data, Vector2Int at) {
		// keep old API for compatibility: convert to baseCell + default size/rotation
		if (data == null) {
			Debug.LogError("PlaceableData is null");
			return null;
		}
		Vector2Int baseCell = new(at.x - data.pivot.x, at.y - data.pivot.y);
		// default placement rotation starts at -90 degrees
		return Place(data, baseCell, data.size, Quaternion.Euler(0f, -90f, 0f));
	}

	/// <summary>
	/// Place an object using an explicit base cell and size (size should already account for rotation).
	/// Rotation is applied to the instantiated GameObject.
	/// </summary>
	public PlaceableObject Place(PlaceableData data, Vector2Int baseCell, Vector2Int size, Quaternion rotation) {
		if (data == null || data.prefab == null) {
			Debug.LogError("PlaceableData or its prefab is null");
			return null;
		}

		if (!CanPlace(data, baseCell, size, rotation)) return null;

		Vector3 worldPos = GetWorldPositionForCell(baseCell, size);
		GameObject go = Instantiate(data.prefab, worldPos, rotation, transform);
		if (!go.TryGetComponent<PlaceableObject>(out var po)) po = go.AddComponent<PlaceableObject>();
		po.Initialize(this, data, baseCell, size, rotation);

		// mark occupied
		foreach (var cell in po.OccupiedCells) {
			occupied[cell] = po;
		}

		if (go.TryGetComponent<TableBehavior>(out var table)) {
			TryAutoPlaceChairsAroundTable(po, table);
		}

		return po;
	}

	private void TryAutoPlaceChairsAroundTable(PlaceableObject tableObject, TableBehavior table) {
		if (tableObject == null || table == null || autoChairData == null || autoChairData.prefab == null)
			return;

		var seatOffsets = table.SeatCellOffsets;
		for (int i = 0; i < seatOffsets.Count; i++) {
			Vector2Int chairBaseCell = tableObject.OriginCell + seatOffsets[i];
			Vector2Int chairSize = autoChairData.size;
			Vector3 chairWorldPos = GetWorldPositionForCell(chairBaseCell, chairSize);
			Vector3 tablePos = table.Location != null ? table.Location.position : table.transform.position;
			Vector3 dir = tablePos - chairWorldPos;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.0001f)
				continue;

			float y = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
			float snapped = Mathf.Round(y / 90f) * 90f;
			int rotationIndex = ((int)Mathf.Round((snapped + 90f) / 90f)) % 4;
			Quaternion chairRotation = GetRotationForIndex(rotationIndex);

			if (!CanPlace(autoChairData, chairBaseCell, chairSize, chairRotation))
				continue;

			Place(autoChairData, chairBaseCell, chairSize, chairRotation);
		}
	}

	public bool TryGetAutoChairPlacement(Vector3 worldPos, out Vector2Int baseCell, out Vector2Int size, out Quaternion rotation) {
		baseCell = default;
		size = default;
		rotation = Quaternion.identity;

		if (autoChairData == null || autoChairData.prefab == null)
			return false;

		size = autoChairData.size;
		baseCell = WorldToCell(worldPos, size);
		var allowedIndices = GetValidChairRotationIndices(autoChairData, baseCell, size);
		if (allowedIndices == null || allowedIndices.Length == 0)
			return false;

		for (int i = 0; i < allowedIndices.Length; i++) {
			rotation = GetRotationForIndex(allowedIndices[i]);
			if (CanPlace(autoChairData, baseCell, size, rotation))
				return true;
		}

		return false;
	}

	public bool RotatePlacedObject(PlaceableObject obj) {
		if (obj == null || obj.Data == null) return false;

		int currentRotationIndex = GetRotationIndex(obj.transform.rotation);
		int nextRotationIndex = -1;

		if (obj.Data.prefab != null && obj.Data.prefab.TryGetComponent<ChairBehavior>(out _)) {
			int[] allowedIndices = GetChairAllowedRotationIndices(obj.OriginCell, obj.OccupiedCells.Count > 0 ? new Vector2Int(1, 1) : obj.Data.size);
			if (allowedIndices.Length <= 1) return false;

			int currentPos = System.Array.IndexOf(allowedIndices, currentRotationIndex);
			if (currentPos < 0) nextRotationIndex = allowedIndices[0];
			else nextRotationIndex = allowedIndices[(currentPos + 1) % allowedIndices.Length];
		} else {
			for (int offset = 1; offset <= 4; offset++) {
				int candidateIndex = (currentRotationIndex + offset) % 4;
				Vector2Int candidateSize = GetRotatedSize(obj.Data.size, candidateIndex);
				Quaternion candidateRotation = GetRotationForIndex(candidateIndex);
				if (CanPlaceInternal(obj.Data, obj.OriginCell, candidateSize, candidateRotation, obj)) {
					nextRotationIndex = candidateIndex;
					break;
				}
			}
			if (nextRotationIndex < 0) return false;
		}

		Vector2Int nextSize = GetRotatedSize(obj.Data.size, nextRotationIndex);
		Quaternion nextRotation = GetRotationForIndex(nextRotationIndex);
		if (!CanPlaceInternal(obj.Data, obj.OriginCell, nextSize, nextRotation, obj)) return false;

		for (int i = 0; i < obj.OccupiedCells.Count; i++) {
			var cell = obj.OccupiedCells[i];
			if (occupied.TryGetValue(cell, out var occupiedObject) && occupiedObject == obj)
				occupied.Remove(cell);
		}

		obj.UpdatePlacement(obj.OriginCell, nextSize, nextRotation);

		for (int i = 0; i < obj.OccupiedCells.Count; i++) {
			occupied[obj.OccupiedCells[i]] = obj;
		}

		return true;
	}

	public void Remove(PlaceableObject obj) {
		if (obj == null) return;

		// if removing a table, also remove any chairs attached to it
		if (obj.TryGetComponent<TableBehavior>(out var table)) {
			var toRemove = new System.Collections.Generic.List<PlaceableObject>();
			var chairs = ChairBehavior.AllChairs;
			for (int i = 0; i < chairs.Count; i++) {
				var chair = chairs[i];
				if (chair == null) continue;
				if (chair.Table == table) {
					var cpo = chair.GetComponentInParent<PlaceableObject>();
					if (cpo != null && cpo != obj) toRemove.Add(cpo);
				}
			}
			for (int i = 0; i < toRemove.Count; i++) {
				Remove(toRemove[i]);
			}
		}

		foreach (var cell in obj.OccupiedCells) {
			if (occupied.ContainsKey(cell) && occupied[cell] == obj)
				occupied.Remove(cell);
		}
		Destroy(obj.gameObject);
	}

	public PlaceableObject GetObjectAt(Vector2Int cell) {
		occupied.TryGetValue(cell, out var obj);
		return obj;
	}

	private void OnDrawGizmos() {
		if (!showGizmos) return;
		Gizmos.color = Color.gray;
		for (int x = 0; x <= width; x++) {
			Vector3 start = origin + new Vector3(x * cellSize, 0, 0);
			Vector3 end = origin + new Vector3(x * cellSize, 0, height * cellSize);
			Gizmos.DrawLine(start, end);
		}
		for (int y = 0; y <= height; y++) {
			Vector3 start = origin + new Vector3(0, 0, y * cellSize);
			Vector3 end = origin + new Vector3(width * cellSize, 0, y * cellSize);
			Gizmos.DrawLine(start, end);
		}

		// draw occupied cells
		Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.5f);
		if (occupied != null) {
			foreach (var kv in occupied) {
				Vector2Int c = kv.Key;
				Vector3 center = origin + new Vector3((c.x + 0.5f) * cellSize, 0, (c.y + 0.5f) * cellSize);
				Gizmos.DrawCube(center, new Vector3(cellSize, 0.01f, cellSize));
			}
		}
	}
}
