using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuiltinObjectCoverageTests {
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
	public void TableBuiltinClass_ExposesCustomerMembers() {
		var tableGo = new GameObject("builtin_table_test");
		var customerGo = new GameObject("builtin_table_customer_test");
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();

		try {
			table.RegisterCustomer(customer);
			var builtin = new TableBuiltinClass(table, ScriptInvocationContext.Create(null));

			Assert.IsTrue(builtin.TryGetMember("customers", out object customersResult));
			Assert.IsTrue(builtin.TryGetMember("customer_count", out object countResult));
			Assert.IsTrue(builtin.TryGetMember("has_customer", out object hasCustomerResult));
			Assert.IsTrue(builtin.TryGetMember("is_empty", out object isEmptyResult));

			var wrappedCustomers = customersResult as List<object>;
			Assert.IsNotNull(wrappedCustomers);
			Assert.IsTrue(EditorTestUtility.ContainsWrapped(wrappedCustomers, customer));
			Assert.AreEqual(1f, countResult);
			Assert.AreEqual(true, hasCustomerResult);
			Assert.AreEqual(false, isEmptyResult);
		} finally {
			EditorTestUtility.DestroyAll(customerGo, tableGo);
		}
	}

	[Test]
	public void CustomerBuiltinClass_ExposesStateAndOrderMembers() {
		var tableGo = new GameObject("builtin_customer_table_test");
		var customerGo = new GameObject("builtin_customer_test");
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();
		var item = EditorTestUtility.CreateItem("cola", "Cola", true, 10);

		try {
			EditorTestUtility.SetPrivateField(customer, "table", table);
			EditorTestUtility.SetPrivateField(customer, "state", CustomerState.Ordering);
			EditorTestUtility.SetPrivateField(customer, "orderReady", true);
			EditorTestUtility.SetPrivateField(customer, "defaultOrderItems", new List<OrderItem> { new(item, 1) });

			var robotGo = new GameObject("builtin_customer_robot_test");
			var robot = robotGo.AddComponent<TestRobotStub>();
			robotGo.transform.position = tableGo.transform.position;
			CommandExecutionContext.CurrentRobot = robot;
			var builtin = new CustomerBuiltinClass(customer, ScriptInvocationContext.Create(null));

			try {
				Assert.IsTrue(builtin.TryGetMember("table", out object tableResult));
				Assert.IsTrue(builtin.TryGetMember("state", out object stateResult));
				Assert.IsTrue(builtin.TryGetMember("is_order_ready", out object orderReadyResult));
				Assert.IsTrue(builtin.TryGetMember("current_order", out object orderBeforeGetResult));
				Assert.IsTrue(builtin.TryInvoke("has_order", new object[0], 12, out object hasOrderResult));
				Assert.IsTrue(builtin.TryInvoke("get_order", new object[0], 13, out object getOrderResult));
				Assert.IsTrue(builtin.TryGetMember("current_order", out object orderAfterGetResult));

				Assert.IsInstanceOf<TableBuiltinClass>(tableResult);
				Assert.AreEqual(CustomerState.Ordering.ToString(), stateResult);
				Assert.AreEqual(true, orderReadyResult);
				Assert.IsNull(orderBeforeGetResult);
				Assert.AreEqual(true, hasOrderResult);
				Assert.IsInstanceOf<OrderBuiltinClass>(getOrderResult);
				Assert.IsInstanceOf<OrderBuiltinClass>(orderAfterGetResult);
			} finally {
				EditorTestUtility.DestroyAll(robotGo);
			}
		} finally {
			EditorTestUtility.DestroyAll(item, customerGo, tableGo);
		}
	}

	[Test]
	public void OrderBuiltinClass_ExposesCustomerItemsAndFulfillment() {
		var customerGo = new GameObject("builtin_order_customer_test");
		var robotGo = new GameObject("builtin_order_robot_test");
		var customer = customerGo.AddComponent<Customer>();
		var robot = robotGo.AddComponent<TestRobotStub>();
		var inventory = robotGo.AddComponent<RobotInventory>();
		var item = EditorTestUtility.CreateItem("toast", "Toast", true, 10);
		var order = new Order { Customer = customer };

		try {
			EditorTestUtility.InitializeRobotInventory(inventory);
			inventory.TryAddItem(item, 2);
			order.AddItem(new OrderItem(item, 2));
			CommandExecutionContext.CurrentRobot = robot;

			var builtin = new OrderBuiltinClass(order, ScriptInvocationContext.Create(null));
			Assert.IsTrue(builtin.TryGetMember("customer", out object customerResult));
			Assert.IsTrue(builtin.TryGetMember("items", out object itemsResult));
			Assert.IsTrue(builtin.TryGetMember("item_count", out object countResult));
			Assert.IsTrue(builtin.TryInvoke("is_fulfilled", new object[0], 20, out object isFulfilledResult));

			var wrappedItems = itemsResult as List<object>;
			Assert.IsInstanceOf<CustomerBuiltinClass>(customerResult);
			Assert.IsNotNull(wrappedItems);
			Assert.AreEqual(1, wrappedItems.Count);
			Assert.AreEqual(1f, countResult);
			Assert.AreEqual(true, isFulfilledResult);
		} finally {
			EditorTestUtility.DestroyAll(item, robotGo, customerGo);
		}
	}

	[Test]
	public void RobotBuiltinClass_ExposesPositionAndInventoryMembers() {
		var robotGo = new GameObject("builtin_robot_test");
		var robot = robotGo.AddComponent<TestRobotStub>();
		var inventory = robotGo.AddComponent<RobotInventory>();
		var item = EditorTestUtility.CreateItem("milk", "Milk", true, 10);

		try {
			EditorTestUtility.InitializeRobotInventory(inventory);
			robotGo.transform.position = new Vector3(2f, 0f, 4f);
			inventory.TryAddItem(item, 3);
			var builtin = new RobotBuiltinClass(robot, ScriptInvocationContext.Create(null));

			Assert.IsTrue(builtin.TryGetMember("position", out object positionResult));
			Assert.IsTrue(builtin.TryGetMember("is_moving", out object isMovingResult));
			Assert.IsTrue(builtin.TryGetMember("inventory_slots", out object slotsResult));
			Assert.IsTrue(builtin.TryGetMember("slot_count", out object slotCountResult));

			var wrappedSlots = slotsResult as List<object>;
			Assert.AreEqual(new Vector3(2f, 0f, 4f), positionResult);
			Assert.AreEqual(false, isMovingResult);
			Assert.IsNotNull(wrappedSlots);
			Assert.AreEqual(4, wrappedSlots.Count);
			Assert.AreEqual(4f, slotCountResult);
		} finally {
			EditorTestUtility.DestroyAll(item, robotGo);
		}
	}

	[Test]
	public void FridgeBuiltinClass_Take_TransfersItemsWhenRobotIsNear() {
		var robotGo = new GameObject("builtin_fridge_robot_test");
		var inventoryManagerGo = new GameObject("builtin_fridge_inventory_manager_test");
		var fridgeGo = new GameObject("builtin_fridge_test");
		var robot = robotGo.AddComponent<TestRobotStub>();
		var robotInventory = robotGo.AddComponent<RobotInventory>();
		var inventoryManager = inventoryManagerGo.AddComponent<InventoryManager>();
		var storageInventory = fridgeGo.AddComponent<StorageInventory>();
		var fridge = fridgeGo.AddComponent<FridgeBehavior>();
		var item = EditorTestUtility.CreateItem("lettuce", "Lettuce", true, 10);

		try {
			EditorTestUtility.InitializeRobotInventory(robotInventory);
			EditorTestUtility.SetPrivateField(storageInventory, "inventoryManager", inventoryManager);
			EditorTestUtility.SetPrivateField(fridge, "storageInventory", storageInventory);
			inventoryManager.AddItem(item, 3);
			robotGo.transform.position = fridgeGo.transform.position;

			var builtin = new FridgeBuiltinClass(fridge, new ScriptInvocationContext(robot, null));
			Assert.IsTrue(builtin.TryInvoke("take", new object[] { item, 2f }, 30, out object result));
			Assert.AreEqual(true, result);
			Assert.AreEqual(1, inventoryManager.GetQuantity(item));
			Assert.AreEqual(2, robotInventory.GetQuantity(item));
		} finally {
			EditorTestUtility.DestroyAll(item, fridgeGo, inventoryManagerGo, robotGo);
		}
	}
}
