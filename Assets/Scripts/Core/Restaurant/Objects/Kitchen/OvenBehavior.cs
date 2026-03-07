using UnityEngine;

public class OvenBehavior : BaseRestaurantObject, IPlaceableLifecycle {
	public enum OvenState {
		Empty,
		Placed,
		Cooking,
		Ready,
		BurnedDirty
	}

	[SerializeField] private float cookTime = 5f;
	[SerializeField] private float burnAfterReadyTime = 4f;

	private float timer = 0f;
	private OvenState state = OvenState.Empty;

	public OvenState State => state;

	public void StartCooking() {
		Cook();
	}

	public void StopCooking() {
		if (state == OvenState.Cooking) {
			state = OvenState.Placed;
			timer = 0f;
		}
	}

	public void Place() {
		if (state != OvenState.Empty) {
			Debug.Log($"[Oven] {name}: Ocağa yeni eşya koyulamaz. Durum={state}");
			return;
		}

		state = OvenState.Placed;
		timer = 0f;
		Debug.Log("[Oven] Eşya ocağa yerleştirildi.");
	}

	public void Cook() {
		if (state == OvenState.Placed) {
			state = OvenState.Cooking;
			timer = 0f;
			Debug.Log("[Oven] hazırlanıyor");
			return;
		}

		if (state == OvenState.Cooking) {
			Debug.Log("[Oven] Zaten hazırlanıyor.");
			return;
		}

		if (state == OvenState.Ready) {
			Debug.Log("[Oven] Zaten hazırlandı.");
			return;
		}

		if (state == OvenState.BurnedDirty) {
			Debug.Log("[Oven] yandı. Önce oven.clean() çağır.");
			return;
		}

		Debug.Log("[Oven] Önce oven.place() ile eşyayı koy.");
	}

	public bool Take() {
		if (state == OvenState.Ready) {
			state = OvenState.Empty;
			timer = 0f;
			Debug.Log("[Oven] Pişmiş yemek alındı.");
			return true;
		}

		if (state == OvenState.BurnedDirty) {
			Debug.Log("[Oven] yandı. Alınacak yemek yok. Önce oven.clean() çağır.");
			return false;
		}

		if (state == OvenState.Cooking || state == OvenState.Placed) {
			Debug.Log("[Oven] Yemek henüz hazır değil.");
			return false;
		}

		Debug.Log("[Oven] Alınacak bir şey yok.");
		return false;
	}

	public void Clean() {
		if (state != OvenState.BurnedDirty) {
			Debug.Log("[Oven] Temizlenecek bir kir yok.");
			return;
		}

		state = OvenState.Empty;
		timer = 0f;
		Debug.Log("[Oven] Ocak temizlendi.");
	}

	private void Update() {
		TickOven(Time.deltaTime);
	}

	public void TickOven(float deltaTime) {
		if (state == OvenState.Cooking) {
			timer += deltaTime;
			if (timer >= cookTime) {
				state = OvenState.Ready;
				timer = 0f;
				Debug.Log("[Oven] hazırlandı");
			}
			return;
		}

		if (state == OvenState.Ready) {
			timer += deltaTime;
			if (timer >= burnAfterReadyTime) {
				state = OvenState.BurnedDirty;
				timer = 0f;
				Debug.Log("[Oven] yandı");
			}
		}
	}

	public void OnPlaced(PlaceableObject placedObject) {
		// called when object is placed on the grid
		// can be used to initialize state
	}

	public void OnRemoved(PlaceableObject placedObject) {
		// cleanup
	}
}
