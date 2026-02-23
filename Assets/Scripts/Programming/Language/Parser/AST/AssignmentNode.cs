public class AssignmentNode : AstNode {
	// Support multiple targets: e.g. a, b or a[i], b[j]
	public System.Collections.Generic.List<ExpressionNode> Targets = new();
	public ExpressionNode Value;
}
