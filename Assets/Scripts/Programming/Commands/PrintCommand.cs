using System.Collections.Generic;
using UnityEngine;

public class PrintCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;

	public bool Tick(params object[] args) {
		int line = CommandExecutionContext.CurrentLine;

		if (args == null || args.Length != 1)
			throw new InvalidArgumentCountError("print", ExpectedArgumentCount, args?.Length ?? 0, line);

		var value = args[0];

		string outStr;
		// If value is a non-string enumerable (list/array), print its elements
		if (value is System.Collections.IEnumerable ie && value is not string) {
			var parts = new List<string>();
			foreach (var el in ie) {
				if (el is System.Collections.IEnumerable inner && el is not string) {
					var innerParts = new List<string>();
					foreach (var iel in inner) innerParts.Add(iel?.ToString() ?? "null");
					parts.Add("[" + string.Join(",", innerParts) + "]");
				} else {
					parts.Add(el?.ToString() ?? "null");
				}
			}
			outStr = "[" + string.Join(",", parts) + "]";
		} else if (value is float f) {
			outStr = f.ToString();
		} else if (value is int i) {
			outStr = i.ToString();
		} else if (value is string s) {
			outStr = s;
		} else if (value == null) {
			outStr = "null";
		} else {
			outStr = value.ToString();
		}

		Debug.Log($"[Print] (line {line}) {outStr}");
		return true;
	}

	public void Reset() { }
}
