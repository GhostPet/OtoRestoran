public readonly struct Token {
	public readonly TokenType Type;
	public readonly string Lexeme;
	public readonly int Line;

	public Token(TokenType type, string lexeme, int line) {
		Type = type;
		Lexeme = lexeme;
		Line = line;
	}

	public override string ToString() {
		return $"{Type} '{Lexeme}' (line {Line})";
	}
}