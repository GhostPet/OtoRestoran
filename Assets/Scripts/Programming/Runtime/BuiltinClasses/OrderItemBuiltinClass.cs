public sealed class OrderItemBuiltinClass : BuiltinObject {
	private readonly OrderItem orderItem;

	public OrderItemBuiltinClass(OrderItem orderItem, ScriptInvocationContext context)
		: base(orderItem, context) {
		this.orderItem = orderItem;
	}

	public override string ClassName => "OrderItem";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (orderItem == null) {
			return false;
		}

		if (string.Equals(memberName, "item", System.StringComparison.OrdinalIgnoreCase)) {
			result = orderItem.Item;
			return true;
		}

		if (string.Equals(memberName, "quantity", System.StringComparison.OrdinalIgnoreCase)) {
			result = (float)orderItem.Quantity;
			return true;
		}

		if (string.Equals(memberName, "display_name", System.StringComparison.OrdinalIgnoreCase)) {
			result = orderItem.Item != null ? orderItem.Item.DisplayName : string.Empty;
			return true;
		}

		return false;
	}
}
