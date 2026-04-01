using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public static class DslArgumentReader {
	public static DslPosition ReadPosition(object value, string argumentName, int line = -1) {
		if (value is DslPosition position) {
			return position;
		}

		if (value is IDslEntityFacade entityFacade) {
			return entityFacade.EntityAdapter.Position;
		}

		if (value is IList list && list.Count >= 2) {
			return new DslPosition(
				ReadFloat(list[0], argumentName + "[0]", line),
				ReadFloat(list[1], argumentName + "[1]", line));
		}

		throw new DslRuntimeError($"Argument '{argumentName}' must be a position value like [x, y] (line {line})", line);
	}

	public static IDslEntityAdapter ReadEntityAdapter(object value, string argumentName, int line = -1) {
		if (value is IDslEntityFacade entityFacade) {
			return entityFacade.EntityAdapter;
		}

		throw new DslRuntimeError($"Argument '{argumentName}' must be a restaurant object or customer (line {line})", line);
	}

	public static IDslItemAdapter ReadItemAdapter(object value, string argumentName, int line = -1) {
		if (value is IDslItemFacade itemFacade) {
			return itemFacade.ItemAdapter;
		}

		if (value is IDslOrderItemFacadeAccessor orderItemFacade) {
			return orderItemFacade.OrderItemAdapter.Item;
		}

		throw new DslRuntimeError($"Argument '{argumentName}' must be an item-compatible DSL value (line {line})", line);
	}

	public static IReadOnlyList<IDslItemAdapter> ReadItemAdapters(object value, string argumentName, int line = -1) {
		var items = new List<IDslItemAdapter>();
		AppendItems(items, value, argumentName, line);
		return items;
	}

	public static int ReadPositiveInteger(object value, string argumentName, int line = -1) {
		int integer = ReadInteger(value, argumentName, line);
		if (integer <= 0) {
			throw new DslRuntimeError($"Argument '{argumentName}' must be a positive integer (line {line})", line);
		}

		return integer;
	}

	public static int ReadInteger(object value, string argumentName, int line = -1) {
		if (value is int intValue) {
			return intValue;
		}

		if (value is float floatValue) {
			int rounded = (int)floatValue;
			if (Math.Abs(floatValue - rounded) < 0.0001f) {
				return rounded;
			}
		}

		if (value is string stringValue && int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) {
			return parsed;
		}

		throw new DslRuntimeError($"Argument '{argumentName}' must be an integer (line {line})", line);
	}

	public static float ReadFloat(object value, string argumentName, int line = -1) {
		if (value is float floatValue) {
			return floatValue;
		}

		if (value is int intValue) {
			return intValue;
		}

		if (value is string stringValue && float.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed)) {
			return parsed;
		}

		throw new DslRuntimeError($"Argument '{argumentName}' must be a number (line {line})", line);
	}

	private static void AppendItems(List<IDslItemAdapter> items, object value, string argumentName, int line) {
		if (value == null) {
			throw new DslRuntimeError($"Argument '{argumentName}' cannot be null (line {line})", line);
		}

		if (value is string) {
			throw new DslRuntimeError($"Argument '{argumentName}' must be an item or item list, not a string (line {line})", line);
		}

		if (value is IEnumerable enumerable) {
			foreach (object entry in enumerable) {
				AppendItems(items, entry, argumentName, line);
			}
			return;
		}

		if (value is IDslOrderItemFacadeAccessor orderItemFacade) {
			for (int i = 0; i < orderItemFacade.OrderItemAdapter.Quantity; i++) {
				items.Add(orderItemFacade.OrderItemAdapter.Item);
			}
			return;
		}

		if (value is IDslItemFacade itemFacade) {
			items.Add(itemFacade.ItemAdapter);
			return;
		}

		throw new DslRuntimeError($"Argument '{argumentName}' contains a value that is not item-compatible (line {line})", line);
	}
}
