using NUnit.Framework;
using UnityEngine;

public class CommandBehaviorTests {
	[SetUp]
	public void SetUp() {
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "default";
		CommandExecutionContext.CurrentLine = -1;
		CommandExecutionContext.CurrentRobot = null;
		WaitCommand.DeltaProvider = null;
		ActiveOrders.Clear();
	}

	[TearDown]
	public void TearDown() {
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "default";
		CommandExecutionContext.CurrentLine = -1;
		CommandExecutionContext.CurrentRobot = null;
		WaitCommand.DeltaProvider = null;
		ActiveOrders.Clear();
	}

	[Test]
	public void BuiltinCommandRegistry_ReturnsFreshInstancesForStatefulCommands() {
		IRobotCommand first = BuiltinCommandRegistry.GetCommand("wait");
		IRobotCommand second = BuiltinCommandRegistry.GetCommand("wait");

		Assert.IsNotNull(first);
		Assert.IsNotNull(second);
		Assert.AreNotSame(first, second);
	}

	[Test]
	public void WaitCommand_CompletesUsingInjectedDeltaProvider() {
		var command = new WaitCommand();
		WaitCommand.DeltaProvider = () => 0.05f;

		try {
			Assert.IsFalse(command.Tick(0.1f));
			Assert.IsTrue(command.Tick());
		} finally {
			WaitCommand.DeltaProvider = null;
		}
	}

	[Test]
	public void MoveToCommand_StartsRobotMovementAndCompletesAfterArrival() {
		var robotGo = new GameObject("robot_move_test");
		var robot = robotGo.AddComponent<TestRobotStub>();
		var command = new MoveToCommand();

		try {
			CommandExecutionContext.CurrentRobot = robot;
			CommandExecutionContext.CurrentLine = 12;

			bool firstTick = command.Tick(3f, 7f);
			Assert.IsFalse(firstTick);
			Assert.IsTrue(robot.IsMoving);
			Assert.AreEqual(new Vector3(3f, 0f, 7f), robot.LastMoveTarget);

			robot.FinishMove();

			bool secondTick = command.Tick(3f, 7f);
			Assert.IsTrue(secondTick);
		} finally {
			EditorTestUtility.DestroyAll(robotGo);
		}
	}

	[Test]
	public void MoveToCommand_UsesNearestTableServePointForRobot() {
		var robotGo = new GameObject("robot_table_move_test");
		var tableGo = new GameObject("table_move_target_test");
		var nearPointGo = new GameObject("serve_point_near");
		var farPointGo = new GameObject("serve_point_far");
		var robot = robotGo.AddComponent<TestRobotStub>();
		var table = tableGo.AddComponent<TableBehavior>();
		var command = new MoveToCommand();

		try {
			robotGo.transform.position = Vector3.zero;
			nearPointGo.transform.SetParent(tableGo.transform);
			farPointGo.transform.SetParent(tableGo.transform);
			nearPointGo.transform.position = new Vector3(1f, 0f, 0f);
			farPointGo.transform.position = new Vector3(5f, 0f, 0f);
			EditorTestUtility.SetPrivateField(table, "servePoints", new[] { farPointGo.transform, nearPointGo.transform });

			CommandExecutionContext.CurrentRobot = robot;
			CommandExecutionContext.CurrentLine = 24;

			bool firstTick = command.Tick(table);
			Assert.IsFalse(firstTick);
			Assert.AreEqual(nearPointGo.transform.position, robot.LastMoveTarget);
		} finally {
			EditorTestUtility.DestroyAll(farPointGo, nearPointGo, tableGo, robotGo);
		}
	}

	[Test]
	public void ServeOrderCommand_ServesWaitingCustomerFromRobotInventory() {
		var robotGo = new GameObject("robot_serve_test");
		var tableGo = new GameObject("table_serve_test");
		var customerGo = new GameObject("customer_serve_test");
		var robot = robotGo.AddComponent<TestRobotStub>();
		var inventory = robotGo.AddComponent<RobotInventory>();
		var table = tableGo.AddComponent<TableBehavior>();
		var customer = customerGo.AddComponent<Customer>();
		var item = EditorTestUtility.CreateItem("burger", "Burger", true, 10);
		var order = new Order { Customer = customer };
		var command = new ServeOrderCommand();

		try {
			EditorTestUtility.InitializeRobotInventory(inventory);
			inventory.TryAddItem(item, 1);
			order.AddItem(new OrderItem(item, 1));
			EditorTestUtility.SetPrivateField(customer, "table", table);
			EditorTestUtility.SetPrivateField(customer, "state", CustomerState.Waiting);
			EditorTestUtility.SetPrivateField(customer, "currentOrder", order);
			robotGo.transform.position = tableGo.transform.position;

			CommandExecutionContext.CurrentRobot = robot;
			CommandExecutionContext.CurrentLine = 38;

			bool firstTick = command.Tick(order);
			Assert.IsFalse(firstTick);
			Assert.IsTrue(customer.IsOrderServed);
			Assert.AreEqual(0, inventory.GetQuantity(item));

			bool secondTick = command.Tick(order);
			Assert.IsTrue(secondTick);
		} finally {
			EditorTestUtility.DestroyAll(item, customerGo, tableGo, robotGo);
		}
	}
}
