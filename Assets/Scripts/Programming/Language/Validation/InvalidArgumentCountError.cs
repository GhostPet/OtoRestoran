public class InvalidArgumentCountError : ValidationError {
	public InvalidArgumentCountError(string functionName, int expected, int actual, int line)
		: base($"Function '{functionName}' expects {expected} arguments, got {actual}", line) {
	}
}
