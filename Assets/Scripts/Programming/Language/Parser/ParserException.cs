using System;

public class ParserException : Exception {
	public int Line { get; private set; }
	public int Column { get; private set; }

	public ParserException(string message, int line, int column)
		: base(message) {
		Line = line;
		Column = column;
	}

	public override string ToString() {
		return $"[ParserError] Line {Line}, Column {Column}: {Message}";
	}
}
