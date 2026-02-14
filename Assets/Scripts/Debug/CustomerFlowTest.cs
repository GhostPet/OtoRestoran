using UnityEngine;
using System.Collections;

public class CustomerFlowTest : MonoBehaviour {
	[Header("Scene References")]
	[SerializeField] private Table table;                 // Sahnedeki Table
	[SerializeField] private CustomerSpawner spawner;     // Sahnedeki Spawner
	[SerializeField] private Seat[] seats;                // Sahnedeki Seat'ler (table.seats ile aynı sıra)

	private void Start() {
		StartCoroutine(RunTest());
	}

	private IEnumerator RunTest() {
		Debug.Log("=== CUSTOMER FLOW SCENE TEST START ===");

		if (table == null || spawner == null || seats == null || seats.Length == 0) {
			Debug.LogError("❌ Referanslar eksik. Table / Spawner / Seats bağla.");
			yield break;
		}

		// Başlangıç: tüm seat'ler boş mu?
		foreach (var s in seats) {
			if (s == null) {
				Debug.LogError("❌ Seats dizisinde null var.");
				yield break;
			}

			if (!s.IsEmpty()) {
				Debug.LogError($"❌ Test başında {s.name} dolu. Teste boş başlamak lazım.");
				yield break;
			}
		}

		Debug.Log("✔ All seats empty at start.");

		// Spawn
		Debug.Log("Spawning customer...");
		spawner.SendMessage("TrySpawnCustomer", SendMessageOptions.DontRequireReceiver);
		yield return null;

		// En az bir seat dolmalı
		Seat occupiedSeat = null;
		foreach (var s in seats) {
			if (s.IsOccupied()) {
				occupiedSeat = s;
				break;
			}
		}

		if (occupiedSeat == null) {
			Debug.LogError("❌ Spawn sonrası hiçbir seat dolmadı.");
			yield break;
		}

		Debug.Log($"✔ Customer spawned on seat: {occupiedSeat.name}");

		var customer = occupiedSeat.CurrentCustomer;
		if (customer == null) {
			Debug.LogError("❌ Seat dolu ama CurrentCustomer null.");
			yield break;
		}

		// Seating -> Thinking geçişi için koltuğa ulaştıralım
		customer.transform.position = occupiedSeat.transform.position;
		yield return null;

		if (customer.State != CustomerState.Thinking) {
			Debug.LogError($"❌ Customer state {customer.State}, Thinking bekleniyordu.");
			yield break;
		}

		Debug.Log("✔ Customer entered Thinking state.");
		Debug.Log("=== CUSTOMER FLOW SCENE TEST PASSED ===");
	}
}
