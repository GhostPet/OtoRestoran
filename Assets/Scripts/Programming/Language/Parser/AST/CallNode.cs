using System.Collections.Generic;

public class CallNode : ExpressionNode {
	public string FunctionName;
	public List<ExpressionNode> Arguments = new();
}
