using System;
using System.Collections.Generic;

public class Parser {
	private readonly List<Token> _tokens;
	private int _current;

	public Parser(List<Token> tokens) {
		_tokens = tokens;
	}

	public List<FunctionDefNode> Parse() {
		var functions = new List<FunctionDefNode>();

		while (!IsAtEnd()) {
			// Skip stray newlines/indentation tokens between top-level functions
			if (Match(TokenType.NewLine) || Match(TokenType.Indent) || Match(TokenType.Dedent))
				continue;

			// Only parse top-level function definitions; skip unexpected tokens to recover
			if (Check(TokenType.KeywordDef)) {
				functions.Add(ParseFunction());
			} else {
				// advance past unexpected token
				Advance();
			}
		}

		return functions;
	}

	private FunctionDefNode ParseFunction() {
		var defToken = Consume(TokenType.KeywordDef, "Expected 'def'");
		var name = Consume(TokenType.Identifier, "Expected function name");

		Consume(TokenType.LParen, "Expected '('");
		Consume(TokenType.RParen, "Expected ')'");
		Consume(TokenType.Colon, "Expected ':'");
		Consume(TokenType.NewLine, "Expected newline after function declaration");
		Consume(TokenType.Indent, "Expected indented block");

		var fn = new FunctionDefNode {
			Name = name.Lexeme,
			Line = defToken.Line,
			Body = ParseBlock().Statements
		};
		return fn;
	}

	private BlockNode ParseBlock() {
		var block = new BlockNode();

		while (!Check(TokenType.Dedent) && !IsAtEnd()) {
			if (Match(TokenType.NewLine))
				continue;

			block.Statements.Add(ParseStatement());
		}

		Consume(TokenType.Dedent, "Expected dedent");
		return block;
	}

	private AstNode ParseStatement() {
		if (Match(TokenType.KeywordIf)) return ParseIf();
		if (Match(TokenType.KeywordWhile)) return ParseWhile();
		if (Match(TokenType.KeywordFor)) return ParseFor();

		if (Check(TokenType.Identifier) && CheckNext(TokenType.Equals))
			return ParseAssignment();

		return ParseCall();
	}

	private AstNode ParseIf() {
		var ifNode = new IfNode {
			Condition = ParseExpression()
		};
		Consume(TokenType.Colon, "Expected ':' after if condition");
		Consume(TokenType.NewLine, "Expected newline");
		Consume(TokenType.Indent, "Expected indent");

		ifNode.ThenBlock = ParseBlock();

		if (Match(TokenType.KeywordElif)) {
			var elseIf = new IfNode {
				Condition = ParseExpression()
			};
			Consume(TokenType.Colon, "Expected ':'");
			Consume(TokenType.NewLine, "Expected newline");
			Consume(TokenType.Indent, "Expected indent");
			elseIf.ThenBlock = ParseBlock();

			ifNode.ElseBlock = new BlockNode();
			ifNode.ElseBlock.Statements.Add(elseIf);
		} else if (Match(TokenType.KeywordElse)) {
			Consume(TokenType.Colon, "Expected ':'");
			Consume(TokenType.NewLine, "Expected newline");
			Consume(TokenType.Indent, "Expected indent");
			ifNode.ElseBlock = ParseBlock();
		}

		return ifNode;
	}

	private AstNode ParseWhile() {
		var node = new WhileNode {
			Condition = ParseExpression()
		};
		Consume(TokenType.Colon, "Expected ':'");
		Consume(TokenType.NewLine, "Expected newline");
		Consume(TokenType.Indent, "Expected indent");
		node.Body = ParseBlock();
		return node;
	}

	private AstNode ParseFor() {
		var iterator = Consume(TokenType.Identifier, "Expected iterator name");
		Consume(TokenType.KeywordIn, "Expected 'in'");
		var iterable = ParseExpression();

		Consume(TokenType.Colon, "Expected ':'");
		Consume(TokenType.NewLine, "Expected newline");
		Consume(TokenType.Indent, "Expected indent");

		return new ForNode {
			IteratorName = iterator.Lexeme,
			Iterable = iterable,
			Body = ParseBlock()
		};
	}

	private AstNode ParseAssignment() {
		var name = Consume(TokenType.Identifier, "Expected variable name");
		Consume(TokenType.Equals, "Expected '='");
		var value = ParseExpression();
		Consume(TokenType.NewLine, "Expected newline after assignment");

		return new AssignmentNode {
			VariableName = name.Lexeme,
			Value = value,
			Line = name.Line
		};
	}

	private AstNode ParseCall() {
		var call = ParseExpression();

		// Accept a terminating newline, or allow a dedent/EOF (end of block/file)
		if (Match(TokenType.NewLine))
			return call;

		if (Check(TokenType.Dedent) || IsAtEnd())
			return call;

		throw Error(Peek(), "Expected newline after statement");
	}

	private ExpressionNode ParseExpression() {
		// Use precedence climbing to parse binary expressions (+ - * /)
		return ParseBinary(0);
	}

	private ExpressionNode ParseBinary(int parentPrecedence) {
		ExpressionNode left = ParsePrimary();

		while (true) {
			int precedence = GetPrecedence(Peek().Type);
			if (precedence <= parentPrecedence) break;
			var op = Advance();
			var right = ParseBinary(precedence);
			left = new BinaryExpression { Left = left, Right = right, Operator = op.Type, Line = op.Line };
		}

		return left;
	}

	private int GetPrecedence(TokenType t) {
		switch (t) {
			case TokenType.Star:
			case TokenType.Slash:
				return 20;
			case TokenType.Plus:
			case TokenType.Minus:
				return 10;
			case TokenType.Greater:
			case TokenType.Less:
			case TokenType.GreaterEqual:
			case TokenType.LessEqual:
			case TokenType.EqualEqual:
			case TokenType.NotEqual:
				return 5;
			default:
				return 0;
		}
	}

	private ExpressionNode ParsePrimary() {
		if (Match(TokenType.Number))
			return new NumberLiteralExpression { Value = float.Parse(Previous().Lexeme), Line = Previous().Line };

		if (Match(TokenType.String))
			return new StringLiteralExpression { Value = Previous().Lexeme, Line = Previous().Line };

		if (Match(TokenType.Identifier)) {
			var id = Previous();

			if (Match(TokenType.LParen)) {
				var call = new CallNode { FunctionName = id.Lexeme, Line = id.Line };

				if (!Check(TokenType.RParen)) {
					do {
						call.Arguments.Add(ParseExpression());
					}
					while (Match(TokenType.Comma));
				}

				Consume(TokenType.RParen, "Expected ')'");
				return call;
			}

			return new IdentifierExpression { Name = id.Lexeme, Line = id.Line };
		}

		// parenthesized tuple or expression
		if (Match(TokenType.LParen)) {
			var start = Previous();
			var inner = new ListLiteralExpression { Line = start.Line };
			if (!Check(TokenType.RParen)) {
				do {
					inner.Elements.Add(ParseExpression());
				} while (Match(TokenType.Comma));
			}

			Consume(TokenType.RParen, "Expected ')' for tuple/list literal");
			if (inner.Elements.Count == 1 && !Check(TokenType.Comma))
				return inner.Elements[0];
			return inner;
		}

		throw Error(Peek(), "Invalid expression");
	}

	// ------------------ Helpers ------------------

	private bool Match(TokenType type) {
		if (Check(type)) {
			Advance();
			return true;
		}
		return false;
	}

	private Token Consume(TokenType type, string message) {
		if (Check(type)) return Advance();
		throw Error(Peek(), message);
	}

	private bool Check(TokenType type) =>
		!IsAtEnd() && Peek().Type == type;

	private bool CheckNext(TokenType type) =>
		_current + 1 < _tokens.Count && _tokens[_current + 1].Type == type;

	private Token Advance() {
		if (!IsAtEnd()) _current++;
		return Previous();
	}

	private bool IsAtEnd() => Peek().Type == TokenType.EOF;

	private Token Peek() => _tokens[_current];
	private Token Previous() => _tokens[_current - 1];

	private Exception Error(Token token, string message) =>
		new($"[Parser] Line {token.Line}: {message} (token: {token.Type} '{token.Lexeme}')");
}
