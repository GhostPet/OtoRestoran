using System.Collections.Generic;
using UnityEngine;

public sealed class TableBuiltinClass : BuiltinObject {
	private readonly TableBehavior table;

	public TableBuiltinClass(TableBehavior table, ScriptInvocationContext context)
		: base(table, context) {
		this.table = table;
	}

	public override string ClassName => "Table";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (table == null) {
			return false;
		}

		if (string.Equals(memberName, "customers", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(table.Customers);
			return true;
		}

		if (string.Equals(memberName, "customer_count", System.StringComparison.OrdinalIgnoreCase)) {
			IReadOnlyList<Customer> customers = table.Customers;
			result = customers != null ? (float)customers.Count : 0f;
			return true;
		}

		if (string.Equals(memberName, "has_customer", System.StringComparison.OrdinalIgnoreCase)) {
			result = table.HasCustomer();
			return true;
		}

		if (string.Equals(memberName, "is_empty", System.StringComparison.OrdinalIgnoreCase)) {
			result = table.IsEmpty();
			return true;
		}

		if (string.Equals(memberName, "position", System.StringComparison.OrdinalIgnoreCase)) {
			result = table.transform.position;
			return true;
		}

		return false;
	}
}
