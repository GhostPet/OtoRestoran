using System.Collections.Generic;
using UnityEngine;

public abstract class RecipeSO : ScriptableObject {
	[SerializeField] private string recipeId;
	[SerializeField] private string displayName;
	[SerializeField][TextArea(2, 4)] private string description;
	[SerializeField] private List<RecipeItemStack> inputItems = new List<RecipeItemStack>();
	[SerializeField] private List<RecipeItemStack> outputItems = new List<RecipeItemStack>();

	public string RecipeId => recipeId;

	public string DisplayName => displayName;

	public string Description => description;

	public virtual string RecipeTypeId => GetType().Name;

	public IReadOnlyList<RecipeItemStack> InputItems => inputItems;

	public IReadOnlyList<RecipeItemStack> OutputItems => outputItems;

	protected virtual void OnValidate() {
		ValidateEntries(inputItems);
		ValidateEntries(outputItems);
	}

	private static void ValidateEntries(List<RecipeItemStack> entries) {
		if (entries == null) {
			return;
		}

		for (int i = 0; i < entries.Count; i++) {
			RecipeItemStack entry = entries[i];
			if (entry == null) {
				continue;
			}

			entry.ClampQuantity();
		}
	}
}
