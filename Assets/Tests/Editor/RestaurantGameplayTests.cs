using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RestaurantGameplayTests {
	[SetUp]
	public void SetUp() {
		ActiveOrders.Clear();
	}

	[TearDown]
	public void TearDown() {
		ActiveOrders.Clear();
	}

	[Test]
	public void TableBehavior_RegisterCustomer_IgnoresDuplicatesAndSupportsRemoval() {
		var tableGo = new GameObject("gameplay_table_test");
		var customerGo = new GameObject("gameplay_customer_test");
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();

		try {
			table.RegisterCustomer(customer);
			table.RegisterCustomer(customer);

			Assert.AreEqual(1, table.Customers.Count);
			Assert.IsTrue(table.HasCustomer());

			table.UnregisterCustomer(customer);

			Assert.AreEqual(0, table.Customers.Count);
			Assert.IsTrue(table.IsEmpty());
		} finally {
			EditorTestUtility.DestroyAll(customerGo, tableGo);
		}
	}

	[Test]
	public void Order_TryConsumeItem_RemovesCompletedEntries() {
		var item = EditorTestUtility.CreateItem("soup", "Soup", true, 10);
		var order = new Order();

		try {
			order.AddItem(new OrderItem(item, 2));

			bool consumedOne = order.TryConsumeItem(item, 1);
			bool consumedSecond = order.TryConsumeItem(item, 1);

			Assert.IsTrue(consumedOne);
			Assert.IsTrue(consumedSecond);
			Assert.AreEqual(0, order.GetRemainingQuantity(item));
			Assert.IsTrue(order.IsCompleted);
			Assert.AreEqual(0, order.Items.Count);
		} finally {
			EditorTestUtility.DestroyAll(item);
		}
	}

	[Test]
	public void ActiveOrders_RemoveByCustomer_RemovesOnlyMatchingOrders() {
		var firstCustomerGo = new GameObject("active_order_first_customer_test");
		var secondCustomerGo = new GameObject("active_order_second_customer_test");
		var firstCustomer = firstCustomerGo.AddComponent<Customer>();
		var secondCustomer = secondCustomerGo.AddComponent<Customer>();
		var firstOrder = new Order { Customer = firstCustomer };
		var secondOrder = new Order { Customer = secondCustomer };

		try {
			ActiveOrders.Remember(firstOrder);
			ActiveOrders.Remember(secondOrder);

			ActiveOrders.RemoveByCustomer(firstCustomer);

			Assert.AreEqual(1, ActiveOrders.Orders.Count);
			Assert.AreSame(secondOrder, ActiveOrders.Orders[0]);
		} finally {
			EditorTestUtility.DestroyAll(secondCustomerGo, firstCustomerGo);
		}
	}

	[Test]
	public void OvenBehavior_PlaceCookTake_ConsumesInputsAndProducesOutputs() {
		var ovenGo = new GameObject("oven_gameplay_test");
		var robotGo = new GameObject("oven_robot_test");
		var oven = ovenGo.AddComponent<OvenBehavior>();
		var inventory = robotGo.AddComponent<RobotInventory>();
		var inputItem = EditorTestUtility.CreateItem("raw_patty", "Raw Patty", true, 10);
		var outputItem = EditorTestUtility.CreateItem("cooked_patty", "Cooked Patty", true, 10);
		var recipe = EditorTestUtility.CreateFurnaceRecipe(
			"grill_patty",
			"Grill Patty",
			0.5f,
			0.5f,
			new List<RecipeItemStack> { EditorTestUtility.CreateRecipeItemStack(inputItem, 1) },
			new List<RecipeItemStack> { EditorTestUtility.CreateRecipeItemStack(outputItem, 1) },
			new List<RecipeItemStack>());

		try {
			EditorTestUtility.InitializeRobotInventory(inventory);
			EditorTestUtility.SetPrivateField(oven, "recipes", new List<FurnaceRecipeSO> { recipe });
			inventory.TryAddItem(inputItem, 1);

			Assert.IsTrue(oven.Place(inventory));
			Assert.AreEqual(0, inventory.GetQuantity(inputItem));
			Assert.AreEqual(OvenBehavior.OvenState.Placed, oven.State);

			oven.StartCooking();
			oven.TickOven(0.5f);
			Assert.AreEqual(OvenBehavior.OvenState.Ready, oven.State);

			Assert.IsTrue(oven.Take(inventory));
			Assert.AreEqual(OvenBehavior.OvenState.Empty, oven.State);
			Assert.AreEqual(1, inventory.GetQuantity(outputItem));
		} finally {
			EditorTestUtility.DestroyAll(recipe, outputItem, inputItem, robotGo, ovenGo);
		}
	}

	[Test]
	public void OvenBehavior_BurnedOutputCanBeTakenAndThenRequiresCleaning() {
		var ovenGo = new GameObject("oven_burned_gameplay_test");
		var robotGo = new GameObject("oven_burned_robot_test");
		var oven = ovenGo.AddComponent<OvenBehavior>();
		var inventory = robotGo.AddComponent<RobotInventory>();
		var inputItem = EditorTestUtility.CreateItem("raw_fish", "Raw Fish", true, 10);
		var outputItem = EditorTestUtility.CreateItem("cooked_fish", "Cooked Fish", true, 10);
		var burnedOutputItem = EditorTestUtility.CreateItem("burned_fish", "Burned Fish", true, 10);
		var recipe = EditorTestUtility.CreateFurnaceRecipe(
			"grill_fish",
			"Grill Fish",
			0.25f,
			0.25f,
			new List<RecipeItemStack> { EditorTestUtility.CreateRecipeItemStack(inputItem, 1) },
			new List<RecipeItemStack> { EditorTestUtility.CreateRecipeItemStack(outputItem, 1) },
			new List<RecipeItemStack> { EditorTestUtility.CreateRecipeItemStack(burnedOutputItem, 1) });

		try {
			EditorTestUtility.InitializeRobotInventory(inventory);
			EditorTestUtility.SetPrivateField(oven, "recipes", new List<FurnaceRecipeSO> { recipe });
			inventory.TryAddItem(inputItem, 1);

			Assert.IsTrue(oven.Place(inventory));
			oven.StartCooking();
			oven.TickOven(0.25f);
			Assert.AreEqual(OvenBehavior.OvenState.Ready, oven.State);

			oven.TickOven(0.25f);
			Assert.AreEqual(OvenBehavior.OvenState.BurnedDirty, oven.State);

			Assert.IsTrue(oven.Take(inventory));
			Assert.AreEqual(1, inventory.GetQuantity(burnedOutputItem));
			Assert.AreEqual(OvenBehavior.OvenState.BurnedDirty, oven.State);

			oven.Clean();
			Assert.AreEqual(OvenBehavior.OvenState.Empty, oven.State);
		} finally {
			EditorTestUtility.DestroyAll(recipe, burnedOutputItem, outputItem, inputItem, robotGo, ovenGo);
		}
	}
}
