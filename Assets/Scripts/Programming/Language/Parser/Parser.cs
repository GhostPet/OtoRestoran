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
		List<string> parameters = ParseParameterNames();
		Consume(TokenType.RParen, "Expected ')'");
		Consume(TokenType.Colon, "Expected ':'");
		Consume(TokenType.NewLine, "Expected newline after function declaration");
		Consume(TokenType.Indent, "Expected indented block");

		var fn = new FunctionDefNode {
			Name = name.Lexeme,
			Line = defToken.Line,
			Parameters = parameters,
			Body = ParseBlock().Statements
		};
		return fn;
	}

	private List<string> ParseParameterNames() {
		var parameters = new List<string>();
		if (Check(TokenType.RParen)) {
			return parameters;
		}

		do {
			Token parameter = Consume(TokenType.Identifier, "Expected parameter name");
			parameters.Add(parameter.Lexeme);
		} while (Match(TokenType.Comma));

		return parameters;
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

		// Detect assignment even for complex targets like "a[i], b = ..." by
		// scanning ahead for an '=' before end-of-statement tokens.
		if (IsAssignmentAhead()) return ParseAssignment();

		return ParseCall();
	}

	private bool IsAssignmentAhead() {
		int i = _current;
		while (i < _tokens.Count) {
			var t = _tokens[i].Type;
			if (t == TokenType.Equals) return true;
			if (t == TokenType.NewLine || t == TokenType.Colon || t == TokenType.Dedent || t == TokenType.EOF) return false;
			i++;
		}
		return false;
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
		// Support: multiple assignment targets like 'a, b = ...' and indexing 'a[i], b[j] = ...'
		var targets = new System.Collections.Generic.List<ExpressionNode>();

		while (true) {
			if (Check(TokenType.Identifier)) {
				var id = Advance();
				ExpressionNode baseExpr = new IdentifierExpression { Name = id.Lexeme, Line = id.Line };
				if (Match(TokenType.LBracket)) {
					var idx = ParseExpression();
					Consume(TokenType.RBracket, "Expected ']' after index");
					baseExpr = new IndexExpression { Target = baseExpr, Index = idx, Line = id.Line };
				}
				targets.Add(baseExpr);
			} else {
				throw Error(Peek(), "Expected target variable in assignment");
			}

			if (Match(TokenType.Comma)) continue;
			break;
		}

		Consume(TokenType.Equals, "Expected '='");
		// Parse right-hand side: single expression or comma-separated list (tuple) without parentheses
		var firstValue = ParseExpression();
		ExpressionNode value;
		if (Match(TokenType.Comma)) {
			var list = new ListLiteralExpression { Line = firstValue.Line };
			list.Elements.Add(firstValue);
			do {
				list.Elements.Add(ParseExpression());
			} while (Match(TokenType.Comma));
			value = list;
		} else {
			value = firstValue;
		}
		Consume(TokenType.NewLine, "Expected newline after assignment");

		return new AssignmentNode {
			Targets = targets,
			Value = value
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
			case TokenType.KeywordIn:
				return 5;
			default:
				return 0;
		}
	}

	private ExpressionNode ParsePrimary() {
		// Support unary minus (e.g. -5 or -x)
		if (Match(TokenType.Minus)) {
			// If directly followed by a number token, return a negative number literal
			if (Match(TokenType.Number)) {
				return new NumberLiteralExpression { Value = -float.Parse(Previous().Lexeme), Line = Previous().Line };
			}
			// Otherwise parse the following primary and represent unary minus as (0 - expr)
			var rhs = ParsePrimary();
			return new BinaryExpression { Left = new NumberLiteralExpression { Value = 0f, Line = rhs.Line }, Right = rhs, Operator = TokenType.Minus, Line = rhs.Line };
		}

		ExpressionNode expr;
		if (Match(TokenType.Number)) {
			expr = new NumberLiteralExpression { Value = float.Parse(Previous().Lexeme), Line = Previous().Line };
		} else if (Match(TokenType.String)) {
			expr = new StringLiteralExpression { Value = Previous().Lexeme, Line = Previous().Line };
		} else if (Match(TokenType.KeywordNone)) {
			expr = new NullLiteralExpression { Line = Previous().Line };
		} else if (Match(TokenType.KeywordTrue)) {
			expr = new BooleanLiteralExpression { Value = true, Line = Previous().Line };
		} else if (Match(TokenType.KeywordFalse)) {
			expr = new BooleanLiteralExpression { Value = false, Line = Previous().Line };
		} else if (Match(TokenType.Identifier)) {
			var id = Previous();
			expr = new IdentifierExpression { Name = id.Lexeme, Line = id.Line };
		} else if (Match(TokenType.LParen) || Match(TokenType.LBracket)) {
			// list literal using square brackets or parenthesized tuple
			var start = Previous();
			var inner = new ListLiteralExpression { Line = start.Line };
			if (!(Check(TokenType.RParen) || Check(TokenType.RBracket))) {
				do {
					inner.Elements.Add(ParseExpression());
				} while (Match(TokenType.Comma));
			}

			if (start.Type == TokenType.LParen)
				Consume(TokenType.RParen, "Expected ')' for tuple/list literal");
			else
				Consume(TokenType.RBracket, "Expected ']' for list literal");

			expr = inner;
		} else {
			throw Error(Peek(), "Invalid expression");
		}

		while (true) {
			if (Match(TokenType.Dot)) {
				var member = Consume(TokenType.Identifier, "Expected member name after '.'");
				expr = new MemberAccessExpression {
					Target = expr,
					MemberName = member.Lexeme,
					Line = member.Line
				};
				continue;
			}

			if (Match(TokenType.LBracket)) {
				var idx = ParseExpression();
				Consume(TokenType.RBracket, "Expected ']' after index");
				expr = new IndexExpression { Target = expr, Index = idx, Line = expr.Line };
				continue;
			}

			if (Match(TokenType.LParen)) {
				var args = new List<ExpressionNode>();
				if (!Check(TokenType.RParen)) {
					do {
						args.Add(ParseExpression());
					} while (Match(TokenType.Comma));
				}
				Consume(TokenType.RParen, "Expected ')'");

				if (expr is IdentifierExpression idExpr) {
					var call = new CallNode { FunctionName = idExpr.Name, Line = idExpr.Line };
					call.Arguments.AddRange(args);
					expr = call;
				} else {
					var invoke = new InvocationExpression { Target = expr, Line = expr.Line };
					invoke.Arguments.AddRange(args);
					expr = invoke;
				}
				continue;
			}

			break;
		}

		return expr;
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
