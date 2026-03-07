using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour {
	public int width = 10;
	public int height = 10;
	public float cellSize = 1f;
	public Vector3 origin = Vector3.zero;
	public bool showGizmos = true;

	// map from grid coordinate to occupying PlaceableObject
	private readonly Dictionary<Vector2Int, PlaceableObject> occupied = new();

	public static GridManager Instance { get; private set; }

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

	/// <summary>
	/// Check placement using an explicit base cell and size (size should already account for rotation).
	/// baseCell is the bottom-left / origin cell for the object.
	/// </summary>
	public bool CanPlace(PlaceableData data, Vector2Int baseCell, Vector2Int size) {
		if (data == null || data.prefab == null)
			return false;

		for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++) {
				Vector2Int c = new(baseCell.x + x, baseCell.y + y);
				if (!IsInsideGrid(c)) return false;
				if (occupied.ContainsKey(c)) return false;
			}

		// special-case: chairs must be adjacent to exactly one table seat cell
		if (data.prefab.TryGetComponent<ChairBehavior>(out _)) {
			Vector2Int chairCell = baseCell;
			TableBehavior[] allTables = FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
			int matchCount = 0;
			for (int i = 0; i < allTables.Length; i++) {
				var table = allTables[i];
				if (table != null && table.IsSeatCell(chairCell))
					matchCount++;
			}

			// allow placement if at least one table adjacent; final facing check is done in rotation-aware overload
			return matchCount > 0;
		}

		return true;
	}

	/// <summary>
	/// Rotation-aware placement check. For chairs, ensure the given rotation faces one of the adjacent tables.
	/// </summary>
	public bool CanPlace(PlaceableData data, Vector2Int baseCell, Vector2Int size, Quaternion rotation) {
		if (data == null || data.prefab == null) return false;

		// basic bounds/occupancy check
		for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++) {
				Vector2Int c = new(baseCell.x + x, baseCell.y + y);
				if (!IsInsideGrid(c)) return false;
				if (occupied.ContainsKey(c)) return false;
			}

		// if not a chair, rotation doesn't affect placement
		if (!data.prefab.TryGetComponent<ChairBehavior>(out _)) return true;

		// gather adjacent tables for this seat cell
		TableBehavior[] allTables = FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
		System.Collections.Generic.List<float> tableAngles = new();
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
			if (!tableAngles.Contains(snapped)) tableAngles.Add(snapped);
		}

		if (tableAngles.Count == 0) return false;

		// compute rotation's snapped angle
		float rotY = rotation.eulerAngles.y;
		float rotSnapped = Mathf.Round(rotY / 90f) * 90f;

		// valid if rotation matches any adjacent table's snapped angle
		for (int i = 0; i < tableAngles.Count; i++) {
			if (Mathf.Abs(Mathf.DeltaAngle(rotSnapped, tableAngles[i])) < 0.1f) return true;
		}

		return false;
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

		if (!CanPlace(data, baseCell, size)) return null;

		Vector3 worldPos = GetWorldPositionForCell(baseCell, size);
		GameObject go = Instantiate(data.prefab, worldPos, rotation, transform);
		if (!go.TryGetComponent<PlaceableObject>(out var po)) po = go.AddComponent<PlaceableObject>();
		po.Initialize(this, data, baseCell, size, rotation);

		// mark occupied
		foreach (var cell in po.OccupiedCells) {
			occupied[cell] = po;
		}

		return po;
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
