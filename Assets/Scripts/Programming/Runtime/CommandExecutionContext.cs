public static class CommandExecutionContext {
	public static event System.Action<string, string, bool> StatusMessagePublished;

	// Context scoping: support multiple interpreters (contexts) running concurrently.
	// CurrentContextId selects which variable set and robot the static accessors operate on.
	public static string CurrentContextId { get; set; } = "default";

	public static int CurrentLine {
		get => GetIntForCurrentContext(_currentLineKey);
		set => SetIntForCurrentContext(_currentLineKey, value);
	}

	// Current robot executing enqueued commands for the active context
	public static IRobot CurrentRobot {
		get => GetObjectForCurrentContext<IRobot>(_currentRobotKey);
		set => SetObjectForCurrentContext(_currentRobotKey, value);
	}

	private const string _varsKey = "__vars";
	private const string _currentLineKey = "__currentLine";
	private const string _currentRobotKey = "__currentRobot";

	// contexts -> (key -> object)
	private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>> _contexts
		= new(System.StringComparer.OrdinalIgnoreCase);

	private static System.Collections.Generic.Dictionary<string, object> EnsureContext(string id) {
		if (!_contexts.TryGetValue(id, out var ctx)) {
			ctx = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase) {
				// initialize defaults
				[_varsKey] = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase),
				[_currentLineKey] = -1,
				[_currentRobotKey] = null
			};
			_contexts[id] = ctx;
		}
		return ctx;
	}

	private static System.Collections.Generic.Dictionary<string, object> VarsForCurrent() {
		var ctx = EnsureContext(CurrentContextId);
		return (System.Collections.Generic.Dictionary<string, object>)ctx[_varsKey];
	}

	public static void SetVariable(string name, object value) {
		var vars = VarsForCurrent();
		vars[name] = value;
	}

	public static bool RemoveVariable(string name) {
		var vars = VarsForCurrent();
		return vars.Remove(name);
	}

	public static bool TryGetVariable(string name, out object value) {
		var vars = VarsForCurrent();
		return vars.TryGetValue(name, out value);
	}

	public static object GetVariable(string name, object defaultValue = null) {
		var vars = VarsForCurrent();
		return vars.TryGetValue(name, out var v) ? v : defaultValue;
	}

	public static void ClearVariables(string contextId = null) {
		if (string.IsNullOrEmpty(contextId)) {
			_contexts.Clear();
		} else {
			_contexts.Remove(contextId);
		}
	}

	public static void PublishStatusMessage(string message, bool isError = false) {
		if (string.IsNullOrWhiteSpace(message)) {
			return;
		}

		StatusMessagePublished?.Invoke(CurrentContextId, message, isError);
	}

	private static int GetIntForCurrentContext(string key) {
		var ctx = EnsureContext(CurrentContextId);
		if (ctx.TryGetValue(key, out var v) && v is int vi) return vi;
		return -1;
	}

	private static void SetIntForCurrentContext(string key, int value) {
		var ctx = EnsureContext(CurrentContextId);
		ctx[key] = value;
	}

	private static T GetObjectForCurrentContext<T>(string key) where T : class {
		var ctx = EnsureContext(CurrentContextId);
		if (ctx.TryGetValue(key, out var v)) return v as T;
		return null;
	}

	private static void SetObjectForCurrentContext<T>(string key, T value) where T : class {
		var ctx = EnsureContext(CurrentContextId);
		ctx[key] = value;
	}
}
