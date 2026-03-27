using System.Collections;
using System.Collections.Generic;

public static class BuiltinClassRegistry {
	public static object UnwrapValue(object value) {
		if (value is BuiltinObject builtin) {
			return builtin.RawInstance;
		}

		return value;
	}

	public static object WrapValue(object value, ScriptInvocationContext context) {
		if (value == null) {
			return null;
		}

		if (value is BuiltinObject || value is string || value is bool || value is float || value is int) {
			return value;
		}

		if (TryCreate(value, context, out BuiltinObject builtin)) {
			return builtin;
		}

		if (value is IEnumerable enumerable) {
			return WrapEnumerable(enumerable, context);
		}

		return value;
	}

	public static bool TryGetMember(object instance, string memberName, ScriptInvocationContext context, out object result) {
		result = null;
		if (!TryCreate(instance, context, out BuiltinObject builtin)) {
			return false;
		}

		return builtin.TryGetMember(memberName, out result);
	}

	public static bool TryInvoke(object instance, string memberName, object[] args, ScriptInvocationContext context, int line, out object result) {
		result = null;
		if (!TryCreate(instance, context, out BuiltinObject builtin)) {
			return false;
		}

		return builtin.TryInvoke(memberName, args, line, out result);
	}

	public static string GetTypeName(object value) {
		object wrapped = WrapValue(value, ScriptInvocationContext.Create(null));
		if (wrapped is BuiltinObject builtin) {
			return builtin.ClassName;
		}

		return null;
	}

	private static List<object> WrapEnumerable(IEnumerable values, ScriptInvocationContext context) {
		var wrapped = new List<object>();
		foreach (object value in values) {
			wrapped.Add(WrapValue(value, context));
		}

		return wrapped;
	}

	private static bool TryCreate(object instance, ScriptInvocationContext context, out BuiltinObject builtin) {
		builtin = instance as BuiltinObject;
		if (builtin != null) {
			return true;
		}

		switch (instance) {
			case TableBehavior table:
				builtin = new TableBuiltinClass(table, context);
				return true;
			case FridgeBehavior fridgeBehavior:
				builtin = new FridgeBuiltinClass(fridgeBehavior, context);
				return true;
			case Customer customer:
				builtin = new CustomerBuiltinClass(customer, context);
				return true;
			case Order order:
				builtin = new OrderBuiltinClass(order, context);
				return true;
			case OrderItem orderItem:
				builtin = new OrderItemBuiltinClass(orderItem, context);
				return true;
			case OvenBehavior oven:
				builtin = new FurnaceBuiltinClass(oven, context);
				return true;
			case StorageInventory fridge:
				FridgeBehavior attachedFridge = null;
				if (fridge != null) {
					attachedFridge = fridge.GetComponent<FridgeBehavior>();
				}

				if (attachedFridge == null) {
					builtin = null;
					return false;
				}

				builtin = new FridgeBuiltinClass(attachedFridge, context);
				return true;
			case IRobot robot:
				builtin = new RobotBuiltinClass(robot, context);
				return true;
			case RobotInventorySlot slot:
				builtin = new RobotInventorySlotBuiltinClass(slot, context);
				return true;
			default:
				builtin = null;
				return false;
		}
	}
}
