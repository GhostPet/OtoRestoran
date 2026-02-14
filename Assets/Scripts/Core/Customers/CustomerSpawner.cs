using UnityEngine;

public class CustomerSpawner : MonoBehaviour {
	public Customer customerPrefab;

	[SerializeField] private float spawnInterval = 3f;
	private float timer;

	private void Update() {
		if (customerPrefab == null)
			return;

		timer -= Time.deltaTime;

		if (timer <= 0f) {
			TrySpawnCustomer();
			timer = spawnInterval;
		}
	}

	private void TrySpawnCustomer() {
		var freeSeat = Seat.FindAnyFreeSeat();
		if (freeSeat == null) {
			Debug.Log("[Spawner] No free seat. Spawn skipped.");
			return;
		}

		var customerGO = Instantiate(customerPrefab.gameObject);
		var customer = customerGO.GetComponent<Customer>();

		freeSeat.Assign(customer);
		customer.SetSeat(freeSeat);

		Debug.Log($"[Spawner] Customer spawned and seated at {freeSeat.name} (Table: {freeSeat.Table?.name})");
	}
}
