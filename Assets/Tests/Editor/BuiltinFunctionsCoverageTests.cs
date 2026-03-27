using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuiltinFunctionsCoverageTests {
	[SetUp]
	public void SetUp() {
		ActiveOrders.Clear();
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "default";
		CommandExecutionContext.CurrentRobot = null;
	}

	[TearDown]
	public void TearDown() {
		ActiveOrders.Clear();
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "default";
		CommandExecutionContext.CurrentRobot = null;
	}

	[Test]
	public void PrimitiveBuiltinFunctions_ReturnExpectedValues() {
		var singleRange = BuiltinFunctions.Invoke("range", new object[] { 4f }) as List<object>;
		var steppedRange = BuiltinFunctions.Invoke("range", new object[] { 5f, 0f, -2f }) as List<object>;
		var values = new List<object> { 1f, 2f, 3f };

		Assert.IsNotNull(singleRange);
		Assert.AreEqual(new List<object> { 0f, 1f, 2f, 3f }, singleRange);
		Assert.IsNotNull(steppedRange);
		Assert.AreEqual(new List<object> { 5f, 3f, 1f }, steppedRange);
		Assert.AreEqual(3f, BuiltinFunctions.Invoke("len", new object[] { values }));
		Assert.AreEqual("hello", BuiltinFunctions.Invoke("str", new object[] { "hello" }));
		Assert.AreEqual(6, BuiltinFunctions.Invoke("int", new object[] { 6f }));
		Assert.AreEqual(2.5f, BuiltinFunctions.Invoke("float", new object[] { "2.5" }));
	}

	[Test]
	public void TypeBuiltin_ReturnsBuiltinNamesForWrappedObjects() {
		var tableGo = new GameObject("type_table_test");
		var customerGo = new GameObject("type_customer_test");
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();

		try {
			Assert.AreEqual("Table", BuiltinFunctions.Invoke("type", new object[] { table }));
			Assert.AreEqual("Customer", BuiltinFunctions.Invoke("type", new object[] { customer }));
		} finally {
			EditorTestUtility.DestroyAll(customerGo, tableGo);
		}
	}

	[Test]
	public void SceneQueryBuiltins_ReturnWrappedSceneObjects() {
		var tableGo = new GameObject("query_table_test");
		var customerGo = new GameObject("query_customer_test");
		var ovenGo = new GameObject("query_oven_test");
		var fridgeGo = new GameObject("query_fridge_test");
		var inventoryManagerGo = new GameObject("query_inventory_manager_test");
		var robotGo = new GameObject("query_robot_test");
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();
		var oven = ovenGo.AddComponent<OvenBehavior>();
		var inventoryManager = inventoryManagerGo.AddComponent<InventoryManager>();
		var storageInventory = fridgeGo.AddComponent<StorageInventory>();
		var fridge = fridgeGo.AddComponent<FridgeBehavior>();
		var robot = robotGo.AddComponent<TestRobotStub>();

		try {
			EditorTestUtility.SetPrivateField(storageInventory, "inventoryManager", inventoryManager);
			EditorTestUtility.SetPrivateField(fridge, "storageInventory", storageInventory);

			var tables = BuiltinFunctions.Invoke("get_tables", new object[0]) as List<object>;
			var customers = BuiltinFunctions.Invoke("get_customers", new object[0]) as List<object>;
			var furnaces = BuiltinFunctions.Invoke("get_furnaces", new object[0]) as List<object>;
			var fridges = BuiltinFunctions.Invoke("get_fridges", new object[0]) as List<object>;
			var robots = BuiltinFunctions.Invoke("get_robots", new object[0]) as List<object>;

			Assert.IsTrue(EditorTestUtility.ContainsWrapped(tables, table));
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(customers, customer));
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(furnaces, oven));
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(fridges, fridge));
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(robots, robot));
		} finally {
			EditorTestUtility.DestroyAll(robotGo, inventoryManagerGo, fridgeGo, ovenGo, customerGo, tableGo);
		}
	}

	[Test]
	public void ActiveOrdersBuiltin_ReturnsWrappedOrders() {
		var customerGo = new GameObject("active_order_customer_test");
		var customer = customerGo.AddComponent<Customer>();
		var item = EditorTestUtility.CreateItem("fries", "Fries", true, 10);
		var order = new Order { Customer = customer };

		try {
			order.AddItem(new OrderItem(item, 1));
			ActiveOrders.Remember(order);

			var wrappedOrders = BuiltinFunctions.Invoke("active_orders", new object[0]) as List<object>;
			Assert.IsNotNull(wrappedOrders);
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(wrappedOrders, order));
		} finally {
			EditorTestUtility.DestroyAll(item, customerGo);
		}
	}
}
