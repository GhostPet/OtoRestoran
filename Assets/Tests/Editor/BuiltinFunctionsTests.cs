using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BuiltinFunctionsTests {
	[Test]
	public void Range_SingleArg_ReturnsSequence() {
		var res = BuiltinFunctions.Invoke("range", new object[] { 5f }) as List<object>;
		Assert.IsNotNull(res);
		Assert.AreEqual(5, res.Count);
		for (int i = 0; i < 5; i++) Assert.AreEqual((float)i, res[i]);
	}

	[Test]
	public void Range_StartStopStep_NegativeStep_Works() {
		var res = BuiltinFunctions.Invoke("range", new object[] { 5f, 0f, -1f }) as List<object>;
		Assert.IsNotNull(res);
		Assert.AreEqual(5, res.Count);
		for (int i = 0; i < 5; i++) Assert.AreEqual((float)(5 - i), res[i]);
	}

	[Test]
	public void Len_ListAndString_ReturnsLengths() {
		var list = new List<object> { 1f, 2f, 3f };
		Assert.AreEqual(3f, BuiltinFunctions.Invoke("len", new object[] { list }));
		Assert.AreEqual(5f, BuiltinFunctions.Invoke("len", new object[] { "hello" }));
	}

	[Test]
	public void Type_PrimitivesAndList_ReturnsNames() {
		Assert.AreEqual("null", BuiltinFunctions.Invoke("type", new object[] { null }));
		Assert.AreEqual("string", BuiltinFunctions.Invoke("type", new object[] { "x" }));
		Assert.AreEqual("float", BuiltinFunctions.Invoke("type", new object[] { 1f }));
		Assert.AreEqual("int", BuiltinFunctions.Invoke("type", new object[] { 1 }));
		Assert.AreEqual("bool", BuiltinFunctions.Invoke("type", new object[] { true }));
		var l = new List<object> { 1f };
		Assert.AreEqual("list", BuiltinFunctions.Invoke("type", new object[] { l }));
	}

	[Test]
	public void IntFloatStr_Conversions_AndErrors() {
		Assert.AreEqual(5, BuiltinFunctions.Invoke("int", new object[] { 5f }));
		Assert.AreEqual(3f, BuiltinFunctions.Invoke("float", new object[] { 3 }));
		Assert.AreEqual("42", BuiltinFunctions.Invoke("str", new object[] { 42 }));
		Assert.Throws<ValidationError>(() => BuiltinFunctions.Invoke("int", new object[] { "nope" }));
		Assert.Throws<ValidationError>(() => BuiltinFunctions.Invoke("float", new object[] { "nope" }));
	}

	[Test]
	public void GetTables_ReturnsSceneTables() {
		var go1 = new GameObject("table_test_1");
		var go2 = new GameObject("table_test_2");
		var t1 = go1.AddComponent<TableBehavior>();
		var t2 = go2.AddComponent<TableBehavior>();

		try {
          var res = BuiltinFunctions.Invoke("get_tables", Array.Empty<object>()) as List<object>;
			Assert.IsNotNull(res);
           Assert.IsTrue(ContainsWrapped<TableBehavior>(res, t1));
			Assert.IsTrue(ContainsWrapped<TableBehavior>(res, t2));
		} finally {
			UnityEngine.Object.DestroyImmediate(go1);
			UnityEngine.Object.DestroyImmediate(go2);
		}
	}

	[Test]
	public void GetFurnaces_ReturnsSceneFurnaces() {
		var go1 = new GameObject("furnace_test_1");
		var go2 = new GameObject("furnace_test_2");
		var f1 = go1.AddComponent<OvenBehavior>();
		var f2 = go2.AddComponent<OvenBehavior>();

		try {
         var res = BuiltinFunctions.Invoke("get_furnaces", Array.Empty<object>()) as List<object>;
			Assert.IsNotNull(res);
           Assert.IsTrue(ContainsWrapped<OvenBehavior>(res, f1));
			Assert.IsTrue(ContainsWrapped<OvenBehavior>(res, f2));
		} finally {
			UnityEngine.Object.DestroyImmediate(go1);
			UnityEngine.Object.DestroyImmediate(go2);
		}
	}

	[Test]
	public void ActiveOrders_ReturnsRememberedOrders() {
		ActiveOrders.Clear();
		var customerGo = new GameObject("order_customer_test");
		var customer = customerGo.AddComponent<Customer>();
		var item = ScriptableObject.CreateInstance<ItemSO>();
		var order = new Order { Customer = customer };
		order.Items.Add(new OrderItem(item, 1));
		ActiveOrders.Remember(order);

		try {
			var res = BuiltinFunctions.Invoke("active_orders", Array.Empty<object>()) as List<Order>;
          if (res != null) {
				Assert.AreEqual(1, res.Count);
				Assert.AreEqual(order, res[0]);
			} else {
				var wrapped = BuiltinFunctions.Invoke("active_orders", Array.Empty<object>()) as List<object>;
				Assert.IsNotNull(wrapped);
				Assert.IsTrue(ContainsWrapped<Order>(wrapped, order));
			}
		} finally {
			ActiveOrders.Clear();
			UnityEngine.Object.DestroyImmediate(item);
			UnityEngine.Object.DestroyImmediate(customerGo);
		}
	}

	private static bool ContainsWrapped<T>(List<object> wrappedObjects, T expected) where T : class {
		if (wrappedObjects == null || expected == null) {
			return false;
		}

		for (int i = 0; i < wrappedObjects.Count; i++) {
			if (wrappedObjects[i] is not BuiltinObject builtin) {
				continue;
			}

			if (ReferenceEquals(builtin.RawInstance, expected)) {
				return true;
			}
		}

		return false;
	}
}
