using System.Collections.Generic;
using UnityEngine;

public class TableLogic : MonoBehaviour {
	[SerializeField] private Transform[] seatPoints;
	[SerializeField] private Transform[] servePoints;
	[SerializeField] private Seat[] seats; // DSL için tutuluyor

	private bool dirty;
	private readonly List<Customer> customers = new List<Customer>();

	public Transform Location => transform;
	public IReadOnlyList<Customer> Customers => customers;
	public IReadOnlyList<Seat> Seats => seats; // DSL için okunabilir expose

	private void Awake() {
		BindSeatsToNearestPoints();
		RebuildCustomerCache();
	}

	private void BindSeatsToNearestPoints() {
		if (seats == null || seatPoints == null)
			return;

		var usedPoints = new HashSet<Transform>();

		foreach (var seat in seats) {
			if (seat == null)
				continue;

			Transform bestPoint = null;
			float bestDist = float.MaxValue;

			foreach (var p in seatPoints) {
				if (p == null || usedPoints.Contains(p))
					continue;

				float d = (p.position - seat.transform.position).sqrMagnitude;
				if (d < bestDist) {
					bestDist = d;
					bestPoint = p;
				}
			}

			if (bestPoint != null) {
				seat.transform.position = bestPoint.position;
				seat.transform.rotation = bestPoint.rotation;
				usedPoints.Add(bestPoint);
			}

			seat.SetTable(this);
		}
	}

	private void RebuildCustomerCache() {
		customers.Clear();

		if (seats == null)
			return;

		foreach (var seat in seats) {
			if (seat == null) continue;

			if (seat.CurrentCustomer != null)
				customers.Add(seat.CurrentCustomer);
		}
	}

	public bool IsDirty() => dirty;
	public void SetDirty(bool value) => dirty = value;

	public bool IsEmpty() => customers.Count == 0;
	public bool HasCustomer() => customers.Count > 0;

	public void RegisterCustomer(Customer customer) {
		if (!customers.Contains(customer))
			customers.Add(customer);
	}

	public void UnregisterCustomer(Customer customer) {
		customers.Remove(customer);
	}

	public Transform GetNearestServePoint(Vector3 fromWorldPos) {
		if (servePoints == null || servePoints.Length == 0)
			return transform;

		Transform best = null;
		float bestDist = float.MaxValue;

		foreach (var p in servePoints) {
			if (p == null) continue;

			float d = (p.position - fromWorldPos).sqrMagnitude;
			if (d < bestDist) {
				bestDist = d;
				best = p;
			}
		}

		return best != null ? best : transform;
	}
}
