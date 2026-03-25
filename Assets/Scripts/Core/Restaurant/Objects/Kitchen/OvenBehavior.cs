using System.Collections.Generic;
using UnityEngine;

public class OvenBehavior : BaseRestaurantObject, IPlaceableLifecycle {
	public enum OvenState {
		Empty,
		Placed,
		Cooking,
		Ready,
		BurnedDirty
	}

	[SerializeField] private List<FurnaceRecipeSO> recipes = new List<FurnaceRecipeSO>();

	private float timer = 0f;
	private OvenState state = OvenState.Empty;
	private FurnaceRecipeSO activeRecipe;
	private bool burnedOutputAvailable;

	public OvenState State => state;

	public FurnaceRecipeSO ActiveRecipe => activeRecipe;

	public void StartCooking() {
		Cook();
	}

	public void StopCooking() {
		if (state == OvenState.Cooking) {
			state = OvenState.Placed;
			timer = 0f;
		}
	}

	public bool Place(RobotInventory inventory) {
		if (state != OvenState.Empty) {
			Debug.Log($"[Oven] {name}: Ocağa yeni eşya koyulamaz. Durum={state}");
			return false;
		}

		if (inventory == null) {
			Debug.Log("[Oven] Yerleştirme için robot envanteri gerekli.");
			return false;
		}

		FurnaceRecipeSO matchingRecipe;
		if (!TryFindMatchingRecipe(inventory, out matchingRecipe)) {
			Debug.Log("[Oven] Robot envanterinde bu fırın için uygun tarif malzemesi yok.");
			return false;
		}

		if (!inventory.TryConsumeIngredients(matchingRecipe.InputItems)) {
			Debug.Log("[Oven] Tarif malzemeleri robot envanterinden alınamadı.");
			return false;
		}

		state = OvenState.Placed;
		timer = 0f;
		activeRecipe = matchingRecipe;
		burnedOutputAvailable = false;
		Debug.Log($"[Oven] {GetRecipeName(matchingRecipe)} için malzemeler ocağa yerleştirildi.");
		return true;
	}

	public bool Place(RobotInventory inventory, IReadOnlyList<ItemSO> items) {
		if (state != OvenState.Empty) {
			Debug.Log($"[Oven] {name}: Ocağa yeni eşya koyulamaz. Durum={state}");
			return false;
		}

		if (inventory == null) {
			Debug.Log("[Oven] Yerleştirme için robot envanteri gerekli.");
			return false;
		}

		FurnaceRecipeSO matchingRecipe;
		if (!TryFindMatchingRecipe(items, out matchingRecipe)) {
			Debug.Log("[Oven] Verilen itemler için uygun fırın tarifi bulunamadı.");
			return false;
		}

		if (!inventory.TryConsumeIngredients(matchingRecipe.InputItems)) {
			Debug.Log("[Oven] Tarif malzemeleri robot envanterinden alınamadı.");
			return false;
		}

		state = OvenState.Placed;
		timer = 0f;
		activeRecipe = matchingRecipe;
		burnedOutputAvailable = false;
		Debug.Log($"[Oven] {GetRecipeName(matchingRecipe)} için seçilen malzemeler ocağa yerleştirildi.");
		return true;
	}

	public void Place() {
		Debug.Log("[Oven] Eşya yerleştirmek için robot envanteri bağlamı gerekli.");
	}

	public void Cook() {
		if (activeRecipe == null) {
			Debug.Log("[Oven] Önce uygun tarif malzemelerini yerleştir.");
			return;
		}

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

	public bool Take(RobotInventory inventory) {
		if (state == OvenState.Ready) {
			if (inventory == null) {
				Debug.Log("[Oven] Pişmiş ürünü almak için robot envanteri gerekli.");
				return false;
			}

			if (activeRecipe == null || !inventory.TryAddRecipeOutputs(activeRecipe.OutputItems)) {
				Debug.Log("[Oven] Pişmiş çıktı robot envanterine eklenemedi.");
				return false;
			}

			Debug.Log($"[Oven] {GetRecipeName(activeRecipe)} çıktısı alındı.");
			ResetToEmpty();
			return true;
		}

		if (state == OvenState.BurnedDirty) {
			if (!burnedOutputAvailable) {
				Debug.Log("[Oven] Yanan üründen alınacak çıktı yok. Önce oven.clean() çağır.");
				return false;
			}

			if (inventory == null) {
				Debug.Log("[Oven] Yanmış çıktıyı almak için robot envanteri gerekli.");
				return false;
			}

			if (activeRecipe == null || !inventory.TryAddRecipeOutputs(activeRecipe.BurnedOutputItems)) {
				Debug.Log("[Oven] Yanmış çıktı robot envanterine eklenemedi.");
				return false;
			}

			burnedOutputAvailable = false;
			Debug.Log($"[Oven] {GetRecipeName(activeRecipe)} yanmış çıktısı alındı. Fırın kirli kaldı.");
			return true;
		}

		if (state == OvenState.Cooking || state == OvenState.Placed) {
			Debug.Log("[Oven] Yemek henüz hazır değil.");
			return false;
		}

		Debug.Log("[Oven] Alınacak bir şey yok.");
		return false;
	}

	public bool Take() {
		Debug.Log("[Oven] Çıktıyı almak için robot envanteri bağlamı gerekli.");
		return false;
	}

	public void Clean() {
		if (state != OvenState.BurnedDirty) {
			Debug.Log("[Oven] Temizlenecek bir kir yok.");
			return;
		}

		ResetToEmpty();
		Debug.Log("[Oven] Ocak temizlendi.");
	}

	private void Update() {
		TickOven(Time.deltaTime);
	}

	public void TickOven(float deltaTime) {
		if (state == OvenState.Cooking) {
			timer += deltaTime;
			if (activeRecipe != null && timer >= activeRecipe.CookingDuration) {
				state = OvenState.Ready;
				timer = 0f;
				Debug.Log($"[Oven] {GetRecipeName(activeRecipe)} hazırlandı");
			}
			return;
		}

		if (state == OvenState.Ready) {
			timer += deltaTime;
			if (activeRecipe != null && timer >= activeRecipe.BurnDuration) {
				state = OvenState.BurnedDirty;
				timer = 0f;
				burnedOutputAvailable = HasValidEntries(activeRecipe.BurnedOutputItems);
				Debug.Log($"[Oven] {GetRecipeName(activeRecipe)} yandı");
			}
		}
	}

	private bool TryFindMatchingRecipe(RobotInventory inventory, out FurnaceRecipeSO recipe) {
		for (int i = 0; i < recipes.Count; i++) {
			FurnaceRecipeSO candidate = recipes[i];
			if (candidate == null) {
				continue;
			}

			if (inventory.HasIngredients(candidate.InputItems)) {
				recipe = candidate;
				return true;
			}
		}

		recipe = null;
		return false;
	}

	private bool TryFindMatchingRecipe(IReadOnlyList<ItemSO> items, out FurnaceRecipeSO recipe) {
		if (items == null || items.Count == 0) {
			recipe = null;
			return false;
		}

		for (int i = 0; i < recipes.Count; i++) {
			FurnaceRecipeSO candidate = recipes[i];
			if (candidate == null) {
				continue;
			}

			if (RecipeMatchesItems(candidate.InputItems, items)) {
				recipe = candidate;
				return true;
			}
		}

		recipe = null;
		return false;
	}

	private static bool RecipeMatchesItems(IReadOnlyList<RecipeItemStack> recipeItems, IReadOnlyList<ItemSO> providedItems) {
		if (recipeItems == null || providedItems == null) {
			return false;
		}

		Dictionary<ItemSO, int> required = BuildItemCounts(recipeItems);
		Dictionary<ItemSO, int> provided = BuildItemCounts(providedItems);
		if (required.Count != provided.Count) {
			return false;
		}

		foreach (KeyValuePair<ItemSO, int> pair in required) {
			int providedCount;
			if (!provided.TryGetValue(pair.Key, out providedCount) || providedCount != pair.Value) {
				return false;
			}
		}

		return true;
	}

	private static Dictionary<ItemSO, int> BuildItemCounts(IReadOnlyList<RecipeItemStack> recipeItems) {
		var counts = new Dictionary<ItemSO, int>();
		for (int i = 0; i < recipeItems.Count; i++) {
			RecipeItemStack entry = recipeItems[i];
			if (entry == null || !entry.IsValid) {
				continue;
			}

			if (counts.ContainsKey(entry.Item)) {
				counts[entry.Item] += entry.Quantity;
			} else {
				counts.Add(entry.Item, entry.Quantity);
			}
		}

		return counts;
	}

	private static Dictionary<ItemSO, int> BuildItemCounts(IReadOnlyList<ItemSO> items) {
		var counts = new Dictionary<ItemSO, int>();
		for (int i = 0; i < items.Count; i++) {
			ItemSO item = items[i];
			if (item == null) {
				continue;
			}

			if (counts.ContainsKey(item)) {
				counts[item] += 1;
			} else {
				counts.Add(item, 1);
			}
		}

		return counts;
	}

	private static bool HasValidEntries(IReadOnlyList<RecipeItemStack> entries) {
		if (entries == null) {
			return false;
		}

		for (int i = 0; i < entries.Count; i++) {
			RecipeItemStack entry = entries[i];
			if (entry != null && entry.IsValid) {
				return true;
			}
		}

		return false;
	}

	private static string GetRecipeName(FurnaceRecipeSO recipe) {
		if (recipe == null || string.IsNullOrWhiteSpace(recipe.DisplayName)) {
			return "ürün";
		}

		return recipe.DisplayName;
	}

	private void ResetToEmpty() {
		state = OvenState.Empty;
		timer = 0f;
		activeRecipe = null;
		burnedOutputAvailable = false;
	}

	public void OnPlaced(PlaceableObject placedObject) {
		// called when object is placed on the grid
		// can be used to initialize state
	}

	public void OnRemoved(PlaceableObject placedObject) {
		// cleanup
	}
}
