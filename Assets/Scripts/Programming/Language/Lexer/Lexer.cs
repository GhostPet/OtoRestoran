using System.Collections.Generic;
using System.Text;

public class Lexer {
	private readonly string _source;
	private int _index;
	private int _line = 1;

	private readonly List<Token> _tokens = new();
	private readonly Stack<int> _indentStack = new();

	private bool _atLineStart = true;

	private static readonly Dictionary<string, TokenType> _keywords = new()
	{
		{ "def", TokenType.KeywordDef },
		{ "if", TokenType.KeywordIf },
		{ "elif", TokenType.KeywordElif },
		{ "else", TokenType.KeywordElse },
		{ "while", TokenType.KeywordWhile },
		{ "for", TokenType.KeywordFor },
		{ "in", TokenType.KeywordIn },
		{ "return", TokenType.KeywordReturn }
	};

	public Lexer(string source) {
		// Normalize line endings and expand tabs to spaces so indentation counting is stable
		_source = source.Replace("\r\n", "\n").Replace("\t", "    ");
		_indentStack.Push(0);
	}

	public List<Token> Tokenize() {
		while (!IsAtEnd()) {
			if (_atLineStart) {
				HandleIndentation();
				_atLineStart = false;
			}

			char c = Advance();

			if (c == ' ' || c == '\t')
				continue;

            if (c == '\n') {
                // Collapse consecutive blank lines into a single NewLine token so parser
                // doesn't get confused by multiple empty lines.
                _tokens.Add(new Token(TokenType.NewLine, "\\n", _line));
                _line++;
                _atLineStart = true;

                // consume any additional newline characters immediately following
                while (!IsAtEnd() && Peek() == '\n') {
                    Advance();
                    _line++;
                }

                continue;
            }

			if (char.IsLetter(c) || c == '_') {
				ReadIdentifier(c);
				continue;
			}

			if (char.IsDigit(c)) {
				ReadNumber(c);
				continue;
			}

			switch (c) {
				case ':': Add(TokenType.Colon, ":"); break;
				case ',': Add(TokenType.Comma, ","); break;
			case '(': Add(TokenType.LParen, "("); break;
			case ')': Add(TokenType.RParen, ")"); break;
			case '+': Add(TokenType.Plus, "+"); break;
			case '-': Add(TokenType.Minus, "-"); break;
			case '*': Add(TokenType.Star, "*"); break;
			case '/': Add(TokenType.Slash, "/"); break;
			case '.':
				// Support numbers starting with a dot like .5
				if (_index < _source.Length && char.IsDigit(Peek())) {
					var sb = new StringBuilder();
					sb.Append('0');
					sb.Append(Advance()); // consume '.'
					// consume following digits
					while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
					_tokens.Add(new Token(TokenType.Number, sb.ToString(), _line));
					break;
				}
				throw new LexerException($"Unexpected character '.'", _line);
				case '>':
					if (Peek() == '=') { Advance(); Add(TokenType.GreaterEqual, ">="); }
					else Add(TokenType.Greater, ">");
					break;
				case '<':
					if (Peek() == '=') { Advance(); Add(TokenType.LessEqual, "<="); }
					else Add(TokenType.Less, "<");
					break;
				case '!':
					if (Peek() == '=') { Advance(); Add(TokenType.NotEqual, "!="); break; }
					throw new LexerException($"Unexpected character '!'", _line);
				case '=':
					if (Peek() == '=') { Advance(); Add(TokenType.EqualEqual, "=="); break; }
					Add(TokenType.Equals, "="); break;
				case '"':
				case '\'': ReadString(c); break; // Hem ' hem " için
				default:
					throw new LexerException($"Unexpected character '{c}'", _line);
			}
		}

		EmitDedentsAtEOF();
		_tokens.Add(new Token(TokenType.EOF, string.Empty, _line));
		return _tokens;
	}

	private void HandleIndentation() {
		int count = 0;
		int i = _index;

		// Count spaces/tabs without changing _index directly; we'll advance _index to i after counting.
		while (i < _source.Length) {
			char c = _source[i];
			if (c == ' ') { count++; i++; }
			else if (c == '\t') { count += 4; i++; }
			else break;
		}

		int prev = _indentStack.Peek();

		if (count > prev) {
			_indentStack.Push(count);
			_tokens.Add(new Token(TokenType.Indent, string.Empty, _line));
		} else {
			while (count < prev) {
				_indentStack.Pop();
				_tokens.Add(new Token(TokenType.Dedent, string.Empty, _line));
				prev = _indentStack.Peek();
			}

			if (count != prev)
				throw new LexerException("Invalid indentation level", _line);
		}

		// Move index to the first non-indent character
		_index = i;

	}

	private void EmitDedentsAtEOF() {
		while (_indentStack.Count > 1) {
			_indentStack.Pop();
			_tokens.Add(new Token(TokenType.Dedent, string.Empty, _line));
		}
	}

	private void ReadIdentifier(char first) {
		var sb = new StringBuilder();
		sb.Append(first);

		while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
			sb.Append(Advance());

		string text = sb.ToString();

		if (_keywords.TryGetValue(text, out var type))
			_tokens.Add(new Token(type, text, _line));
		else
			_tokens.Add(new Token(TokenType.Identifier, text, _line));
	}

	private void ReadNumber(char first) {
		var sb = new StringBuilder();
		sb.Append(first);

		bool seenDot = false;

		while (!IsAtEnd()) {
			char p = Peek();
			if (char.IsDigit(p)) {
				sb.Append(Advance());
				continue;
			}

			if (p == '.' && !seenDot) {
				// only accept dot if followed by a digit (to avoid lone dot tokens)
				if (_index + 1 < _source.Length && char.IsDigit(_source[_index + 1])) {
					seenDot = true;
					sb.Append(Advance()); // consume '.'
					continue;
				}
			}

			break;
		}

		_tokens.Add(new Token(TokenType.Number, sb.ToString(), _line));
	}

	private void ReadString(char quoteType) {
		var sb = new StringBuilder();

		while (!IsAtEnd() && Peek() != quoteType) {
			if (Peek() == '\\') {
				Advance();
				if (IsAtEnd()) break;

				char esc = Peek();
				switch (esc) {
					case 'n': sb.Append('\n'); break;
					case 't': sb.Append('\t'); break;
					case '\\': sb.Append('\\'); break;
					case '\'': sb.Append('\''); break;
					case '"': sb.Append('"'); break;
					default: sb.Append(esc); break;
				}
				Advance();
			} else {
				sb.Append(Advance());
			}
		}

		if (IsAtEnd())
			throw new LexerException("Unterminated string literal", _line);

		Advance(); // closing quote
		_tokens.Add(new Token(TokenType.String, sb.ToString(), _line));
	}

	private char Advance() => _source[_index++];

	private char Peek() => IsAtEnd() ? '\0' : _source[_index];

	private bool IsAtEnd() => _index >= _source.Length;

	private void Add(TokenType type, string lexeme) =>
		_tokens.Add(new Token(type, lexeme, _line));
}
