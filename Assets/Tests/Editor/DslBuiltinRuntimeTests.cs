using NUnit.Framework;
using System.Collections.Generic;

public class DslBuiltinRuntimeTests {
	[Test]
	public void PrintBuiltin_WritesFormattedMessage() {
		var sink = new TestOutputSink();
		var runtime = new DslBuiltinRuntime(new TestWorldAdapter(), sink, new DslFacadeFactory());

		runtime.Invoke("print", new object[] { "robot", new List<object> { 1f, 2f } }, 3);

		Assert.AreEqual(1, sink.Messages.Count);
		Assert.AreEqual("robot [1, 2]", sink.Messages[0]);
	}

	[Test]
	public void WaitBuiltin_ReturnsAsyncOperation() {
		var runtime = new DslBuiltinRuntime(new TestWorldAdapter(), new TestOutputSink(), new DslFacadeFactory());

		DslBuiltinInvocation invocation = runtime.Invoke("wait", new object[] { 0.5f }, 5);

		Assert.IsTrue(invocation.IsAsync);
		Assert.IsFalse(invocation.AsyncOperation.Tick(0.2f));
		Assert.IsTrue(invocation.AsyncOperation.Tick(0.3f));
	}

	[Test]
	public void FindItemBuiltin_ReturnsMatchingItemFacade() {
		var world = new TestWorldAdapter();
		world.ItemsByName["meatball_raw"] = new TestItemAdapter("meatball_raw", "Raw Meatball");
		var runtime = new DslBuiltinRuntime(world, new TestOutputSink(), new DslFacadeFactory());

		DslBuiltinInvocation invocation = runtime.Invoke("find_item", new object[] { "meatball_raw" }, 5);
		ItemFacade item = invocation.Value as ItemFacade;

		Assert.IsNotNull(item);
		Assert.AreEqual("meatball_raw", item.id());
	}

	[Test]
	public void FindItemBuiltin_ReturnsNull_WhenItemIsMissing() {
		var runtime = new DslBuiltinRuntime(new TestWorldAdapter(), new TestOutputSink(), new DslFacadeFactory());

		DslBuiltinInvocation invocation = runtime.Invoke("find_item", new object[] { "missing_item" }, 5);

		Assert.IsNull(invocation.Value);
	}

	[Test]
	public void NearestTableBuiltin_ReturnsClosestTableFacade() {
		var world = new TestWorldAdapter();
		world.CurrentRobot = new TestRobotAdapter("robot-1", new DslPosition(0f, 0f));
		world.Tables.Add(new TestTableAdapter("table-far", new DslPosition(5f, 0f)));
		world.Tables.Add(new TestTableAdapter("table-near", new DslPosition(1f, 0f)));
		var runtime = new DslBuiltinRuntime(world, new TestOutputSink(), new DslFacadeFactory());

		DslBuiltinInvocation invocation = runtime.Invoke("get_nearest_table", new object[0], 8);
		TableFacade table = invocation.Value as TableFacade;

		Assert.IsNotNull(table);
		Assert.AreEqual("table-near", table.id());
	}

	[Test]
	public void GlobalScope_ProvidesEnumValues() {
		var runtime = new DslBuiltinRuntime(new TestWorldAdapter(), new TestOutputSink(), new DslFacadeFactory());
		IReadOnlyDictionary<string, object> scope = runtime.CreateGlobalScope();

		Assert.IsTrue(scope.ContainsKey("CustomerState"));
		Assert.IsTrue(scope.ContainsKey("FurnaceState"));
		Assert.AreEqual("Waiting", ((CustomerStateScope)scope["CustomerState"]).Waiting);
		Assert.AreEqual("Burnt", ((FurnaceStateScope)scope["FurnaceState"]).Burnt);
	}

	[Test]
	public void OrderFacade_ExposesItemsAndCustomer() {
		var customer = new TestCustomerAdapter("customer-1", new DslPosition(2f, 2f));
		var burger = new TestItemAdapter("burger", "Burger");
		var order = new TestOrderAdapter("order-1", customer, new List<IDslOrderItemAdapter> { new TestOrderItemAdapter(burger, 2) });
		var facade = new DslFacadeFactory().WrapOrder(order);

		List<object> items = facade.items();
		OrderItemFacade orderItem = items[0] as OrderItemFacade;

		Assert.IsNotNull(orderItem);
		Assert.AreEqual(2f, orderItem.quantity());
		Assert.AreEqual("burger", orderItem.item().id());
		Assert.AreEqual("customer-1", facade.customer().id());
	}

	[Test]
	public void FacadeFactory_ReturnsNull_WhenOptionalAdapterIsMissing() {
		var factory = new DslFacadeFactory();

		Assert.IsNull(factory.WrapOrder(null));
		Assert.IsNull(factory.WrapCustomer(null));
		Assert.IsNull(factory.WrapTable(null));
	}

	[Test]
	public void CustomerFacade_TakeOrder_ReturnsNull_WhenAdapterReturnsNull() {
		var customer = new TestCustomerAdapter("customer-1", new DslPosition(2f, 2f));
		var facade = new DslFacadeFactory().WrapCustomer(customer);

		OrderFacade order = facade.take_order();

		Assert.IsNull(order);
	}

	[Test]
	public void CustomerHasOrder_ReflectsCurrentOrderAvailability() {
		var customerWithoutOrder = new TestCustomerAdapter("customer-1", new DslPosition(2f, 2f)) {
			HasActiveOrderValue = false
		};
		var customerWithOrder = new TestCustomerAdapter("customer-2", new DslPosition(3f, 2f)) {
			HasActiveOrderValue = true,
			CurrentOrderValue = new TestOrderAdapter("order-1", null, new List<IDslOrderItemAdapter>())
		};

		Assert.IsFalse(new DslFacadeFactory().WrapCustomer(customerWithoutOrder).has_order());
		Assert.IsTrue(new DslFacadeFactory().WrapCustomer(customerWithOrder).has_order());
	}

	[Test]
	public void ZeroArgumentFacadeMethods_CanBeCalledWithoutArguments() {
		var robot = new TestRobotAdapter("robot-1", new DslPosition(1f, 2f));
		var customer = new TestCustomerAdapter("customer-1", new DslPosition(2f, 2f));
		var order = new TestOrderAdapter("order-1", customer, new List<IDslOrderItemAdapter>());
		var factory = new DslFacadeFactory();

		RobotFacade robotFacade = factory.WrapRobot(robot);
		CustomerFacade customerFacade = factory.WrapCustomer(customer);
		OrderFacade orderFacade = factory.WrapOrder(order);

		Assert.DoesNotThrow(() => robotFacade.inventory());
		Assert.DoesNotThrow(() => customerFacade.has_order());
		Assert.DoesNotThrow(() => orderFacade.is_completed());
	}

	private sealed class TestOutputSink : IDslOutputSink {
		public readonly List<string> Messages = new List<string>();

		public void Write(string message, bool isError = false) {
			Messages.Add(message);
		}
	}

	private sealed class TestWorldAdapter : IDslWorldAdapter {
		public IDslRobotAdapter CurrentRobot { get; set; }
		public List<IDslRobotAdapter> Robots { get; } = new List<IDslRobotAdapter>();
		public List<IDslTableAdapter> Tables { get; } = new List<IDslTableAdapter>();
		public List<IDslFurnaceAdapter> Furnaces { get; } = new List<IDslFurnaceAdapter>();
		public List<IDslFridgeAdapter> Fridges { get; } = new List<IDslFridgeAdapter>();
		public List<IDslTrashcanAdapter> Trashcans { get; } = new List<IDslTrashcanAdapter>();
		public List<IDslOrderAdapter> Orders { get; } = new List<IDslOrderAdapter>();
		public Dictionary<string, IDslItemAdapter> ItemsByName { get; } = new Dictionary<string, IDslItemAdapter>();

		public IDslRobotAdapter GetCurrentRobot() => CurrentRobot;
		public IReadOnlyList<IDslRobotAdapter> GetRobots() => Robots;
		public IReadOnlyList<IDslTableAdapter> GetTables() => Tables;
		public IReadOnlyList<IDslFurnaceAdapter> GetFurnaces() => Furnaces;
		public IReadOnlyList<IDslFridgeAdapter> GetFridges() => Fridges;
		public IReadOnlyList<IDslTrashcanAdapter> GetTrashcans() => Trashcans;
		public IReadOnlyList<IDslOrderAdapter> GetOrders() => Orders;
		public IDslItemAdapter FindItem(string itemName) => itemName != null && ItemsByName.TryGetValue(itemName, out IDslItemAdapter item) ? item : null;
	}

	private sealed class TestRobotAdapter : IDslRobotAdapter {
		public TestRobotAdapter(string id, DslPosition position) {
			Id = id;
			Position = position;
			Inventory = new TestRobotInventoryAdapter();
		}

		public string Id { get; }
		public DslPosition Position { get; private set; }
		public bool IsMoving { get; private set; }
		public IDslRobotInventoryAdapter Inventory { get; }

		public bool IsNear(IDslEntityAdapter target) {
			return target != null && Position.DistanceTo(target.Position) <= 1.5f;
		}

		public void MoveTo(DslPosition position) {
			Position = position;
			IsMoving = true;
		}
	}

	private sealed class TestRobotInventoryAdapter : IDslRobotInventoryAdapter {
		public bool HasItem(IDslItemAdapter item, int quantity) => false;
		public bool HasEmptySlot() => true;
		public bool IsEmpty() => true;
		public int SlotCount => 4;
		public int Quantity(IDslItemAdapter item) => 0;
		public IReadOnlyList<IDslInventorySlotAdapter> Slots => new List<IDslInventorySlotAdapter>();
		public bool TryAddItem(IDslItemAdapter item, int quantity) => true;
		public bool TryRemoveItem(IDslItemAdapter item, int quantity) => true;
	}

	private sealed class TestTableAdapter : IDslTableAdapter {
		public TestTableAdapter(string id, DslPosition position) {
			Id = id;
			Position = position;
		}

		public string Id { get; }
		public DslPosition Position { get; }
		public IReadOnlyList<IDslCustomerAdapter> Customers => new List<IDslCustomerAdapter>();
		public bool IsDirty => false;
		public bool Clean() => true;
	}

	private sealed class TestCustomerAdapter : IDslCustomerAdapter {
		public TestCustomerAdapter(string id, DslPosition position) {
			Id = id;
			Position = position;
		}

		public string Id { get; }
		public DslPosition Position { get; }
		public IDslTableAdapter Table => null;
		public DslCustomerState State => DslCustomerState.Waiting;
		public bool HasActiveOrder => HasActiveOrderValue;
		public IDslOrderAdapter CurrentOrder => CurrentOrderValue;
		public bool HasActiveOrderValue { get; set; } = true;
		public IDslOrderAdapter CurrentOrderValue { get; set; }
		public IDslOrderAdapter TakeOrder() => null;
	}

	private sealed class TestOrderAdapter : IDslOrderAdapter {
		public TestOrderAdapter(string id, IDslCustomerAdapter customer, IReadOnlyList<IDslOrderItemAdapter> items) {
			Id = id;
			Customer = customer;
			Items = items;
		}

		public string Id { get; }
		public IReadOnlyList<IDslOrderItemAdapter> Items { get; }
		public bool IsCompleted => false;
		public IDslCustomerAdapter Customer { get; }
	}

	private sealed class TestOrderItemAdapter : IDslOrderItemAdapter {
		public TestOrderItemAdapter(IDslItemAdapter item, int quantity) {
			Item = item;
			Quantity = quantity;
		}

		public IDslItemAdapter Item { get; }
		public int Quantity { get; }
	}

	private sealed class TestItemAdapter : IDslItemAdapter {
		public TestItemAdapter(string id, string displayName) {
			Id = id;
			DisplayName = displayName;
		}

		public string Id { get; }
		public string DisplayName { get; }
	}
}
