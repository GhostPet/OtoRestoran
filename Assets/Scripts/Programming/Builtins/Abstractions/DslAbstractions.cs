using System;
using System.Collections.Generic;

public enum DslCustomerState {
	Seating,
	Thinking,
	Ordering,
	Waiting,
	Eating,
	Leaving
}

public enum DslFurnaceState {
	Empty,
	Cooking,
	Ready,
	Burnt
}

public readonly struct DslPosition : IEquatable<DslPosition> {
	public DslPosition(float x, float y) {
		X = x;
		Y = y;
	}

	public float X { get; }
	public float Y { get; }

	public float DistanceTo(DslPosition other) {
		float dx = X - other.X;
		float dy = Y - other.Y;
		return (float)Math.Sqrt((dx * dx) + (dy * dy));
	}

	public List<object> ToDslList() {
		return new List<object> { X, Y };
	}

	public bool Equals(DslPosition other) {
		return X.Equals(other.X) && Y.Equals(other.Y);
	}

	public override bool Equals(object obj) {
		return obj is DslPosition other && Equals(other);
	}

	public override int GetHashCode() {
		unchecked {
			return (X.GetHashCode() * 397) ^ Y.GetHashCode();
		}
	}

	public override string ToString() {
		return $"({X}, {Y})";
	}
}

public interface IDslEntityAdapter {
	string Id { get; }
	DslPosition Position { get; }
}

public interface IDslRestaurantObjectAdapter : IDslEntityAdapter {
}

public interface IDslItemAdapter {
	string Id { get; }
	string DisplayName { get; }
}

public interface IDslInventorySlotAdapter {
	IDslItemAdapter Item { get; }
	int Quantity { get; }
	bool IsEmpty { get; }
}

public interface IDslRobotInventoryAdapter {
	bool HasItem(IDslItemAdapter item, int quantity);
	bool HasEmptySlot();
	bool IsEmpty();
	int SlotCount { get; }
	int Quantity(IDslItemAdapter item);
	IReadOnlyList<IDslInventorySlotAdapter> Slots { get; }
	bool TryAddItem(IDslItemAdapter item, int quantity);
	bool TryRemoveItem(IDslItemAdapter item, int quantity);
}

public interface IDslRobotAdapter : IDslRestaurantObjectAdapter {
	bool IsMoving { get; }
	bool IsNear(IDslEntityAdapter target);
	void MoveTo(DslPosition position);
	IDslRobotInventoryAdapter Inventory { get; }
}

public interface IDslTableAdapter : IDslRestaurantObjectAdapter {
	IReadOnlyList<IDslCustomerAdapter> Customers { get; }
	bool IsDirty { get; }
	bool Clean();
}

public interface IDslCustomerAdapter : IDslEntityAdapter {
	IDslTableAdapter Table { get; }
	DslCustomerState State { get; }
	bool HasActiveOrder { get; }
	IDslOrderAdapter TakeOrder();
	IDslOrderAdapter CurrentOrder { get; }
}

public interface IDslOrderItemAdapter {
	IDslItemAdapter Item { get; }
	int Quantity { get; }
}

public interface IDslOrderAdapter {
	string Id { get; }
	IReadOnlyList<IDslOrderItemAdapter> Items { get; }
	bool IsCompleted { get; }
	IDslCustomerAdapter Customer { get; }
}

public interface IDslFurnaceAdapter : IDslRestaurantObjectAdapter {
	DslFurnaceState State { get; }
	bool IsReady { get; }
	bool IsBusy { get; }
	bool IsDirty { get; }
	bool Place(IReadOnlyList<IDslItemAdapter> items);
	bool Cook();
	IReadOnlyList<IDslItemAdapter> Take();
	bool Clean();
}

public interface IDslFridgeAdapter : IDslRestaurantObjectAdapter {
	bool HasItem(IDslItemAdapter item, int quantity);
	int Quantity(IDslItemAdapter item);
	bool Take(IDslItemAdapter item, int quantity);
	bool Place(IDslItemAdapter item, int quantity);
}

public interface IDslTrashcanAdapter : IDslRestaurantObjectAdapter {
	bool Throw(IReadOnlyList<IDslItemAdapter> items);
}

public interface IDslWorldAdapter {
	IDslRobotAdapter GetCurrentRobot();
	IReadOnlyList<IDslRobotAdapter> GetRobots();
	IReadOnlyList<IDslTableAdapter> GetTables();
	IReadOnlyList<IDslFurnaceAdapter> GetFurnaces();
	IReadOnlyList<IDslFridgeAdapter> GetFridges();
	IReadOnlyList<IDslTrashcanAdapter> GetTrashcans();
	IReadOnlyList<IDslOrderAdapter> GetOrders();
   IDslItemAdapter FindItem(string itemName);
}
