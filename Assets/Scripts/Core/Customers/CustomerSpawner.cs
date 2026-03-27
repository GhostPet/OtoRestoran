using UnityEngine;
using UnityEngine.UI;

public class CustomerSpawner : MonoBehaviour {
	[Header("Prefabs")]
	[SerializeField] private Customer[] customerPrefabs;
	public Customer[] CustomerPrefabs => customerPrefabs;

	[Header("Spawning")]
	[SerializeField] private bool spawningEnabled = true;
	[SerializeField] private float spawnInterval = 3f;
	[SerializeField] private Transform customerSpawnPoint;
	[SerializeField] private bool restaurantOpen = true;

	[Header("UI")]
	[SerializeField] private Button toggleButton;

	[Header("Spawn Parenting")]
	[SerializeField] private Transform spawnParent; // optional parent transform for spawned customers

	/// <summary>
	/// Public read-only access to whether spawning is enabled.
	/// </summary>
	public bool SpawningEnabled => spawningEnabled;
	public bool RestaurantOpen => restaurantOpen;
	private float timer;
	private readonly System.Collections.Generic.List<Customer> spawnedCustomers = new System.Collections.Generic.List<Customer>();

	private void Update() {
		if (customerPrefabs == null || customerPrefabs.Length == 0)
			return;

		if (!spawningEnabled)
			return;

		if (!restaurantOpen)
			return;

		timer -= Time.deltaTime;

		if (timer <= 0f) {
			TrySpawnCustomer();
			timer = spawnInterval;
		}
	}

	private void OnEnable() {
		// Wire button listener if a button has been assigned in inspector
		if (toggleButton != null)
			toggleButton.onClick.AddListener(ToggleSpawning);
	}

	private void OnDisable() {
		if (toggleButton != null)
			toggleButton.onClick.RemoveListener(ToggleSpawning);
	}

	public void SetSpawnParent(Transform parent) {
		spawnParent = parent;
	}

	/// <summary>
	/// Toggle spawning on/off. Can be wired to a UI Button OnClick.
	/// </summary>
	public void ToggleSpawning() {
		spawningEnabled = !spawningEnabled;
	}

	/// <summary>
	/// Explicitly enable or disable spawning (useful for code control).
	/// </summary>
	public void SetSpawning(bool enabled) {
		spawningEnabled = enabled;
	}

	public void SetRestaurantOpen(bool open) {
		restaurantOpen = open;
		if (restaurantOpen)
			timer = 0f;
		else
			ClearSpawnedCustomers();
	}

	public void ClearSpawnedCustomers() {
		CleanupDestroyedCustomers();

		for (int i = spawnedCustomers.Count - 1; i >= 0; i--) {
			Customer customer = spawnedCustomers[i];
			if (customer != null) {
				customer.DespawnImmediately();
			}
		}

		spawnedCustomers.Clear();
	}

	private void TrySpawnCustomer() {
		var freeChair = ChairBehavior.FindAnyFreeChair();
		if (freeChair == null) {
			//Debug.Log("[Spawner] No free seat. Spawn skipped.");
			return;
		}

		Vector3 spawnPosition = customerSpawnPoint != null ? customerSpawnPoint.position : transform.position;
		Quaternion spawnRotation = customerSpawnPoint != null ? customerSpawnPoint.rotation : transform.rotation;

		// Choose a random prefab from the available customer prefabs
		var selectedPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Length)];
		if (selectedPrefab == null) {
			Debug.LogWarning("[Spawner] Selected customer prefab is null. Spawn skipped.");
			return;
		}

		GameObject customerGO;
		if (spawnParent != null) {
			customerGO = Instantiate(selectedPrefab.gameObject, spawnPosition, spawnRotation, spawnParent);
		} else {
			customerGO = Instantiate(selectedPrefab.gameObject, spawnPosition, spawnRotation);
		}
		if (!customerGO.TryGetComponent<Customer>(out var customer)) {
			Debug.LogWarning("[Spawner] Spawned object does not contain a Customer component.");
			Destroy(customerGO);
			return;
		}

		spawnedCustomers.Add(customer);

		customer.SetSpawnPoint(customerSpawnPoint);

		// Parent assignment (in case prefab was instantiated without parent)
		if (spawnParent != null && customerGO.transform.parent != spawnParent) {
			customerGO.transform.SetParent(spawnParent, true);
		}

		freeChair.Assign(customer);
		customer.SetSeat(freeChair);

		string tableName = "None";
		if (freeChair.Table != null)
			tableName = freeChair.Table.name;

		Debug.Log($"[Spawner] Customer spawned and seated at {freeChair.name} (Table: {tableName})");
	}

	private void CleanupDestroyedCustomers() {
		for (int i = spawnedCustomers.Count - 1; i >= 0; i--) {
			if (spawnedCustomers[i] == null) {
				spawnedCustomers.RemoveAt(i);
			}
		}
	}
}
