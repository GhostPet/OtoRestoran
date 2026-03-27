using System.Collections.Generic;
using UnityEngine;

public class ChairBehavior : BaseRestaurantObject, IPlaceableLifecycle {
	private static readonly List<ChairBehavior> allChairs = new();
	public static IReadOnlyList<ChairBehavior> AllChairs => allChairs;

	private TableBehavior attachedTable;
	private Vector2Int attachedCell;
	private Customer currentCustomer;

	public TableBehavior Table => attachedTable;
	public Customer CurrentCustomer => currentCustomer;

	public void OnPlaced(PlaceableObject placedObject) {
		Vector2Int chairCell = placedObject.OriginCell;
		TableBehavior[] allTables = FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
		var previousTable = attachedTable;

		var candidates = new List<TableBehavior>();
		for (int i = 0; i < allTables.Length; i++) {
			var table = allTables[i];
			if (table == null) continue;
			if (table.IsSeatCell(chairCell)) candidates.Add(table);
		}

		if (candidates.Count == 0) {
			ApplyTableBinding(previousTable, null, default);
			return;
		}

		if (candidates.Count == 1) {
			ApplyTableBinding(previousTable, candidates[0], chairCell);
			return;
		}

		// Birden fazla aday varsa, sandalyenin baktığı yöne göre en uygun masayı seç.
		Vector3 chairPos = placedObject.transform.position;
		Vector3 forward = placedObject.transform.forward;
		forward.y = 0f;
		if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
		forward.Normalize();

		float bestAngle = float.MaxValue;
		TableBehavior bestTable = null;
		for (int i = 0; i < candidates.Count; i++) {
			var table = candidates[i];
			if (table == null) continue;
			Vector3 tpos = table.Location != null ? table.Location.position : table.transform.position;
			Vector3 dir = tpos - chairPos;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.0001f) continue;
			float angle = Vector3.Angle(forward, dir.normalized);
			if (angle < bestAngle) { bestAngle = angle; bestTable = table; }
		}

		if (bestTable != null) {
			ApplyTableBinding(previousTable, bestTable, chairCell);
			return;
		}

		Debug.LogWarning($"{name}: Sandalye birden fazla masaya bitişik; uygun olarak bağlanamadı. cell={chairCell}");

	}

	private void ApplyTableBinding(TableBehavior previousTable, TableBehavior newTable, Vector2Int chairCell) {
		if (previousTable != null && previousTable != newTable && currentCustomer != null)
			previousTable.UnregisterCustomer(currentCustomer);

		attachedTable = newTable;
		attachedCell = newTable != null ? chairCell : default;

		if (newTable != null && newTable != previousTable && currentCustomer != null)
			newTable.RegisterCustomer(currentCustomer);
	}

	public void OnRemoved(PlaceableObject placedObject) {
		DetachFromTable();
	}

	public void AttachToTable(TableBehavior table, Vector2Int cell) {
		attachedTable = table;
		attachedCell = cell;
	}

	public void DetachFromTable() {
		Clear();

		attachedTable = null;
		attachedCell = default;
	}

	private void OnEnable() {
		if (!allChairs.Contains(this))
			allChairs.Add(this);

	}

	private void OnDisable() {
		allChairs.Remove(this);
	}

	public bool IsOccupied() {
		return currentCustomer != null;
	}

	public bool IsEmpty() {
		return currentCustomer == null;
	}

	public void SetTable(TableBehavior table) {
		attachedTable = table;
	}

	public void Assign(Customer customer) {
		if (currentCustomer != null) {
			Debug.LogWarning($"{name}: Seat zaten dolu.");
			return;
		}

		currentCustomer = customer;
		if (attachedTable != null)
			attachedTable.RegisterCustomer(customer);
	}

	public void Clear() {
		if (currentCustomer != null) {
			if (attachedTable != null)
				attachedTable.UnregisterCustomer(currentCustomer);
			currentCustomer = null;
		}
	}

	public static ChairBehavior FindAnyFreeChair() {
		for (int i = 0; i < allChairs.Count; i++) {
			var chair = allChairs[i];
			if (chair != null && chair.IsEmpty())
				return chair;
		}
		return null;
	}
}
