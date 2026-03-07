using UnityEngine;

public class SingleCustomerSpawner : MonoBehaviour {
	public Customer customerPrefab;

	[SerializeField] private bool spawnOnStart = true;
	[SerializeField] private KeyCode spawnKey = KeyCode.S;

	private bool hasSpawned = false;

	private void Start() {
		if (spawnOnStart)
			SpawnOnce();
	}

	private void Update() {
		if (hasSpawned)
			return;

		if (Input.GetKeyDown(spawnKey))
			SpawnOnce();
	}

	public void SpawnOnce() {
		if (hasSpawned) {
			Debug.Log("[SingleSpawner] Already spawned a customer. Spawn skipped.");
			return;
		}

		if (customerPrefab == null) {
			Debug.Log("[SingleSpawner] customerPrefab is null. Assign a prefab in the inspector.");
			return;
		}

		var freeChair = ChairBehavior.FindAnyFreeChair();
		if (freeChair == null) {
			Debug.Log("[SingleSpawner] No free seat. Spawn skipped.");
			return;
		}

		var customerGO = Instantiate(customerPrefab.gameObject);
		var customer = customerGO.GetComponent<Customer>();

		freeChair.Assign(customer);
		customer.SetSeat(freeChair);

		string tableName = "None";
		if (freeChair.Table != null)
			tableName = freeChair.Table.name;

		Debug.Log($"[SingleSpawner] Customer spawned and seated at {freeChair.name} (Table: {tableName})");

		hasSpawned = true;
	}
}
