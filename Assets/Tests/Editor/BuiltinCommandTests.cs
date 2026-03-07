using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuiltinCommandTests {
	[Test]
	public void PrintCommand_PrintsVariousValues() {
		var cmd = new PrintCommand();
		CommandExecutionContext.CurrentLine = 42;
		// Try string
		Assert.IsTrue(cmd.Tick("hello"));
		// Try number
		Assert.IsTrue(cmd.Tick(3f));
		// Try list
		var list = new List<object> { 1f, 2f, 3f };
		Assert.IsTrue(cmd.Tick(list));
		// Try nested list
		var nested = new List<object> { new List<object> { 1f, 2f }, 3f };
		Assert.IsTrue(cmd.Tick(nested));
	}

	[Test]
	public void WaitCommand_WaitsAndCompletes() {
		var cmd = new WaitCommand();
		cmd.Reset();

		// Başlatmak için bir kez çağırmak zorunlu değil (kod içinde ilk tick başlangıcı ayarlar),
		// bu yüzden ilk çağrının dönüş değerine güvenmiyoruz ve deterministik bir loop ile tamamlanmayı bekliyoruz.
		bool done = false;
		int safety = 0;
		const float delta = 0.02f;
		const int maxTicks = 200; // yeterli büyük bir güvenlik sınırı

		// Simüle edilecek toplam bekleme süresi: 0.2s
		// İlk olarak komutu başlatıyoruz (set timeRemaining)
		cmd.Tick(0.2f);

		while (!done && safety < maxTicks) {
			done = cmd.Tick(delta);
			safety++;
		}

		Assert.IsTrue(done, "WaitCommand did not complete within expected ticks");
		// reset sonrası başlangıç durumuna dönsün
		cmd.Reset();
	}

	[Test]
	public void DropAndCleanServe_Patterns() {
		var drop = new DropCommand();
		drop.Reset();
		Assert.IsFalse(drop.Tick("item"));
		// second call should return true
		Assert.IsTrue(drop.Tick("item"));

		var clean = new CleanTableCommand();
		clean.Reset();
		Assert.IsFalse(clean.Tick("t1"));
		Assert.IsTrue(clean.Tick("t1"));

		var serve = new ServeOrderCommand();
		serve.Reset();
		Assert.IsFalse(serve.Tick("o1"));
		Assert.IsTrue(serve.Tick("o1"));
	}

	[Test]
	public void MoveToCommand_AcceptsTableBehavior() {
		var go = new GameObject("move_to_table_test");
		var table = go.AddComponent<TableBehavior>();

		try {
			var move = new MoveToCommand();
			move.Reset();
			Assert.IsTrue(move.Tick(table));
		} finally {
			Object.DestroyImmediate(go);
		}
	}

	[Test]
	public void MoveToCommand_AcceptsRestaurantObject() {
		var go = new GameObject("move_to_oven_test");
		var oven = go.AddComponent<OvenBehavior>();

		try {
			var move = new MoveToCommand();
			move.Reset();
			Assert.IsTrue(move.Tick(oven));
		} finally {
			UnityEngine.Object.DestroyImmediate(go);
		}
	}

	[Test]
	public void TableBehavior_Customers_ReturnsCustomersFromAttachedChairs() {
		var tableGo = new GameObject("table_customers_test");
		var chairGo = new GameObject("chair_customers_test");
		var customerGo = new GameObject("customer_customers_test");

		var table = tableGo.AddComponent<TableBehavior>();
		var chair = chairGo.AddComponent<ChairBehavior>();
		var customer = customerGo.AddComponent<Customer>();

		try {
			chair.SetTable(table);
			chair.Assign(customer);

			var customers = table.Customers;
			Assert.IsNotNull(customers);
			Assert.AreEqual(1, customers.Count);
			Assert.AreEqual(customer, customers[0]);
		} finally {
			UnityEngine.Object.DestroyImmediate(customerGo);
			UnityEngine.Object.DestroyImmediate(chairGo);
			UnityEngine.Object.DestroyImmediate(tableGo);
		}
	}
}
