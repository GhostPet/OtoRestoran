using System.Collections.Generic;
using UnityEngine;

public class TableBehavior : BaseRestaurantObject, IPlaceableLifecycle {
	[SerializeField] private Transform[] servePoints;
	[SerializeField]
	private List<Vector2Int> seatCellOffsets = new() {
		new Vector2Int(-1, 0),
		new Vector2Int(1, 0),
		new Vector2Int(0, -1),
		new Vector2Int(0, 1)
	};

	private Vector2Int originCell = new(-999, -999);
	private readonly List<Customer> customers = new();
	private bool dirty;

	public IReadOnlyList<Transform> ServePoints => servePoints;
	public Transform Location => transform;
	public IReadOnlyList<Vector2Int> SeatCellOffsets => seatCellOffsets;
	public IReadOnlyList<Customer> Customers {
		get {
			RefreshCustomersFromAttachedChairs();
			return customers;
		}
	}

	public void OnPlaced(PlaceableObject placedObject) {
		if (placedObject != null) originCell = placedObject.OriginCell;
	}

	public void OnRemoved(PlaceableObject placedObject) {
		originCell = new Vector2Int(-999, -999);
	}

	public bool IsSeatCell(Vector2Int cell) {
		if (originCell.x == -999) return false;
		for (int i = 0; i < seatCellOffsets.Count; i++) {
			if (originCell + seatCellOffsets[i] == cell) return true;
		}
		return false;
	}

	public Transform GetNearestServePoint(Vector3 fromWorldPos) {
		if (servePoints == null || servePoints.Length == 0) return transform;

		Transform best = null;
		float bestDist = float.MaxValue;
		foreach (var p in servePoints) {
			if (p == null) continue;
			float d = (p.position - fromWorldPos).sqrMagnitude;
			if (d < bestDist) { bestDist = d; best = p; }
		}

		return best != null ? best : transform;
	}

	private void RefreshCustomersFromAttachedChairs() {
		customers.Clear();

		var allChairs = ChairBehavior.AllChairs;
		for (int i = 0; i < allChairs.Count; i++) {
			var chair = allChairs[i];
			if (chair == null) continue;
			if (chair.Table != this) continue;

			var customer = chair.CurrentCustomer;
			if (customer == null) continue;
			if (!customers.Contains(customer)) customers.Add(customer);
		}
	}

	// Minimal customer/dirty helpers (kept small to avoid coupling)
	public void RegisterCustomer(Customer customer) {
		if (customer == null) return;
		if (!customers.Contains(customer)) customers.Add(customer);
	}

	public void UnregisterCustomer(Customer customer) {
		if (customer == null) return;
		customers.Remove(customer);
	}

	public void SetDirty(bool value) {
		dirty = value;
	}

	public bool HasCustomer() => customers.Count > 0;
	public bool IsEmpty() => customers.Count == 0;
}
