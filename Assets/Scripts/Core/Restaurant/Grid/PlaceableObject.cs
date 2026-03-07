using System.Collections.Generic;
using UnityEngine;

public class PlaceableObject : MonoBehaviour {
	public PlaceableData Data { get; private set; }
	public Vector2Int OriginCell { get; private set; }
	public List<Vector2Int> OccupiedCells { get; private set; } = new List<Vector2Int>();

	private GridManager grid;

	public void Initialize(GridManager gridManager, PlaceableData data, Vector2Int originCell) {
		// legacy overload: forward to new overload using data.size and default rotation (-90deg)
		Initialize(gridManager, data, originCell, data.size, Quaternion.Euler(0f, -90f, 0f));
	}

	public void Initialize(GridManager gridManager, PlaceableData data, Vector2Int originCell, Vector2Int size, Quaternion rotation) {
		grid = gridManager;
		Data = data;
		OriginCell = originCell;
		OccupiedCells.Clear();
		for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++) {
				var cell = new Vector2Int(originCell.x + x, originCell.y + y);
				OccupiedCells.Add(cell);
			}

		// align transform exactly
		transform.SetPositionAndRotation(grid.GetWorldPositionForCell(originCell, size), rotation);

		// notify other components on the placed object
		foreach (var comp in GetComponents<MonoBehaviour>()) {
			(comp as IPlaceableLifecycle)?.OnPlaced(this);
		}
	}

	private void OnDestroy() {
		foreach (var comp in GetComponents<MonoBehaviour>()) {
			(comp as IPlaceableLifecycle)?.OnRemoved(this);
		}
	}
}

public interface IPlaceableLifecycle {
	void OnPlaced(PlaceableObject placedObject);
	void OnRemoved(PlaceableObject placedObject);
}
