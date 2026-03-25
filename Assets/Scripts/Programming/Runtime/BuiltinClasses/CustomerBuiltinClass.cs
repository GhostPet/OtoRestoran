public sealed class CustomerBuiltinClass : BuiltinObject {
	private readonly Customer customer;

	public CustomerBuiltinClass(Customer customer, ScriptInvocationContext context)
		: base(customer, context) {
		this.customer = customer;
	}

	public override string ClassName => "Customer";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (customer == null) {
			return false;
		}

		if (string.Equals(memberName, "table", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(customer.Table);
			return true;
		}

		if (string.Equals(memberName, "state", System.StringComparison.OrdinalIgnoreCase)) {
			result = customer.State.ToString();
			return true;
		}

		if (string.Equals(memberName, "is_order_ready", System.StringComparison.OrdinalIgnoreCase)) {
			result = customer.IsOrderReady;
			return true;
		}

		if (string.Equals(memberName, "is_order_taken", System.StringComparison.OrdinalIgnoreCase)) {
			result = customer.IsOrderTaken;
			return true;
		}

		if (string.Equals(memberName, "is_order_served", System.StringComparison.OrdinalIgnoreCase)) {
			result = customer.IsOrderServed;
			return true;
		}

		if (string.Equals(memberName, "current_order", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(customer.CurrentOrder);
			return true;
		}

		return false;
	}

	public override bool TryInvoke(string memberName, object[] args, int line, out object result) {
		result = null;
		if (customer == null) {
			return false;
		}

		if (string.Equals(memberName, "has_order", System.StringComparison.OrdinalIgnoreCase)) {
			EnsureNoArguments(memberName, args, line);
			result = Context.IsRobotNearCustomerTable(customer) && customer.State == CustomerState.Ordering && customer.IsOrderReady && !customer.IsOrderTaken;
			return true;
		}

		if (string.Equals(memberName, "get_order", System.StringComparison.OrdinalIgnoreCase)) {
			EnsureNoArguments(memberName, args, line);
			if (!Context.IsRobotNearCustomerTable(customer)) {
				result = null;
				return true;
			}

			result = Wrap(customer.GetOrder());
			return true;
		}

		if (string.Equals(memberName, "serve_order", System.StringComparison.OrdinalIgnoreCase)) {
			if (args == null || args.Length < 1 || args.Length > 2) {
				throw new ValidationError($"serve_order() takes 1 or 2 arguments (line {line})", line);
			}

			if (!Context.IsRobotNearCustomerTable(customer)) {
				result = false;
				return true;
			}

			ItemSO item = ReadItemArgument(args[0], memberName, line);
			int quantity = args.Length == 2 ? ReadQuantityArgument(args[1], memberName, line) : 1;
			result = customer.TryServeOrderItem(item, quantity, Context.RobotInventory);
			return true;
		}

		return false;
	}
}
