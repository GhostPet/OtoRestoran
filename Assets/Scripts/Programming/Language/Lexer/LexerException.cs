using System;

public class LexerException : Exception {
	public int Line { get; }

	public LexerException(string message, int line)
		: base($"[Lexer] Line {line}: {message}") {
		Line = line;
	}
}