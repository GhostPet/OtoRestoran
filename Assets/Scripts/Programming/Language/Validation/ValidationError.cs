using System;

public class ValidationError : Exception {
	public int Line { get; private set; }
	public int Column { get; private set; }

	public ValidationError(string message, int line = -1, int column = -1)
		: base(message) {
		Line = line;
		Column = column;
	}

	public override string ToString() {
		string loc = (Line >= 0 && Column >= 0) ? $"Line {Line}, Column {Column}: " :
					 (Line >= 0) ? $"Line {Line}: " : "";
		return $"[ValidationError] {loc}{Message}";
	}
}
