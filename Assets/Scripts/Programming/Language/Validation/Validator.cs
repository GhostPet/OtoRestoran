using System.Collections.Generic;
using System.Text;

public class Validator {
	public List<ValidationError> Validate(List<string> lines) {
		var errors = new List<ValidationError>();
		if (lines == null || lines.Count == 0) {
			return errors;
		}

		var builder = new StringBuilder();
		for (int i = 0; i < lines.Count; i++) {
			if (i > 0) {
				builder.Append('\n');
			}

			builder.Append(lines[i] ?? string.Empty);
		}

		try {
			Lexer lexer = new(builder.ToString());
			List<Token> tokens = lexer.Tokenize();
			Parser parser = new(tokens);
			parser.Parse();
		} catch (ValidationError validationError) {
			errors.Add(validationError);
		} catch (LexerException lexerException) {
			errors.Add(new InvalidFunctionCallError($"Syntax error: {lexerException.Message}", lexerException.Line));
		} catch (ParserException parserException) {
			errors.Add(new InvalidFunctionCallError($"Syntax error: {parserException.Message}", parserException.Line));
		} catch (System.Exception ex) {
			errors.Add(new InvalidFunctionCallError($"Syntax error: {ex.Message}", 1));
		}

		return errors;
	}
}
