public static class CommandExecutionContext {
	public static int CurrentLine { get; set; } = -1;
	// Current robot executing enqueued commands (set by RobotExecutor during Tick)
	public static IRobot CurrentRobot { get; set; }

	private static readonly System.Collections.Generic.Dictionary<string, object> _variables =
		new(System.StringComparer.OrdinalIgnoreCase);

	public static void SetVariable(string name, object value) {
		_variables[name] = value;
	}

	public static bool TryGetVariable(string name, out object value) {
		return _variables.TryGetValue(name, out value);
	}

	public static object GetVariable(string name, object defaultValue = null) {
		return _variables.TryGetValue(name, out var v) ? v : defaultValue;
	}

	public static void ClearVariables() {
		_variables.Clear();
	}
}
