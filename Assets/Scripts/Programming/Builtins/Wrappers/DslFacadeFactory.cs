using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

internal interface IDslEntityFacade {
	IDslEntityAdapter EntityAdapter { get; }
}

internal interface IDslItemFacade {
	IDslItemAdapter ItemAdapter { get; }
}

internal interface IDslOrderItemFacadeAccessor {
	IDslOrderItemAdapter OrderItemAdapter { get; }
}

public sealed class DslFacadeFactory {
	private readonly Dictionary<string, object> _cache = new Dictionary<string, object>(StringComparer.Ordinal);

	public RobotFacade WrapRobot(IDslRobotAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("robot", adapter != null ? adapter.Id : null, () => new RobotFacade(adapter, this));
	}

	public RobotInventoryFacade WrapRobotInventory(IDslRobotInventoryAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("robot_inventory", adapter != null ? adapter.GetHashCode().ToString(CultureInfo.InvariantCulture) : null, () => new RobotInventoryFacade(adapter, this));
	}

	public TableFacade WrapTable(IDslTableAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("table", adapter != null ? adapter.Id : null, () => new TableFacade(adapter, this));
	}

	public CustomerFacade WrapCustomer(IDslCustomerAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("customer", adapter != null ? adapter.Id : null, () => new CustomerFacade(adapter, this));
	}

	public OrderFacade WrapOrder(IDslOrderAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("order", adapter != null ? adapter.Id : null, () => new OrderFacade(adapter, this));
	}

	public FurnaceFacade WrapFurnace(IDslFurnaceAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("furnace", adapter != null ? adapter.Id : null, () => new FurnaceFacade(adapter, this));
	}

	public FridgeFacade WrapFridge(IDslFridgeAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("fridge", adapter != null ? adapter.Id : null, () => new FridgeFacade(adapter, this));
	}

	public TrashcanFacade WrapTrashcan(IDslTrashcanAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("trashcan", adapter != null ? adapter.Id : null, () => new TrashcanFacade(adapter, this));
	}

	public ItemFacade WrapItem(IDslItemAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return Cache("item", adapter != null ? adapter.Id : null, () => new ItemFacade(adapter));
	}

	public OrderItemFacade WrapOrderItem(IDslOrderItemAdapter adapter) {
		if (adapter == null) {
			return null;
		}

		return new OrderItemFacade(adapter, this);
	}

	public List<object> WrapRobots(IReadOnlyList<IDslRobotAdapter> adapters) {
		return WrapMany(adapters, WrapRobot);
	}

	public List<object> WrapTables(IReadOnlyList<IDslTableAdapter> adapters) {
		return WrapMany(adapters, WrapTable);
	}

	public List<object> WrapCustomers(IReadOnlyList<IDslCustomerAdapter> adapters) {
		return WrapMany(adapters, WrapCustomer);
	}

	public List<object> WrapOrders(IReadOnlyList<IDslOrderAdapter> adapters) {
		return WrapMany(adapters, WrapOrder);
	}

	public List<object> WrapFurnaces(IReadOnlyList<IDslFurnaceAdapter> adapters) {
		return WrapMany(adapters, WrapFurnace);
	}

	public List<object> WrapFridges(IReadOnlyList<IDslFridgeAdapter> adapters) {
		return WrapMany(adapters, WrapFridge);
	}

	public List<object> WrapTrashcans(IReadOnlyList<IDslTrashcanAdapter> adapters) {
		return WrapMany(adapters, WrapTrashcan);
	}

	public List<object> WrapOrderItems(IReadOnlyList<IDslOrderItemAdapter> adapters) {
		return WrapMany(adapters, WrapOrderItem);
	}

	public List<object> WrapItems(IReadOnlyList<IDslItemAdapter> adapters) {
		return WrapMany(adapters, WrapItem);
	}

	internal List<object> WrapEnumerable(IEnumerable values) {
		var wrapped = new List<object>();
		if (values == null) {
			return wrapped;
		}

		foreach (object value in values) {
			wrapped.Add(WrapUnknown(value));
		}

		return wrapped;
	}

	internal object WrapUnknown(object value) {
		if (value == null) {
			return null;
		}

		if (value is IDslRobotAdapter robotAdapter) return WrapRobot(robotAdapter);
		if (value is IDslTableAdapter tableAdapter) return WrapTable(tableAdapter);
		if (value is IDslCustomerAdapter customerAdapter) return WrapCustomer(customerAdapter);
		if (value is IDslOrderAdapter orderAdapter) return WrapOrder(orderAdapter);
		if (value is IDslFurnaceAdapter furnaceAdapter) return WrapFurnace(furnaceAdapter);
		if (value is IDslFridgeAdapter fridgeAdapter) return WrapFridge(fridgeAdapter);
		if (value is IDslTrashcanAdapter trashcanAdapter) return WrapTrashcan(trashcanAdapter);
		if (value is IDslOrderItemAdapter orderItemAdapter) return WrapOrderItem(orderItemAdapter);
		if (value is IDslItemAdapter itemAdapter) return WrapItem(itemAdapter);
		if (value is IEnumerable enumerable && value is not string) return WrapEnumerable(enumerable);
		return value;
	}

	private T Cache<T>(string prefix, string id, Func<T> factory) where T : class {
		if (string.IsNullOrWhiteSpace(id)) {
			return factory();
		}

		string key = prefix + ":" + id;
		if (_cache.TryGetValue(key, out object cached)) {
			return cached as T;
		}

		T created = factory();
		_cache[key] = created;
		return created;
	}

	private List<object> WrapMany<TAdapter, TFacade>(IReadOnlyList<TAdapter> adapters, Func<TAdapter, TFacade> wrap) where TFacade : class {
		var wrapped = new List<object>();
		if (adapters == null) {
			return wrapped;
		}

		for (int i = 0; i < adapters.Count; i++) {
			TFacade value = wrap(adapters[i]);
			if (value != null) {
				wrapped.Add(value);
			}
		}

		return wrapped;
	}
}

public abstract class DslFacadeBase {
	protected DslFacadeBase(DslFacadeFactory factory) {
		Factory = factory;
	}

	protected DslFacadeFactory Factory { get; }
}

public abstract class RestaurantObjectFacade : DslFacadeBase, IDslEntityFacade {
	protected RestaurantObjectFacade(DslFacadeFactory factory)
		: base(factory) {
	}

	internal abstract IDslEntityAdapter EntityAdapter { get; }
	IDslEntityAdapter IDslEntityFacade.EntityAdapter => EntityAdapter;

	public string id() {
		return EntityAdapter.Id;
	}

	public List<object> position() {
		return EntityAdapter.Position.ToDslList();
	}

	public override bool Equals(object obj) {
		if (obj is not IDslEntityFacade other) {
			return false;
		}

		return string.Equals(EntityAdapter.Id, other.EntityAdapter.Id, StringComparison.Ordinal);
	}

	public override int GetHashCode() {
		return EntityAdapter.Id != null ? EntityAdapter.Id.GetHashCode() : 0;
	}

	public override string ToString() {
		return $"{GetType().Name}({EntityAdapter.Id})";
	}
}

public sealed class RobotFacade : RestaurantObjectFacade {
	private readonly IDslRobotAdapter _adapter;

	internal RobotFacade(IDslRobotAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal override IDslEntityAdapter EntityAdapter => _adapter;

	public bool is_moving() {
		return _adapter.IsMoving;
	}

	public bool is_near(object target) {
		return _adapter.IsNear(DslArgumentReader.ReadEntityAdapter(target, "target"));
	}

	public void move(object position) {
		_adapter.MoveTo(DslArgumentReader.ReadPosition(position, "position"));
	}

	public RobotInventoryFacade inventory() {
		return Factory.WrapRobotInventory(_adapter.Inventory);
	}
}

public sealed class RobotInventoryFacade : DslFacadeBase {
	private readonly IDslRobotInventoryAdapter _adapter;

	internal RobotInventoryFacade(IDslRobotInventoryAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	public bool has_item(object item) {
		return _adapter.HasItem(DslArgumentReader.ReadItemAdapter(item, "item"), 1);
	}

	public bool has_empty_slot() {
		return _adapter.HasEmptySlot();
	}

	public bool is_empty() {
		return _adapter.IsEmpty();
	}

	public float slot_count() {
		return _adapter.SlotCount;
	}

	public float quantity(object item) {
		return _adapter.Quantity(DslArgumentReader.ReadItemAdapter(item, "item"));
	}
}

public sealed class TableFacade : RestaurantObjectFacade {
	private readonly IDslTableAdapter _adapter;

	internal TableFacade(IDslTableAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal override IDslEntityAdapter EntityAdapter => _adapter;

	public List<object> customers() {
		return Factory.WrapCustomers(_adapter.Customers);
	}

	public bool is_dirty() {
		return _adapter.IsDirty;
	}

	public bool clean() {
		return _adapter.Clean();
	}
}

public sealed class CustomerFacade : DslFacadeBase, IDslEntityFacade {
	private readonly IDslCustomerAdapter _adapter;

	internal CustomerFacade(IDslCustomerAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal IDslCustomerAdapter Adapter => _adapter;
	IDslEntityAdapter IDslEntityFacade.EntityAdapter => _adapter;

	public string id() {
		return _adapter.Id;
	}

	public TableFacade table() {
		return Factory.WrapTable(_adapter.Table);
	}

	public string state() {
		return _adapter.State.ToString();
	}

	public bool has_order() {
		return _adapter.HasActiveOrder;
	}

	public OrderFacade take_order() {
		return Factory.WrapOrder(_adapter.TakeOrder());
	}

	public OrderFacade order() {
		return Factory.WrapOrder(_adapter.CurrentOrder);
	}

	public List<object> position() {
		return _adapter.Position.ToDslList();
	}

	public override bool Equals(object obj) {
		if (obj is not IDslEntityFacade other) {
			return false;
		}

		return string.Equals(_adapter.Id, other.EntityAdapter.Id, StringComparison.Ordinal);
	}

	public override int GetHashCode() {
		return _adapter.Id != null ? _adapter.Id.GetHashCode() : 0;
	}

	public override string ToString() {
		return $"CustomerFacade({_adapter.Id})";
	}
}

public sealed class OrderFacade : DslFacadeBase {
	private readonly IDslOrderAdapter _adapter;

	internal OrderFacade(IDslOrderAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	public List<object> items() {
		return Factory.WrapOrderItems(_adapter.Items);
	}

	public bool is_completed() {
		return _adapter.IsCompleted;
	}

	public CustomerFacade customer() {
		return Factory.WrapCustomer(_adapter.Customer);
	}

	public string id() {
		return _adapter.Id;
	}
}

public sealed class OrderItemFacade : DslFacadeBase, IDslOrderItemFacadeAccessor, IDslItemFacade {
	private readonly IDslOrderItemAdapter _adapter;

	internal OrderItemFacade(IDslOrderItemAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal IDslOrderItemAdapter Adapter => _adapter;
	IDslOrderItemAdapter IDslOrderItemFacadeAccessor.OrderItemAdapter => _adapter;
	IDslItemAdapter IDslItemFacade.ItemAdapter => _adapter.Item;

	public ItemFacade item() {
		return Factory.WrapItem(_adapter.Item);
	}

	public float quantity() {
		return _adapter.Quantity;
	}
}

public sealed class ItemFacade : DslFacadeBase, IDslItemFacade {
	private readonly IDslItemAdapter _adapter;

	internal ItemFacade(IDslItemAdapter adapter)
		: base(null) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	IDslItemAdapter IDslItemFacade.ItemAdapter => _adapter;

	public string id() {
		return _adapter.Id;
	}

	public string name() {
		return _adapter.DisplayName;
	}

	public override string ToString() {
		return _adapter.DisplayName;
	}
}

public sealed class FurnaceFacade : RestaurantObjectFacade {
	private readonly IDslFurnaceAdapter _adapter;

	internal FurnaceFacade(IDslFurnaceAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal override IDslEntityAdapter EntityAdapter => _adapter;

	public string state() {
		return _adapter.State.ToString();
	}

	public bool is_ready() {
		return _adapter.IsReady;
	}

	public bool is_busy() {
		return _adapter.IsBusy;
	}

	public bool is_dirty() {
		return _adapter.IsDirty;
	}

	public bool place(object itemOrItems) {
		return _adapter.Place(DslArgumentReader.ReadItemAdapters(itemOrItems, "itemOrItems"));
	}

	public bool cook() {
		return _adapter.Cook();
	}

	public List<object> take() {
		return Factory.WrapItems(_adapter.Take());
	}

	public bool clean() {
		return _adapter.Clean();
	}
}

public sealed class FridgeFacade : RestaurantObjectFacade {
	private readonly IDslFridgeAdapter _adapter;

	internal FridgeFacade(IDslFridgeAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal override IDslEntityAdapter EntityAdapter => _adapter;

	public bool has_item(object item, object quantity) {
		return _adapter.HasItem(
			DslArgumentReader.ReadItemAdapter(item, "item"),
			DslArgumentReader.ReadPositiveInteger(quantity, "quantity"));
	}

	public bool has_item(object item) {
		return _adapter.HasItem(DslArgumentReader.ReadItemAdapter(item, "item"), 1);
	}

	public float quantity(object item) {
		return _adapter.Quantity(DslArgumentReader.ReadItemAdapter(item, "item"));
	}

	public bool take(object item, object quantity) {
		return _adapter.Take(
			DslArgumentReader.ReadItemAdapter(item, "item"),
			DslArgumentReader.ReadPositiveInteger(quantity, "quantity"));
	}

	public bool place(object item, object quantity) {
		return _adapter.Place(
			DslArgumentReader.ReadItemAdapter(item, "item"),
			DslArgumentReader.ReadPositiveInteger(quantity, "quantity"));
	}
}

public sealed class TrashcanFacade : RestaurantObjectFacade {
	private readonly IDslTrashcanAdapter _adapter;

	internal TrashcanFacade(IDslTrashcanAdapter adapter, DslFacadeFactory factory)
		: base(factory) {
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	internal override IDslEntityAdapter EntityAdapter => _adapter;

	public bool @throw(object itemOrItems) {
		return _adapter.Throw(DslArgumentReader.ReadItemAdapters(itemOrItems, "itemOrItems"));
	}
}
