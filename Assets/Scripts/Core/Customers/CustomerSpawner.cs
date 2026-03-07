using UnityEngine;
using UnityEngine.UI;

public class CustomerSpawner : MonoBehaviour {
	public Customer customerPrefab;

	[Header("Spawning")]
	[SerializeField] private bool spawningEnabled = true;
	[SerializeField] private float spawnInterval = 3f;

	[Header("UI")]
	[SerializeField] private Button toggleButton;

	/// <summary>
	/// Public read-only access to whether spawning is enabled.
	/// </summary>
	public bool SpawningEnabled => spawningEnabled;
	private float timer;

	private void Update() {
		if (customerPrefab == null)
			return;

		if (!spawningEnabled)
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
