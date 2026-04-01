public class DslRuntimeError : ValidationError {
	public DslRuntimeError(string message, int line = -1)
		: base(message, line) {
	}
}
