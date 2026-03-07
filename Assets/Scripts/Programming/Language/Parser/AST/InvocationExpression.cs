using System.Collections.Generic;

public class InvocationExpression : ExpressionNode {
	public ExpressionNode Target;
	public List<ExpressionNode> Arguments = new();
}
