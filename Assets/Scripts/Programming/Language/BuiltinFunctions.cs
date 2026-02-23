using System;
using System.Collections;
using System.Collections.Generic;

public static class BuiltinFunctions {
	private static readonly Dictionary<string, Func<object[], int, object>> _funcs = new(StringComparer.OrdinalIgnoreCase);

	static BuiltinFunctions() {
		Register("range", Range);
		Register("len", Len);
		Register("type", Type);
		Register("int", Int);
		Register("float", Float);
		Register("str", Str);
	}

	public static void Register(string name, Func<object[], int, object> func) {
		_funcs[name] = func;
	}

	public static bool IsBuiltin(string name) => _funcs.ContainsKey(name);

	public static object Invoke(string name, object[] args, int line = -1) {
		if (!_funcs.TryGetValue(name, out var f)) throw new ValidationError($"Unsupported call expression '{name}' (line {line})", line);
		return f(args ?? new object[0], line);
	}

	// ---------- Implementations ----------
	private static object Range(object[] args, int line) {
		if (args.Length < 1 || args.Length > 3) throw new ValidationError($"range() takes 1 to 3 integer arguments (line {line})", line);
		int ToIntLocal(object o) {
			if (o is float ff) return (int)ff;
			if (o is int ii) return ii;
			throw new ValidationError($"Expected integer argument (line {line})", line);
		}

		int start = 0;
		int stop;
		int step = 1;
		if (args.Length == 1) stop = ToIntLocal(args[0]);
		else {
			start = ToIntLocal(args[0]);
			stop = ToIntLocal(args[1]);
			if (args.Length == 3) step = ToIntLocal(args[2]);
		}

		if (step == 0) throw new ValidationError($"range() step must not be zero (line {line})", line);
		var list = new List<object>();
		if (step > 0) {
			for (int i = start; i < stop; i += step) list.Add((float)i);
		} else {
			for (int i = start; i > stop; i += step) list.Add((float)i);
		}
		return list;
	}

	private static object Len(object[] args, int line) {
		if (args.Length != 1) throw new ValidationError($"len() takes exactly one argument (line {line})", line);
		var obj = args[0];
		if (obj is ICollection coll) return (float)coll.Count;
		if (obj is string s) return (float)s.Length;
		throw new ValidationError($"len() argument must be a list or string (line {line})", line);
	}

	private static object Type(object[] args, int line) {
		if (args.Length != 1) throw new ValidationError($"type() takes exactly one argument (line {line})", line);
		var obj = args[0];
		if (obj == null) return "null";
		if (obj is string) return "string";
		if (obj is float) return "float";
		if (obj is int) return "int";
		if (obj is bool) return "bool";
		if (obj is IEnumerable && obj is not string) return "list";
		return obj.GetType().Name;
	}

	private static object Int(object[] args, int line) {
		if (args.Length != 1) throw new ValidationError($"int() takes exactly one argument (line {line})", line);
		var o = args[0];
		if (o is int i) return i;
		if (o is float f) return (int)f;
		if (o is string s && int.TryParse(s, out var pi)) return pi;
		throw new ValidationError($"int() cannot convert given value to int (line {line})", line);
	}

	private static object Float(object[] args, int line) {
		if (args.Length != 1) throw new ValidationError($"float() takes exactly one argument (line {line})", line);
		var o = args[0];
		if (o is float f) return f;
		if (o is int i) return (float)i;
		if (o is string s && float.TryParse(s, out var pf)) return pf;
		throw new ValidationError($"float() cannot convert given value to float (line {line})", line);
	}

	private static object Str(object[] args, int line) {
		if (args.Length != 1) throw new ValidationError($"str() takes exactly one argument (line {line})", line);
		var o = args[0];
		return o?.ToString() ?? "null";
	}
}
