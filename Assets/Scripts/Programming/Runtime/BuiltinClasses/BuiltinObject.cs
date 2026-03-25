using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuiltinObject {
	protected BuiltinObject(object rawInstance, ScriptInvocationContext context) {
		RawInstance = rawInstance;
		Context = context;
	}

	public object RawInstance { get; }

	public abstract string ClassName { get; }

	protected ScriptInvocationContext Context { get; }

	public virtual bool TryGetMember(string memberName, out object result) {
		result = null;
		return false;
	}

	public virtual bool TryInvoke(string memberName, object[] args, int line, out object result) {
		result = null;
		return false;
	}

	protected object Wrap(object value) {
		return BuiltinClassRegistry.WrapValue(value, Context);
	}

	protected List<object> WrapEnumerable(IEnumerable values) {
		var wrapped = new List<object>();
		if (values == null) {
			return wrapped;
		}

		foreach (object value in values) {
			wrapped.Add(Wrap(value));
		}

		return wrapped;
	}

	protected static void EnsureNoArguments(string memberName, object[] args, int line) {
		if (args != null && args.Length != 0) {
			throw new ValidationError($"{memberName}() takes no arguments (line {line})", line);
		}
	}

	protected static object Unwrap(object value) {
		return BuiltinClassRegistry.UnwrapValue(value);
	}

	protected static int ReadQuantityArgument(object value, string memberName, int line) {
		object rawValue = Unwrap(value);
		if (rawValue is int quantityInt) {
			if (quantityInt > 0) {
				return quantityInt;
			}
		} else if (rawValue is float quantityFloat) {
			int quantity = Mathf.RoundToInt(quantityFloat);
			if (quantity > 0 && Mathf.Approximately(quantityFloat, quantity)) {
				return quantity;
			}
		}

		throw new ValidationError($"{memberName}() quantity argument must be a positive integer (line {line})", line);
	}

	protected static ItemSO ReadItemArgument(object value, string memberName, int line) {
		object rawValue = Unwrap(value);
		if (rawValue is ItemSO item) {
			return item;
		}

		if (rawValue is OrderItem orderItem && orderItem.Item != null) {
			return orderItem.Item;
		}

		if (rawValue is InventoryItemEntry inventoryEntry && inventoryEntry.Item != null) {
			return inventoryEntry.Item;
		}

		if (rawValue is RobotInventorySlot slot && slot.Item != null) {
			return slot.Item;
		}

		throw new ValidationError($"{memberName}() item argument must be an ItemSO-compatible value (line {line})", line);
	}

	protected static List<ItemSO> ReadItemArguments(object[] args, string memberName, int line) {
		var items = new List<ItemSO>();
		if (args == null || args.Length == 0) {
			return items;
		}

		for (int i = 0; i < args.Length; i++) {
			AppendItemArgument(items, args[i], memberName, line);
		}

		return items;
	}

	private static void AppendItemArgument(List<ItemSO> items, object value, string memberName, int line) {
		object rawValue = Unwrap(value);
		if (rawValue is IEnumerable enumerable && rawValue is not string) {
			foreach (object entry in enumerable) {
				AppendItemArgument(items, entry, memberName, line);
			}
			return;
		}

		if (rawValue is OrderItem orderItem && orderItem.Item != null && orderItem.Quantity > 0) {
			for (int i = 0; i < orderItem.Quantity; i++) {
				items.Add(orderItem.Item);
			}
			return;
		}

		if (rawValue is InventoryItemEntry inventoryEntry && inventoryEntry.Item != null && inventoryEntry.Quantity > 0) {
			for (int i = 0; i < inventoryEntry.Quantity; i++) {
				items.Add(inventoryEntry.Item);
			}
			return;
		}

		if (rawValue is RobotInventorySlot slot && slot.Item != null && slot.Quantity > 0) {
			for (int i = 0; i < slot.Quantity; i++) {
				items.Add(slot.Item);
			}
			return;
		}

		items.Add(ReadItemArgument(rawValue, memberName, line));
	}

	public override string ToString() {
		if (RawInstance is Object unityObject && unityObject != null) {
			return $"{ClassName}({unityObject.name})";
		}

		return ClassName;
	}

	public override bool Equals(object obj) {
		if (obj is BuiltinObject otherBuiltin) {
			return Equals(RawInstance, otherBuiltin.RawInstance);
		}

		return Equals(RawInstance, obj);
	}

	public override int GetHashCode() {
		return RawInstance != null ? RawInstance.GetHashCode() : 0;
	}
}
