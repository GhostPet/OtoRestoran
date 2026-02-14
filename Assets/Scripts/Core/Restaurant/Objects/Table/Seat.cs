using System.Collections.Generic;
using UnityEngine;

public class Seat : MonoBehaviour {
	private static readonly List<Seat> allSeats = new List<Seat>();
	public static IReadOnlyList<Seat> AllSeats => allSeats;

	private TableLogic table;
	private Customer currentCustomer;

	public TableLogic Table => table;
	public Customer CurrentCustomer => currentCustomer;

	private void OnEnable() {
		if (!allSeats.Contains(this))
			allSeats.Add(this);
	}

	private void OnDisable() {
		allSeats.Remove(this);
	}

	public bool IsOccupied() => currentCustomer != null;
	public bool IsEmpty() => currentCustomer == null;

	public void SetTable(TableLogic table) {
		this.table = table;
	}

	public void Assign(Customer customer) {
		if (currentCustomer != null) {
			Debug.LogWarning($"{name}: Seat zaten dolu.");
			return;
		}

		currentCustomer = customer;
		table?.RegisterCustomer(customer);
	}

	public void Clear() {
		if (currentCustomer != null) {
			table?.UnregisterCustomer(currentCustomer);
			currentCustomer = null;
		}
	}

	public static Seat FindAnyFreeSeat() {
		foreach (var seat in allSeats) {
			if (seat != null && seat.IsEmpty())
				return seat;
		}
		return null;
	}
}
