using System.Collections.Generic;

public class FunctionDefNode : AstNode {
	public string Name;
	public List<AstNode> Body = new();
}
