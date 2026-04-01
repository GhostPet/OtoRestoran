using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class EditorTestUtility {
	public static ItemSO CreateItem(string itemId, string displayName, bool orderable = true, int maxStack = 10) {
		var item = ScriptableObject.CreateInstance<ItemSO>();
		SetPrivateField(item, "itemId", itemId);
		SetPrivateField(item, "displayName", displayName);
		SetPrivateField(item, "orderable", orderable);
		SetPrivateField(item, "maxStack", maxStack);
		return item;
	}

	public static RecipeItemStack CreateRecipeItemStack(ItemSO item, int quantity) {
		var stack = new RecipeItemStack();
		SetPrivateField(stack, "item", item);
		SetPrivateField(stack, "quantity", quantity);
		return stack;
	}

	public static FurnaceRecipeSO CreateFurnaceRecipe(
		string recipeId,
		string displayName,
		float cookingDuration,
		float burnDuration,
		IReadOnlyList<RecipeItemStack> inputItems,
		IReadOnlyList<RecipeItemStack> outputItems,
		IReadOnlyList<RecipeItemStack> burnedOutputItems) {
		var recipe = ScriptableObject.CreateInstance<FurnaceRecipeSO>();
		SetPrivateField(recipe, "recipeId", recipeId);
		SetPrivateField(recipe, "displayName", displayName);
		SetPrivateField(recipe, "inputItems", inputItems != null ? new List<RecipeItemStack>(inputItems) : new List<RecipeItemStack>());
		SetPrivateField(recipe, "outputItems", outputItems != null ? new List<RecipeItemStack>(outputItems) : new List<RecipeItemStack>());
		SetPrivateField(recipe, "cookingDuration", cookingDuration);
		SetPrivateField(recipe, "burnDuration", burnDuration);
		SetPrivateField(recipe, "burnedOutputItems", burnedOutputItems != null ? new List<RecipeItemStack>(burnedOutputItems) : new List<RecipeItemStack>());
		return recipe;
	}

	public static void InitializeRobotInventory(RobotInventory inventory) {
		InvokePrivateMethod(inventory, "Awake");
	}

	public static void SetPrivateField(object target, string fieldName, object value) {
		FieldInfo field = FindField(target.GetType(), fieldName);
		if (field == null) {
			throw new AssertionException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
		}

		field.SetValue(target, value);
	}

	public static T GetPrivateField<T>(object target, string fieldName) {
		FieldInfo field = FindField(target.GetType(), fieldName);
		if (field == null) {
			throw new AssertionException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
		}

		return (T)field.GetValue(target);
	}

	public static void InvokePrivateMethod(object target, string methodName) {
		if (target == null) {
			throw new AssertionException($"Cannot invoke method '{methodName}' on a null target.");
		}

		MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null) {
			throw new AssertionException($"Method '{methodName}' was not found on {target.GetType().Name}.");
		}

		method.Invoke(target, null);
	}

	public static bool ContainsWrapped<T>(IReadOnlyList<object> wrappedObjects, T expected) where T : class {
		if (wrappedObjects == null || expected == null) {
			return false;
		}

		for (int i = 0; i < wrappedObjects.Count; i++) {
			if (ReferenceEquals(wrappedObjects[i], expected)) {
				return true;
			}
		}

		return false;
	}

	public static void DestroyAll(params UnityEngine.Object[] objects) {
		if (objects == null) {
			return;
		}

		for (int i = 0; i < objects.Length; i++) {
			UnityEngine.Object current = objects[i];
			if (current != null) {
				UnityEngine.Object.DestroyImmediate(current);
			}
		}
	}

	private static FieldInfo FindField(Type type, string fieldName) {
		while (type != null) {
			FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (field != null) {
				return field;
			}

			type = type.BaseType;
		}

		return null;
	}
}

public sealed class TestRobotStub : MonoBehaviour, IRobot {
	private Vector3 lastMoveTarget;
	private bool isMoving;

	public Vector3 Position => transform.position;
	public Vector3 LastMoveTarget => lastMoveTarget;
	public bool IsMoving => isMoving;

	public void MoveTo(Vector3 worldPosition) {
		lastMoveTarget = worldPosition;
		transform.position = worldPosition;
		isMoving = false;
	}

	public void StartMoveTo(Vector3 worldPosition) {
		lastMoveTarget = worldPosition;
		isMoving = true;
	}

	public void FinishMove() {
		transform.position = lastMoveTarget;
		isMoving = false;
	}
}
