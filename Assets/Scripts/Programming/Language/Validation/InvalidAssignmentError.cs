public class InvalidAssignmentError : ValidationError {
	public InvalidAssignmentError(string variableName, string reason, int line)
		: base($"Invalid assignment to '{variableName}': {reason}", line) {
	}
}
