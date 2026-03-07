using System.Collections.Generic;

public class Order {
	public Customer Customer;
	public List<string> Items = new();
}

public static class ActiveOrders {
	private static readonly List<Order> orders = new();

	public static IReadOnlyList<Order> Orders => orders;

	public static void Remember(Order order) {
		if (order == null) return;
		if (!orders.Contains(order)) orders.Add(order);
	}

	public static void Remove(Order order) {
		if (order == null) return;
		orders.Remove(order);
	}

	public static void RemoveByCustomer(Customer customer) {
		if (customer == null) return;
		for (int i = orders.Count - 1; i >= 0; i--) {
			var order = orders[i];
			if (order == null) {
				orders.RemoveAt(i);
				continue;
			}

			if (order.Customer == customer) orders.RemoveAt(i);
		}
	}

	public static List<Order> Snapshot() {
		return new List<Order>(orders);
	}

	public static void Clear() {
		orders.Clear();
	}
}
