public class InvalidFunctionCallError : ValidationError {
	// Accept a custom message so callers (like the Validator) can pass
	// detailed syntax/error messages along with the line number.
	public InvalidFunctionCallError(string message, int line)
		: base(message, line) {
	}
}
