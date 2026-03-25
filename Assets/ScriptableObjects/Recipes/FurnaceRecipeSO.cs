using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FurnaceRecipe", menuName = "OtoRestoran/Recipes/Furnace Recipe")]
public class FurnaceRecipeSO : RecipeSO {
	[SerializeField] private float cookingDuration = 5f;
	[SerializeField] private float burnDuration = 3f;
	[SerializeField] private List<RecipeItemStack> burnedOutputItems = new List<RecipeItemStack>();

	public float CookingDuration => cookingDuration;

	public float BurnDuration => burnDuration;

	public IReadOnlyList<RecipeItemStack> BurnedOutputItems => burnedOutputItems;

	protected override void OnValidate() {
		base.OnValidate();

		if (cookingDuration < 0f) {
			cookingDuration = 0f;
		}

		if (burnDuration < 0f) {
			burnDuration = 0f;
		}

		if (burnedOutputItems == null) {
			return;
		}

		for (int i = 0; i < burnedOutputItems.Count; i++) {
			RecipeItemStack entry = burnedOutputItems[i];
			if (entry == null) {
				continue;
			}

			entry.ClampQuantity();
		}
	}
}
