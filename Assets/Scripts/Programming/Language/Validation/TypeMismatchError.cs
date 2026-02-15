public class TypeMismatchError : ValidationError {
	public TypeMismatchError(string expectedType, string actualType, int line)
		: base($"Type mismatch: expected '{expectedType}', got '{actualType}'", line) {
	}
}
