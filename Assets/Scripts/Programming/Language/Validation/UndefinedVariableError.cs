public class UndefinedVariableError : ValidationError {
	public UndefinedVariableError(string variableName, int line)
		: base($"Undefined variable '{variableName}'", line) {
	}
}
