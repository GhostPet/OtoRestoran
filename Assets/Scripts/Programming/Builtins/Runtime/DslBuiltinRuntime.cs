using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public interface IDslOutputSink {
	void Write(string message, bool isError = false);
}

public interface IDslAsyncOperation {
	bool Tick(float deltaTime);
}

public readonly struct DslBuiltinInvocation {
	public DslBuiltinInvocation(object value, IDslAsyncOperation asyncOperation) {
		Value = value;
		AsyncOperation = asyncOperation;
	}

	public object Value { get; }
	public IDslAsyncOperation AsyncOperation { get; }
	public bool IsAsync => AsyncOperation != null;

	public static DslBuiltinInvocation FromValue(object value) {
		return new DslBuiltinInvocation(value, null);
	}

	public static DslBuiltinInvocation FromAsync(IDslAsyncOperation asyncOperation, object value = null) {
		return new DslBuiltinInvocation(value, asyncOperation);
	}
}

public sealed class DslBuiltinRuntime {
	private readonly Dictionary<string, Func<object[], int, DslBuiltinInvocation>> _functions;
	private readonly IDslWorldAdapter _world;
	private readonly IDslOutputSink _output;
	private readonly DslFacadeFactory _factory;
	private readonly CustomerStateScope _customerState = new CustomerStateScope();
	private readonly FurnaceStateScope _furnaceState = new FurnaceStateScope();

	public DslBuiltinRuntime(IDslWorldAdapter world, IDslOutputSink output, DslFacadeFactory factory) {
		_world = world ?? throw new ArgumentNullException(nameof(world));
		_output = output ?? throw new ArgumentNullException(nameof(output));
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		_functions = new Dictionary<string, Func<object[], int, DslBuiltinInvocation>>(StringComparer.OrdinalIgnoreCase) {
			["print"] = InvokePrint,
			["find_item"] = InvokeFindItem,
			["range"] = InvokeRange,
			["len"] = InvokeLen,
			["wait"] = InvokeWait,
			["get_robot"] = InvokeGetRobot,
			["get_tables"] = InvokeGetTables,
			["get_furnaces"] = InvokeGetFurnaces,
			["get_fridges"] = InvokeGetFridges,
			["get_trashcans"] = InvokeGetTrashcans,
			["get_orders"] = InvokeGetOrders,
			["get_nearest_robot"] = InvokeGetNearestRobot,
			["get_nearest_table"] = InvokeGetNearestTable,
			["get_nearest_furnace"] = InvokeGetNearestFurnace,
			["get_nearest_fridge"] = InvokeGetNearestFridge,
			["get_nearest_trashcan"] = InvokeGetNearestTrashcan
		};
	}

	public IReadOnlyDictionary<string, object> CreateGlobalScope() {
		return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
			["CustomerState"] = _customerState,
			["FurnaceState"] = _furnaceState
		};
	}

	public IEnumerable<string> GetBuiltinNames() {
		return _functions.Keys;
	}

	public bool IsBuiltin(string name) {
		return !string.IsNullOrWhiteSpace(name) && _functions.ContainsKey(name);
	}

	public DslBuiltinInvocation Invoke(string name, IList<object> arguments, int line) {
		if (string.IsNullOrWhiteSpace(name) || !_functions.TryGetValue(name, out Func<object[], int, DslBuiltinInvocation> handler)) {
			throw new DslRuntimeError($"Unknown builtin '{name}' (line {line})", line);
		}

		object[] runtimeArguments = arguments != null ? new List<object>(arguments).ToArray() : Array.Empty<object>();
		return handler(runtimeArguments, line);
	}

	private DslBuiltinInvocation InvokePrint(object[] args, int line) {
		var parts = new List<string>(args.Length);
		for (int i = 0; i < args.Length; i++) {
			parts.Add(FormatValue(args[i]));
		}

		_output.Write(string.Join(" ", parts), false);
		return DslBuiltinInvocation.FromValue(null);
	}

	private DslBuiltinInvocation InvokeRange(object[] args, int line) {
		EnsureArgumentCount("range", args, 1, 3, line);
		int start = 0;
		int stop;
		int step = 1;
		if (args.Length == 1) {
			stop = DslArgumentReader.ReadInteger(args[0], "stop", line);
		} else {
			start = DslArgumentReader.ReadInteger(args[0], "start", line);
			stop = DslArgumentReader.ReadInteger(args[1], "stop", line);
			if (args.Length == 3) {
				step = DslArgumentReader.ReadInteger(args[2], "step", line);
			}
		}

		if (step == 0) {
			throw new DslRuntimeError($"range() step cannot be zero (line {line})", line);
		}

		var values = new List<object>();
		if (step > 0) {
			for (int i = start; i < stop; i += step) {
				values.Add((float)i);
			}
		} else {
			for (int i = start; i > stop; i += step) {
				values.Add((float)i);
			}
		}

		return DslBuiltinInvocation.FromValue(values);
	}

	private DslBuiltinInvocation InvokeLen(object[] args, int line) {
		EnsureArgumentCount("len", args, 1, 1, line);
		object value = args[0];
		if (value is string stringValue) {
			return DslBuiltinInvocation.FromValue((float)stringValue.Length);
		}

		if (value is ICollection collection) {
			return DslBuiltinInvocation.FromValue((float)collection.Count);
		}

		if (value is IEnumerable enumerable) {
			int count = 0;
			foreach (object _ in enumerable) {
				count++;
			}
			return DslBuiltinInvocation.FromValue((float)count);
		}

		throw new DslRuntimeError($"len() expects a list-like value or string (line {line})", line);
	}

	private DslBuiltinInvocation InvokeWait(object[] args, int line) {
		EnsureArgumentCount("wait", args, 1, 1, line);
		float seconds = DslArgumentReader.ReadFloat(args[0], "seconds", line);
		if (seconds < 0f) {
			throw new DslRuntimeError($"wait() seconds must be non-negative (line {line})", line);
		}

		return DslBuiltinInvocation.FromAsync(new DslWaitOperation(seconds));
	}

	private DslBuiltinInvocation InvokeFindItem(object[] args, int line) {
		EnsureArgumentCount("find_item", args, 1, 1, line);
		string itemName = ReadRequiredString(args[0], "itemName", line);
		return DslBuiltinInvocation.FromValue(_factory.WrapItem(_world.FindItem(itemName)));
	}

	private DslBuiltinInvocation InvokeGetRobot(object[] args, int line) {
		EnsureArgumentCount("get_robot", args, 0, 0, line);
		IDslRobotAdapter robot = RequireCurrentRobot(line);
		return DslBuiltinInvocation.FromValue(_factory.WrapRobot(robot));
	}

	private DslBuiltinInvocation InvokeGetTables(object[] args, int line) {
		EnsureArgumentCount("get_tables", args, 0, 0, line);
		return DslBuiltinInvocation.FromValue(_factory.WrapTables(_world.GetTables()));
	}

	private DslBuiltinInvocation InvokeGetFurnaces(object[] args, int line) {
		EnsureArgumentCount("get_furnaces", args, 0, 0, line);
		return DslBuiltinInvocation.FromValue(_factory.WrapFurnaces(_world.GetFurnaces()));
	}

	private DslBuiltinInvocation InvokeGetFridges(object[] args, int line) {
		EnsureArgumentCount("get_fridges", args, 0, 0, line);
		return DslBuiltinInvocation.FromValue(_factory.WrapFridges(_world.GetFridges()));
	}

	private DslBuiltinInvocation InvokeGetTrashcans(object[] args, int line) {
		EnsureArgumentCount("get_trashcans", args, 0, 0, line);
		return DslBuiltinInvocation.FromValue(_factory.WrapTrashcans(_world.GetTrashcans()));
	}

	private DslBuiltinInvocation InvokeGetOrders(object[] args, int line) {
		EnsureArgumentCount("get_orders", args, 0, 0, line);
		return DslBuiltinInvocation.FromValue(_factory.WrapOrders(_world.GetOrders()));
	}

	private DslBuiltinInvocation InvokeGetNearestRobot(object[] args, int line) {
		EnsureArgumentCount("get_nearest_robot", args, 0, 0, line);
		IDslRobotAdapter currentRobot = RequireCurrentRobot(line);
		IDslRobotAdapter nearest = FindNearest(_world.GetRobots(), currentRobot.Position, currentRobot);
		return DslBuiltinInvocation.FromValue(_factory.WrapRobot(nearest));
	}

	private DslBuiltinInvocation InvokeGetNearestTable(object[] args, int line) {
		EnsureArgumentCount("get_nearest_table", args, 0, 0, line);
		IDslTableAdapter nearest = FindNearest(_world.GetTables(), RequireCurrentRobot(line).Position, null);
		return DslBuiltinInvocation.FromValue(_factory.WrapTable(nearest));
	}

	private DslBuiltinInvocation InvokeGetNearestFurnace(object[] args, int line) {
		EnsureArgumentCount("get_nearest_furnace", args, 0, 0, line);
		IDslFurnaceAdapter nearest = FindNearest(_world.GetFurnaces(), RequireCurrentRobot(line).Position, null);
		return DslBuiltinInvocation.FromValue(_factory.WrapFurnace(nearest));
	}

	private DslBuiltinInvocation InvokeGetNearestFridge(object[] args, int line) {
		EnsureArgumentCount("get_nearest_fridge", args, 0, 0, line);
		IDslFridgeAdapter nearest = FindNearest(_world.GetFridges(), RequireCurrentRobot(line).Position, null);
		return DslBuiltinInvocation.FromValue(_factory.WrapFridge(nearest));
	}

	private DslBuiltinInvocation InvokeGetNearestTrashcan(object[] args, int line) {
		EnsureArgumentCount("get_nearest_trashcan", args, 0, 0, line);
		IDslTrashcanAdapter nearest = FindNearest(_world.GetTrashcans(), RequireCurrentRobot(line).Position, null);
		return DslBuiltinInvocation.FromValue(_factory.WrapTrashcan(nearest));
	}

	private IDslRobotAdapter RequireCurrentRobot(int line) {
		IDslRobotAdapter robot = _world.GetCurrentRobot();
		if (robot == null) {
			throw new DslRuntimeError($"No active robot is available for this script (line {line})", line);
		}

		return robot;
	}

	private static TAdapter FindNearest<TAdapter>(IReadOnlyList<TAdapter> adapters, DslPosition origin, IDslEntityAdapter exclude) where TAdapter : class, IDslEntityAdapter {
		if (adapters == null || adapters.Count == 0) {
			return null;
		}

		TAdapter nearest = null;
		float bestDistance = float.MaxValue;
		for (int i = 0; i < adapters.Count; i++) {
			TAdapter candidate = adapters[i];
			if (candidate == null) {
				continue;
			}

			if (exclude != null && string.Equals(candidate.Id, exclude.Id, StringComparison.Ordinal)) {
				continue;
			}

			float distance = origin.DistanceTo(candidate.Position);
			if (distance < bestDistance) {
				bestDistance = distance;
				nearest = candidate;
			}
		}

		return nearest;
	}

	private static string ReadRequiredString(object value, string argumentName, int line) {
		if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue)) {
			return stringValue;
		}

		throw new DslRuntimeError($"{argumentName} must be a non-empty string (line {line})", line);
	}
	private static void EnsureArgumentCount(string functionName, object[] args, int minCount, int maxCount, int line) {
		int count = args != null ? args.Length : 0;
		if (count < minCount || count > maxCount) {
			string expected = minCount == maxCount ? minCount.ToString(CultureInfo.InvariantCulture) : minCount + "-" + maxCount;
			throw new DslRuntimeError($"{functionName}() expects {expected} argument(s) but got {count} (line {line})", line);
		}
	}

	private static string FormatValue(object value) {
		if (value == null) {
			return "null";
		}

		if (value is string stringValue) {
			return stringValue;
		}

		if (value is IEnumerable enumerable && value is not string) {
			StringBuilder builder = new StringBuilder();
			builder.Append('[');
			bool first = true;
			foreach (object entry in enumerable) {
				if (!first) {
					builder.Append(", ");
				}
				builder.Append(FormatValue(entry));
				first = false;
			}
			builder.Append(']');
			return builder.ToString();
		}

		return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString();
	}

	private sealed class DslWaitOperation : IDslAsyncOperation {
		private float _remainingSeconds;

		public DslWaitOperation(float seconds) {
			_remainingSeconds = seconds;
		}

		public bool Tick(float deltaTime) {
			if (_remainingSeconds <= 0f) {
				return true;
			}

			_remainingSeconds -= Math.Max(0f, deltaTime);
			return _remainingSeconds <= 0f;
		}
	}
}

public sealed class CustomerStateScope {
	public string Seating => DslCustomerState.Seating.ToString();
	public string Thinking => DslCustomerState.Thinking.ToString();
	public string Ordering => DslCustomerState.Ordering.ToString();
	public string Waiting => DslCustomerState.Waiting.ToString();
	public string Eating => DslCustomerState.Eating.ToString();
	public string Leaving => DslCustomerState.Leaving.ToString();
}

public sealed class FurnaceStateScope {
	public string Empty => DslFurnaceState.Empty.ToString();
	public string Cooking => DslFurnaceState.Cooking.ToString();
	public string Ready => DslFurnaceState.Ready.ToString();
	public string Burnt => DslFurnaceState.Burnt.ToString();
}
