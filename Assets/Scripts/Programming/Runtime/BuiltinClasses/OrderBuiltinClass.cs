using System.Collections.Generic;

public sealed class OrderBuiltinClass : BuiltinObject {
	private readonly Order order;

	public OrderBuiltinClass(Order order, ScriptInvocationContext context)
		: base(order, context) {
		this.order = order;
	}

	public override string ClassName => "Order";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (order == null) {
			return false;
		}

		if (string.Equals(memberName, "customer", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(order.Customer);
			return true;
		}

		if (string.Equals(memberName, "items", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(order.Items);
			return true;
		}

		if (string.Equals(memberName, "item_count", System.StringComparison.OrdinalIgnoreCase)) {
			List<OrderItem> items = order.Items;
			result = items != null ? (float)items.Count : 0f;
			return true;
		}

		return false;
	}

	public override bool TryInvoke(string memberName, object[] args, int line, out object result) {
		result = null;
		if (order == null) {
			return false;
		}

		if (string.Equals(memberName, "is_fulfilled", System.StringComparison.OrdinalIgnoreCase)) {
			EnsureNoArguments(memberName, args, line);
			result = order.IsFulfilledBy(Context.RobotInventory);
			return true;
		}

		return false;
	}
}
