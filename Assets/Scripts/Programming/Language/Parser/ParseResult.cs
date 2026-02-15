using System.Collections.Generic;

public class ParseResult {
	public AstNode Root { get; private set; }
	public List<ParserException> Errors { get; private set; }

	public bool HasErrors => Errors.Count > 0;

	public ParseResult(AstNode root) {
		Root = root;
		Errors = new List<ParserException>();
	}

	public ParseResult(List<ParserException> errors) {
		Root = null;
		Errors = errors;
	}

	public void AddError(ParserException error) {
		Errors.Add(error);
	}
}
