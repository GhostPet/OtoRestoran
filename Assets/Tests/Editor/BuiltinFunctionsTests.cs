using System;
using System.Collections.Generic;
using NUnit.Framework;

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
}
