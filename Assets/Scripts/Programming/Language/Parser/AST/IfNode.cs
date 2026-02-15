public class IfNode : AstNode {
	public ExpressionNode Condition;
	public BlockNode ThenBlock;
	public BlockNode ElseBlock; // null olabilir
}
