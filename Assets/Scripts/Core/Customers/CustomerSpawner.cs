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
		var freeChair = ChairBehavior.FindAnyFreeChair();
		if (freeChair == null) {
			Debug.Log("[Spawner] No free seat. Spawn skipped.");
			return;
		}

		var customerGO = Instantiate(customerPrefab.gameObject);
		var customer = customerGO.GetComponent<Customer>();

		freeChair.Assign(customer);
		customer.SetSeat(freeChair);

		string tableName = "None";
		if (freeChair.Table != null)
			tableName = freeChair.Table.name;

		Debug.Log($"[Spawner] Customer spawned and seated at {freeChair.name} (Table: {tableName})");
	}
}
