public enum TokenType {
	Identifier,
	Number,
	String,

	NewLine,
	Indent,
	Dedent,

	Colon,
	Comma,
	Equals,
	LParen,
	RParen,
	Plus,
	Minus,
	Star,
	Slash,
    LBracket,
    RBracket,
	Greater,
	Less,
	GreaterEqual,
	LessEqual,
	EqualEqual,
	NotEqual,

	KeywordDef,
	KeywordIf,
	KeywordElif,
	KeywordElse,
	KeywordWhile,
	KeywordFor,
	KeywordIn,
	KeywordReturn,

	EOF
}