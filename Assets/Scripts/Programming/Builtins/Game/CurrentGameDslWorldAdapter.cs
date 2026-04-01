using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class CurrentGameDslWorldAdapter : IDslWorldAdapter {
	private readonly RobotExecutor _executor;

	public CurrentGameDslWorldAdapter(RobotExecutor executor) {
		_executor = executor;
	}

	public IDslRobotAdapter GetCurrentRobot() {
		IRobot robot = ResolveCurrentRobot();
		return robot != null ? new GameRobotAdapter(robot) : null;
	}

	public IReadOnlyList<IDslRobotAdapter> GetRobots() {
		MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
		var robots = new List<IDslRobotAdapter>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < behaviours.Length; i++) {
			MonoBehaviour behaviour = behaviours[i];
			if (behaviour == null) {
				continue;
			}

			IRobot robot = behaviour as IRobot;
			if (robot == null) {
				continue;
			}

			GameRobotAdapter adapter = new GameRobotAdapter(robot);
			if (seen.Add(adapter.Id)) {
				robots.Add(adapter);
			}
		}

		return robots;
	}

	public IReadOnlyList<IDslTableAdapter> GetTables() {
		TableBehavior[] tables = UnityEngine.Object.FindObjectsByType<TableBehavior>(FindObjectsSortMode.None);
		var adapters = new List<IDslTableAdapter>(tables.Length);
		for (int i = 0; i < tables.Length; i++) {
			if (tables[i] != null) {
				adapters.Add(new GameTableAdapter(tables[i]));
			}
		}

		return adapters;
	}

	public IDslItemAdapter FindItem(string itemName) {
		if (string.IsNullOrWhiteSpace(itemName)) {
			return null;
		}

		string normalizedName = itemName.Trim();
		ItemSO[] items = Resources.FindObjectsOfTypeAll<ItemSO>();
		for (int i = 0; i < items.Length; i++) {
			ItemSO item = items[i];
			if (item == null) {
				continue;
			}

			if (string.Equals(item.ItemId, normalizedName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(item.DisplayName, normalizedName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(item.name, normalizedName, StringComparison.OrdinalIgnoreCase)) {
				return new GameItemAdapter(item);
			}
		}

		return null;
	}

	public IReadOnlyList<IDslFurnaceAdapter> GetFurnaces() {
		OvenBehavior[] furnaces = UnityEngine.Object.FindObjectsByType<OvenBehavior>(FindObjectsSortMode.None);
		var adapters = new List<IDslFurnaceAdapter>(furnaces.Length);
		for (int i = 0; i < furnaces.Length; i++) {
			if (furnaces[i] != null) {
				adapters.Add(new GameFurnaceAdapter(furnaces[i], this));
			}
		}

		return adapters;
	}

	public IReadOnlyList<IDslFridgeAdapter> GetFridges() {
		FridgeBehavior[] fridges = UnityEngine.Object.FindObjectsByType<FridgeBehavior>(FindObjectsSortMode.None);
		var adapters = new List<IDslFridgeAdapter>(fridges.Length);
		for (int i = 0; i < fridges.Length; i++) {
			if (fridges[i] != null) {
				adapters.Add(new GameFridgeAdapter(fridges[i], this));
			}
		}

		return adapters;
	}

	public IReadOnlyList<IDslTrashcanAdapter> GetTrashcans() {
		return Array.Empty<IDslTrashcanAdapter>();
	}

	public IReadOnlyList<IDslOrderAdapter> GetOrders() {
		List<Order> orders = ActiveOrders.Snapshot();
		var adapters = new List<IDslOrderAdapter>(orders.Count);
		for (int i = 0; i < orders.Count; i++) {
			if (orders[i] != null) {
				adapters.Add(new GameOrderAdapter(orders[i]));
			}
		}

		return adapters;
	}

	internal RobotInventory GetCurrentRobotInventory() {
		Component component = ResolveCurrentRobot() as Component;
		if (component == null) {
			return null;
		}

		return component.GetComponent<RobotInventory>();
	}

	private IRobot ResolveCurrentRobot() {
		if (_executor == null) {
			return null;
		}

		MonoBehaviour[] behaviours = _executor.GetComponents<MonoBehaviour>();
		for (int i = 0; i < behaviours.Length; i++) {
			IRobot robot = behaviours[i] as IRobot;
			if (robot != null) {
				return robot;
			}
		}

		return null;
	}

	private static string CreateObjectId(UnityEngine.Object value, string prefix) {
		if (value == null) {
			return prefix + ":null";
		}

		return prefix + ":" + value.GetInstanceID().ToString(CultureInfo.InvariantCulture);
	}

	private static string CreateReferenceId(object value, string prefix) {
		if (value == null) {
			return prefix + ":null";
		}

		return prefix + ":" + RuntimeHelpers.GetHashCode(value).ToString(CultureInfo.InvariantCulture);
	}

	private static DslPosition ToDslPosition(Vector3 worldPosition) {
		return new DslPosition(worldPosition.x, worldPosition.z);
	}

	private sealed class GameItemAdapter : IDslItemAdapter {
		private readonly ItemSO _item;

		public GameItemAdapter(ItemSO item) {
			_item = item;
		}

		public string Id {
			get {
				if (_item == null) {
					return "item:null";
				}

				if (!string.IsNullOrWhiteSpace(_item.ItemId)) {
					return _item.ItemId;
				}

				return CreateObjectId(_item, "item");
			}
		}

		public string DisplayName {
			get {
				if (_item == null) {
					return "Unknown Item";
				}

				return !string.IsNullOrWhiteSpace(_item.DisplayName) ? _item.DisplayName : _item.name;
			}
		}

		public ItemSO Item => _item;
	}

	private sealed class GameInventorySlotAdapter : IDslInventorySlotAdapter {
		private readonly RobotInventorySlot _slot;

		public GameInventorySlotAdapter(RobotInventorySlot slot) {
			_slot = slot;
		}

		public IDslItemAdapter Item => _slot != null && _slot.Item != null ? new GameItemAdapter(_slot.Item) : null;
		public int Quantity => _slot != null ? _slot.Quantity : 0;
		public bool IsEmpty => _slot == null || _slot.IsEmpty;
	}

	private sealed class GameRobotInventoryAdapter : IDslRobotInventoryAdapter {
		private readonly RobotInventory _inventory;

		public GameRobotInventoryAdapter(RobotInventory inventory) {
			_inventory = inventory;
		}

		public bool HasItem(IDslItemAdapter item, int quantity) {
			ItemSO engineItem = ResolveItem(item);
			return _inventory != null && engineItem != null && _inventory.HasEnough(engineItem, quantity);
		}

		public bool HasEmptySlot() {
			if (_inventory == null || _inventory.Slots == null) {
				return false;
			}

			for (int i = 0; i < _inventory.Slots.Count; i++) {
				RobotInventorySlot slot = _inventory.Slots[i];
				if (slot != null && slot.IsEmpty) {
					return true;
				}
			}

			return false;
		}

		public bool IsEmpty() {
			if (_inventory == null || _inventory.Slots == null) {
				return true;
			}

			for (int i = 0; i < _inventory.Slots.Count; i++) {
				RobotInventorySlot slot = _inventory.Slots[i];
				if (slot != null && !slot.IsEmpty) {
					return false;
				}
			}

			return true;
		}

		public int SlotCount => _inventory != null ? _inventory.SlotCount : 0;

		public int Quantity(IDslItemAdapter item) {
			ItemSO engineItem = ResolveItem(item);
			return _inventory != null && engineItem != null ? _inventory.GetQuantity(engineItem) : 0;
		}

		public IReadOnlyList<IDslInventorySlotAdapter> Slots {
			get {
				var slots = new List<IDslInventorySlotAdapter>();
				if (_inventory == null || _inventory.Slots == null) {
					return slots;
				}

				for (int i = 0; i < _inventory.Slots.Count; i++) {
					slots.Add(new GameInventorySlotAdapter(_inventory.Slots[i]));
				}

				return slots;
			}
		}

		public bool TryAddItem(IDslItemAdapter item, int quantity) {
			ItemSO engineItem = ResolveItem(item);
			return _inventory != null && engineItem != null && _inventory.TryAddItem(engineItem, quantity);
		}

		public bool TryRemoveItem(IDslItemAdapter item, int quantity) {
			ItemSO engineItem = ResolveItem(item);
			return _inventory != null && engineItem != null && _inventory.TryRemoveItem(engineItem, quantity);
		}

		private static ItemSO ResolveItem(IDslItemAdapter item) {
			GameItemAdapter gameItem = item as GameItemAdapter;
			return gameItem != null ? gameItem.Item : null;
		}
	}

	private sealed class GameRobotAdapter : IDslRobotAdapter {
		private const float NearDistance = 1.5f;
		private readonly IRobot _robot;

		public GameRobotAdapter(IRobot robot) {
			_robot = robot;
		}

		public string Id {
			get {
				Component component = _robot as Component;
				return component != null ? CreateObjectId(component, "robot") : CreateReferenceId(_robot, "robot");
			}
		}

		public DslPosition Position => ToDslPosition(_robot.Position);
		public bool IsMoving => _robot != null && _robot.IsMoving;
		public IDslRobotInventoryAdapter Inventory => new GameRobotInventoryAdapter(ResolveInventory());

		public bool IsNear(IDslEntityAdapter target) {
			if (_robot == null || target == null) {
				return false;
			}

			return Position.DistanceTo(target.Position) <= NearDistance;
		}

		public void MoveTo(DslPosition position) {
			if (_robot == null) {
				return;
			}

			_robot.StartMoveTo(new Vector3(position.X, 0f, position.Y));
		}

		private RobotInventory ResolveInventory() {
			Component component = _robot as Component;
			return component != null ? component.GetComponent<RobotInventory>() : null;
		}
	}

	private sealed class GameTableAdapter : IDslTableAdapter {
		private readonly TableBehavior _table;

		public GameTableAdapter(TableBehavior table) {
			_table = table;
		}

		public string Id => CreateObjectId(_table, "table");
		public DslPosition Position => _table != null ? ToDslPosition(_table.transform.position) : default;

		public IReadOnlyList<IDslCustomerAdapter> Customers {
			get {
				var customers = new List<IDslCustomerAdapter>();
				if (_table == null || _table.Customers == null) {
					return customers;
				}

				for (int i = 0; i < _table.Customers.Count; i++) {
					Customer customer = _table.Customers[i];
					if (customer != null) {
						customers.Add(new GameCustomerAdapter(customer));
					}
				}

				return customers;
			}
		}

		public bool IsDirty => _table != null && _table.IsDirty;

		public bool Clean() {
			return _table != null && _table.Clean();
		}
	}

	private sealed class GameCustomerAdapter : IDslCustomerAdapter {
		private readonly Customer _customer;

		public GameCustomerAdapter(Customer customer) {
			_customer = customer;
		}

		public string Id => CreateObjectId(_customer, "customer");
		public DslPosition Position => _customer != null ? ToDslPosition(_customer.transform.position) : default;
		public IDslTableAdapter Table => _customer != null && _customer.Table != null ? new GameTableAdapter(_customer.Table) : null;
		public DslCustomerState State => _customer != null ? MapCustomerState(_customer.State) : DslCustomerState.Seating;
		public bool HasActiveOrder => _customer != null && _customer.CurrentOrder != null;
		public IDslOrderAdapter CurrentOrder => _customer != null && _customer.CurrentOrder != null ? new GameOrderAdapter(_customer.CurrentOrder) : null;

		public IDslOrderAdapter TakeOrder() {
			if (_customer == null) {
				return null;
			}

			Order order = _customer.GetOrder();
			return order != null ? new GameOrderAdapter(order) : null;
		}

		private static DslCustomerState MapCustomerState(CustomerState state) {
			switch (state) {
				case CustomerState.Thinking:
					return DslCustomerState.Thinking;
				case CustomerState.Ordering:
					return DslCustomerState.Ordering;
				case CustomerState.Waiting:
					return DslCustomerState.Waiting;
				case CustomerState.Eating:
					return DslCustomerState.Eating;
				case CustomerState.Leaving:
					return DslCustomerState.Leaving;
				default:
					return DslCustomerState.Seating;
			}
		}
	}

	private sealed class GameOrderItemAdapter : IDslOrderItemAdapter {
		private readonly OrderItem _orderItem;

		public GameOrderItemAdapter(OrderItem orderItem) {
			_orderItem = orderItem;
		}

		public IDslItemAdapter Item => _orderItem != null && _orderItem.Item != null ? new GameItemAdapter(_orderItem.Item) : null;
		public int Quantity => _orderItem != null ? _orderItem.Quantity : 0;
	}

	private sealed class GameOrderAdapter : IDslOrderAdapter {
		private readonly Order _order;

		public GameOrderAdapter(Order order) {
			_order = order;
		}

		public string Id => CreateReferenceId(_order, "order");

		public IReadOnlyList<IDslOrderItemAdapter> Items {
			get {
				var items = new List<IDslOrderItemAdapter>();
				if (_order == null || _order.Items == null) {
					return items;
				}

				for (int i = 0; i < _order.Items.Count; i++) {
					OrderItem item = _order.Items[i];
					if (item != null && item.IsValid) {
						items.Add(new GameOrderItemAdapter(item));
					}
				}

				return items;
			}
		}

		public bool IsCompleted => _order != null && _order.IsCompleted;
		public IDslCustomerAdapter Customer => _order != null && _order.Customer != null ? new GameCustomerAdapter(_order.Customer) : null;
	}

	private sealed class GameFurnaceAdapter : IDslFurnaceAdapter {
		private readonly OvenBehavior _furnace;
		private readonly CurrentGameDslWorldAdapter _world;

		public GameFurnaceAdapter(OvenBehavior furnace, CurrentGameDslWorldAdapter world) {
			_furnace = furnace;
			_world = world;
		}

		public string Id => CreateObjectId(_furnace, "furnace");
		public DslPosition Position => _furnace != null ? ToDslPosition(_furnace.transform.position) : default;
		public DslFurnaceState State => MapFurnaceState(_furnace != null ? _furnace.State : OvenBehavior.OvenState.Empty);
		public bool IsReady => _furnace != null && _furnace.State == OvenBehavior.OvenState.Ready;
		public bool IsBusy => _furnace != null && (_furnace.State == OvenBehavior.OvenState.Placed || _furnace.State == OvenBehavior.OvenState.Cooking);
		public bool IsDirty => _furnace != null && _furnace.State == OvenBehavior.OvenState.BurnedDirty;

		public bool Place(IReadOnlyList<IDslItemAdapter> items) {
			RobotInventory inventory = _world.GetCurrentRobotInventory();
			if (_furnace == null || inventory == null) {
				return false;
			}

			if (items == null || items.Count == 0) {
				return _furnace.Place(inventory);
			}

			var engineItems = new List<ItemSO>();
			for (int i = 0; i < items.Count; i++) {
				ItemSO item = ResolveItem(items[i]);
				if (item != null) {
					engineItems.Add(item);
				}
			}

			return engineItems.Count > 0 && _furnace.Place(inventory, engineItems);
		}

		public bool Cook() {
			if (_furnace == null) {
				return false;
			}

			_furnace.Cook();
			return _furnace.State == OvenBehavior.OvenState.Cooking || _furnace.State == OvenBehavior.OvenState.Ready;
		}

		public IReadOnlyList<IDslItemAdapter> Take() {
			var takenItems = new List<IDslItemAdapter>();
			RobotInventory inventory = _world.GetCurrentRobotInventory();
			if (_furnace == null || inventory == null) {
				return takenItems;
			}

			IReadOnlyList<RecipeItemStack> outputs = _furnace.State == OvenBehavior.OvenState.Ready
				? _furnace.ActiveRecipe != null ? _furnace.ActiveRecipe.OutputItems : null
				: _furnace.State == OvenBehavior.OvenState.BurnedDirty && _furnace.ActiveRecipe != null ? _furnace.ActiveRecipe.BurnedOutputItems : null;
			if (!_furnace.Take(inventory) || outputs == null) {
				return takenItems;
			}

			for (int i = 0; i < outputs.Count; i++) {
				RecipeItemStack entry = outputs[i];
				if (entry == null || !entry.IsValid) {
					continue;
				}

				for (int quantityIndex = 0; quantityIndex < entry.Quantity; quantityIndex++) {
					takenItems.Add(new GameItemAdapter(entry.Item));
				}
			}

			return takenItems;
		}

		public bool Clean() {
			if (_furnace == null) {
				return false;
			}

			bool wasDirty = IsDirty;
			_furnace.Clean();
			return wasDirty && !IsDirty;
		}

		private static DslFurnaceState MapFurnaceState(OvenBehavior.OvenState state) {
			switch (state) {
				case OvenBehavior.OvenState.Cooking:
					return DslFurnaceState.Cooking;
				case OvenBehavior.OvenState.Ready:
					return DslFurnaceState.Ready;
				case OvenBehavior.OvenState.BurnedDirty:
					return DslFurnaceState.Burnt;
				case OvenBehavior.OvenState.Placed:
					return DslFurnaceState.Cooking;
				default:
					return DslFurnaceState.Empty;
			}
		}
	}

	private sealed class GameFridgeAdapter : IDslFridgeAdapter {
		private readonly FridgeBehavior _fridge;
		private readonly CurrentGameDslWorldAdapter _world;

		public GameFridgeAdapter(FridgeBehavior fridge, CurrentGameDslWorldAdapter world) {
			_fridge = fridge;
			_world = world;
		}

		public string Id => CreateObjectId(_fridge, "fridge");
		public DslPosition Position => _fridge != null ? ToDslPosition(_fridge.transform.position) : default;

		public bool HasItem(IDslItemAdapter item, int quantity) {
			ItemSO engineItem = ResolveItem(item);
			return _fridge != null && engineItem != null && _fridge.HasEnough(engineItem, quantity);
		}

		public int Quantity(IDslItemAdapter item) {
			ItemSO engineItem = ResolveItem(item);
			return _fridge != null && engineItem != null ? _fridge.GetQuantity(engineItem) : 0;
		}

		public bool Take(IDslItemAdapter item, int quantity) {
			ItemSO engineItem = ResolveItem(item);
			RobotInventory inventory = _world.GetCurrentRobotInventory();
			return _fridge != null && engineItem != null && inventory != null && _fridge.TryTake(engineItem, quantity, inventory);
		}

		public bool Place(IDslItemAdapter item, int quantity) {
			if (_fridge == null || _fridge.Inventory == null || _fridge.Inventory.InventoryManager == null) {
				return false;
			}

			ItemSO engineItem = ResolveItem(item);
			RobotInventory inventory = _world.GetCurrentRobotInventory();
			if (engineItem == null || inventory == null) {
				return false;
			}

			if (!inventory.TryRemoveItem(engineItem, quantity)) {
				return false;
			}

			if (_fridge.Inventory.InventoryManager.TryAddItem(engineItem, quantity)) {
				return true;
			}

			inventory.TryAddItem(engineItem, quantity);
			return false;
		}
	}

	private static ItemSO ResolveItem(IDslItemAdapter item) {
		GameItemAdapter gameItem = item as GameItemAdapter;
		return gameItem != null ? gameItem.Item : null;
	}
}
