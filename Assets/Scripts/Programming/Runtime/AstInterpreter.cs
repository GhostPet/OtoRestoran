using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Basit AST yürütücü: sadece statement olarak CallNode (fonksiyon/komut çağrıları) destekler.
// Komutlar tick-tabanlı olduğu için coroutine ile frame-by-frame yürütme yapar.
public class AstInterpreter : MonoBehaviour {
	public List<FunctionDefNode> Functions;
	public event System.Action<AstInterpreter> ExecutionFinished;
	// If set, builtin commands will be enqueued to this executor instead of run inline
	public RobotExecutor Executor;
	private const float TableInteractionDistance = 1.75f;
	// Assigned unique context id for this interpreter so multiple interpreters
	// can run concurrently without sharing variables/state.
	public string ContextId { get; private set; }

	// Başlatmak için çağırın: interpreter.StartExecution(functions);
	public void StartExecution(List<FunctionDefNode> functions, string entryFunctionName = "main") {
		Functions = functions;
		ContextId = System.Guid.NewGuid().ToString();
		// Ensure context exists and clear any previous context state
		CommandExecutionContext.ClearVariables(ContextId);
		StartCoroutine(RunExecution(entryFunctionName));
	}

	private IEnumerator RunExecution(string entryFunctionName) {
		yield return StartCoroutine(RunFunction(entryFunctionName));
		FinishExecution();
	}

	private void FinishExecution() {
		if (!string.IsNullOrEmpty(ContextId)) {
			CommandExecutionContext.ClearVariables(ContextId);
		}
		ExecutionFinished?.Invoke(this);
		Destroy(gameObject);
	}

	// Helper to apply assignments (handles multiple targets and indexed assignments)
	private void ApplyAssignment(AssignmentNode an) {
		int line = GetLine(an);
		object val = EvaluateExpression(an.Value);

		if (an.Targets.Count == 1) {
			var tgt = an.Targets[0];
			if (tgt is IdentifierExpression idt) {
				// Single target: assign the whole RHS value (do not unpack lists)
				CommandExecutionContext.SetVariable(idt.Name, val);
				return;
			}
			if (tgt is IndexExpression ie) {
				var baseName = ((ie.Target as IdentifierExpression)?.Name) ?? throw new ValidationError($"Unsupported indexed target (line {line})", line);
				if (!CommandExecutionContext.TryGetVariable(baseName, out var container)) throw new ValidationError($"Unknown variable '{baseName}' for indexed assignment (line {line})", line);
				if (container is IList list) {
					object idxVal = EvaluateExpression(ie.Index);
					int idx = idxVal is float ff ? (int)ff : idxVal is int ii ? ii : -1;
					if (idx < 0 || idx >= list.Count) throw new ValidationError($"Index out of range for '{baseName}' (line {line})", line);
					list[idx] = val;
					Debug.Log($"[AstInterpreter] Assigned {baseName}[{idx}] = {val} (line {line})");
					return;
				}
				throw new ValidationError($"Cannot index assign to non-list variable '{baseName}' (line {line})", line);
			}
			throw new ValidationError($"Unsupported assignment target (line {line})", line);
		}

		// multiple targets: RHS must be a list/tuple with same length
		if (val is not List<object> values) throw new ValidationError($"Right-hand side must be a list for multiple assignment (line {line})", line);
		if (values.Count != an.Targets.Count) throw new ValidationError($"Mismatch in multiple assignment target/value count (line {line})", line);
		for (int ti = 0; ti < an.Targets.Count; ti++) {
			var tgt = an.Targets[ti];
			var v = values[ti];
			if (tgt is IdentifierExpression idt2) {
				CommandExecutionContext.SetVariable(idt2.Name, v);
				continue;
			}
			if (tgt is IndexExpression ie2) {
				var baseName = ((ie2.Target as IdentifierExpression)?.Name) ?? throw new ValidationError($"Unsupported indexed target (line {line})", line);
				if (!CommandExecutionContext.TryGetVariable(baseName, out var container2)) throw new ValidationError($"Unknown variable '{baseName}' for indexed assignment (line {line})", line);
				if (container2 is System.Collections.IList list2) {
					object idxVal = EvaluateExpression(ie2.Index);
					int idx = idxVal is float ff ? (int)ff : idxVal is int ii ? ii : -1;
					if (idx < 0 || idx >= list2.Count) throw new ValidationError($"Index out of range for '{baseName}' (line {line})", line);
					list2[idx] = v;
					Debug.Log($"[AstInterpreter] Assigned {baseName}[{idx}] = {v} (line {line})");
					continue;
				}
				throw new ValidationError($"Cannot index assign to non-list variable '{baseName}' (line {line})", line);
			}
			throw new ValidationError($"Unsupported assignment target (line {line})", line);
		}
	}

	private IEnumerator RunFunction(string name) {
	var fn = Functions?.Find(f => f.Name == name);
	if (fn == null) {
		Debug.LogError($"[AstInterpreter] Function '{name}' not found.");
		yield break;
	}

		for (int i = 0; i < fn.Body.Count; i++) {
			var stmt = fn.Body[i];
			// Delegate handling to ExecuteStatement which handles nested blocks properly
			yield return StartCoroutine(ExecuteStatement(stmt));
		}
}

// Executes a single AST statement, handling nested control flow and calls.
private IEnumerator ExecuteStatement(AstNode stmt) {
	if (stmt is CallNode call) {
		yield return StartCoroutine(ExecuteCall(call));
		yield break;
	} else if (stmt is InvocationExpression invStmt) {
		try {
			EvaluateExpression(invStmt);
		} catch (ValidationError vex) {
			Debug.LogError(vex.ToString());
		}
		yield break;
	} else if (stmt is ExpressionNode exprStmt) {
		try {
			EvaluateExpression(exprStmt);
		} catch (ValidationError vex) {
			Debug.LogError(vex.ToString());
		}
		yield break;
	} else if (stmt is AssignmentNode an) {
		try { ApplyAssignment(an); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); }
		yield break;
	} else if (stmt is IfNode ifn) {
		object condVal;
		try { condVal = EvaluateExpression(ifn.Condition); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); yield break; }

		bool cond = false;
		if (condVal is bool b) cond = b;
		else if (condVal is float f) cond = f != 0f;
		else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

		var block = cond ? ifn.ThenBlock : ifn.ElseBlock;
		if (block != null) {
			for (int j = 0; j < block.Statements.Count; j++) {
				var bs = block.Statements[j];
				yield return StartCoroutine(ExecuteStatement(bs));
			}
		}
		yield break;
	} else if (stmt is WhileNode wn) {
		while (true) {
			object condVal;
			try { condVal = EvaluateExpression(wn.Condition); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); break; }
			bool cond = false;
			if (condVal is bool b) cond = b;
			else if (condVal is float f) cond = f != 0f;
			else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

			if (!cond) break;
			for (int j = 0; j < wn.Body.Statements.Count; j++) {
				var bs = wn.Body.Statements[j]; // Keeping the variable assignment for consistency
				yield return StartCoroutine(ExecuteStatement(bs)); // Keeping the yield for consistency
			}
			yield return null; // allow frames between iterations
		}
		yield break;
	} else if (stmt is ForNode fnn) {
		object iterableVal;
		try { iterableVal = EvaluateExpression(fnn.Iterable); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); yield break; }
		if (iterableVal is IEnumerable ie) {
			foreach (var item in ie) {
				CommandExecutionContext.SetVariable(fnn.IteratorName, item);
				for (int j = 0; j < fnn.Body.Statements.Count; j++) {
					var bs = fnn.Body.Statements[j];
					yield return StartCoroutine(ExecuteStatement(bs));
				}
				yield return null;
			}
		} else {
			// iterable not enumerable: nothing to log
		}
		yield break;
		} else {
			// unsupported statement type: nothing to log
			yield break;
		}
}

private IEnumerator ExecuteCall(CallNode call) {
		// ExecuteCall invoked
	// Argümanları değerlendirme
	var runtimeArgs = new List<object>();
	foreach (var argExpr in call.Arguments) {
		runtimeArgs.Add(EvaluateExpression(argExpr));
	}

	// Komutu bul
	if (BuiltinCommandRegistry.IsBuiltin(call.FunctionName)) {
		IRobotCommand command = BuiltinCommandRegistry.GetCommand(call.FunctionName);
		command.Reset();

		if (Executor != null) {
			// Enqueue to executor and wait for completion
			var enq = new EnqueuedCommand(command, runtimeArgs.ToArray(), call.Line, ContextId);
			Executor.Enqueue(enq);

			// wait until executor runs and completes this command
			while (!enq.IsCompleted) yield return null;
			yield break;
		} else {
			// fallback to inline execution (existing behavior)
			// Use this interpreter's context for inline execution
			var prevContext = CommandExecutionContext.CurrentContextId;
			CommandExecutionContext.CurrentContextId = ContextId;
			CommandExecutionContext.CurrentLine = call.Line;

			while (true) {
				bool done;
				try {
					done = command.Tick(runtimeArgs.ToArray());
				} catch (ValidationError vex) {
					Debug.LogError(vex.ToString());
					yield break;
				} catch (System.Exception ex) {
					Debug.LogError($"[AstInterpreter] Runtime error in '{call.FunctionName}' (line {call.Line}): {ex.Message}");
					yield break;
				}

				if (done) break;
				yield return null;
			}
			CommandExecutionContext.CurrentLine = -1;
			CommandExecutionContext.CurrentContextId = prevContext;
			yield break;
		}
	} else {
		// Not a builtin: try to find a user-defined function
		var fn = Functions?.Find(f => f.Name == call.FunctionName);
		if (fn == null) {
			Debug.LogError($"[AstInterpreter] Unknown command or function '{call.FunctionName}' (line {call.Line})");
			yield break;
		}

		// Set current line for the call then execute the function body
		CommandExecutionContext.CurrentLine = call.Line;
		yield return StartCoroutine(RunFunction(call.FunctionName));
		CommandExecutionContext.CurrentLine = -1;
		yield break;
	}
}

private object EvaluateExpression(ExpressionNode expr) {
	switch (expr) {
		case NumberLiteralExpression n:
			return n.Value;
		case StringLiteralExpression s:
			return s.Value;
		case IdentifierExpression id:
			// Try variable lookup first
			if (CommandExecutionContext.TryGetVariable(id.Name, out var v)) return v;
			throw new ValidationError($"Unknown variable '{id.Name}' (line {GetLine(id)})", GetLine(id));
		case ListLiteralExpression list:
			// Beklenen: move_to((0,0,5)) gibi; elemanlar sayı ise List<float>
			var evaluated = new List<object>();
			foreach (var el in list.Elements) {
				var val = EvaluateExpression(el);
				evaluated.Add(val);
			}
			return evaluated;

		case BinaryExpression bin: // Updated binary expression handling
			var l = EvaluateExpression(bin.Left);
			var r = EvaluateExpression(bin.Right);
			// helper to coerce ints to floats
			// If either side is integer we convert to float for arithmetic
			if (l is int li) l = (float)li;
			if (r is int ri) r = (float)ri;

			switch (bin.Operator) {
				case TokenType.Greater:
					if (l is float lg && r is float rg) return lg > rg;
					break;
				case TokenType.Less:
					if (l is float ll && r is float rl) return ll < rl;
					break;
				case TokenType.GreaterEqual:
					if (l is float lge && r is float rge) return lge >= rge;
					break;
				case TokenType.LessEqual:
					if (l is float lle && r is float rle) return lle <= rle;
					break;
				case TokenType.EqualEqual:
					return Equals(l, r);
				case TokenType.NotEqual:
					return !Equals(l, r);
				case TokenType.Plus:
					// Support list concatenation and arithmetic
					if (l is float lf && r is float rf) return lf + rf;
					if (l is string ls && r is string rs) return ls + rs;
					if (l is List<object> la && r is List<object> ra) {
						var res = new List<object>(la);
						res.AddRange(ra);
						return res;
					}
					break;
				case TokenType.Minus:
					if (l is float lf2 && r is float rf2) return lf2 - rf2;
					break;
				case TokenType.Star:
					if (l is float lf3 && r is float rf3) return lf3 * rf3;
					// list repetition: [1,2] * 2 => [1,2,1,2]
					if (l is List<object> la2 && r is float rfMult) {
						int times = (int)rfMult;
						var res = new List<object>();
						for (int i = 0; i < times; i++) res.AddRange(la2);
						return res;
					}
					break;
				case TokenType.Slash:
					if (l is float lf4 && r is float rf4) return lf4 / rf4;
					break;
			}
			throw new ValidationError($"Unsupported binary operation or operand types at line {GetLine(bin)}", GetLine(bin));
		case CallNode callExpr:
			// Delegate built-in call handling to BuiltinFunctions
			var name = callExpr.FunctionName;
			var evaluatedArgs = new List<object>();
			foreach (var a in callExpr.Arguments) evaluatedArgs.Add(EvaluateExpression(a));
			// Use the new BuiltinFunctions helper for expression-level builtins
			if (BuiltinFunctions.IsBuiltin(name)) {
				return BuiltinFunctions.Invoke(name, evaluatedArgs.ToArray(), GetLine(callExpr));
			}
			throw new ValidationError($"Unsupported call expression '{name}' (line {GetLine(callExpr)})", GetLine(callExpr));

		case MemberAccessExpression memberExpr:
			return EvaluateMemberAccess(memberExpr);

		case InvocationExpression invocationExpr:
			return EvaluateInvocation(invocationExpr);

		case IndexExpression idxExpr:
			var target = EvaluateExpression(idxExpr.Target);
			var indexVal = EvaluateExpression(idxExpr.Index);
			int ii;
			if (indexVal is float fidx) ii = (int)fidx;
			else if (indexVal is int iidx) ii = iidx;
			else throw new ValidationError($"Index must be integer (line {GetLine(idxExpr)})", GetLine(idxExpr));

			// Accept any non-string IList (List<object>, List<float>, arrays, etc.)
			if (target is System.Collections.IList listTarget) {
				if (ii < 0 || ii >= listTarget.Count) throw new ValidationError($"Index out of range (line {GetLine(idxExpr)})", GetLine(idxExpr));
				return listTarget[ii];
			}
			throw new ValidationError($"Cannot index non-list value (line {GetLine(idxExpr)})", GetLine(idxExpr));
		default:
			throw new ValidationError($"Unsupported expression type {expr?.GetType().Name}", GetLine(expr));
	}
}

private object EvaluateMemberAccess(MemberAccessExpression memberExpr) {
	var target = EvaluateExpression(memberExpr.Target) ?? throw new ValidationError($"Cannot access member '{memberExpr.MemberName}' of null (line {GetLine(memberExpr)})", GetLine(memberExpr));
	var type = target.GetType();

	var prop = type.GetProperty(memberExpr.MemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
	if (prop != null) {
		return prop.GetValue(target);
	}

	var field = type.GetField(memberExpr.MemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
	if (field != null) {
		return field.GetValue(target);
	}

	// Unity-style convenience mappings for scripting names
	if (target is TableBehavior table && string.Equals(memberExpr.MemberName, "customers", System.StringComparison.OrdinalIgnoreCase)) {
		return table.Customers;
	}
	if (target is Customer customer && string.Equals(memberExpr.MemberName, "table", System.StringComparison.OrdinalIgnoreCase)) {
		return customer.Table;
	}

	throw new ValidationError($"Unknown member '{memberExpr.MemberName}' on type '{type.Name}' (line {GetLine(memberExpr)})", GetLine(memberExpr));
}

private object EvaluateInvocation(InvocationExpression invocationExpr) {
	if (invocationExpr.Target is MemberAccessExpression memberTarget) {
		var instance = EvaluateExpression(memberTarget.Target) ?? throw new ValidationError($"Cannot call method '{memberTarget.MemberName}' on null (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
		var evaluatedArgs = new object[invocationExpr.Arguments.Count];
		for (int i = 0; i < invocationExpr.Arguments.Count; i++) {
			evaluatedArgs[i] = EvaluateExpression(invocationExpr.Arguments[i]);
		}

		if (instance is Customer customer) {
			if (string.Equals(memberTarget.MemberName, "has_order", System.StringComparison.OrdinalIgnoreCase)) {
				if (evaluatedArgs.Length != 0)
					throw new ValidationError($"has_order() takes no arguments (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
				if (!IsRobotNearCustomerTable(customer)) return false;
				return customer.State == CustomerState.Ordering && customer.IsOrderReady && !customer.IsOrderTaken;
			}

			if (string.Equals(memberTarget.MemberName, "get_order", System.StringComparison.OrdinalIgnoreCase)) {
				if (evaluatedArgs.Length != 0)
					throw new ValidationError($"get_order() takes no arguments (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
				if (!IsRobotNearCustomerTable(customer)) return null;
				return customer.GetOrder();
			}
		}

		if (instance is OvenBehavior oven) {
			if (string.Equals(memberTarget.MemberName, "place", System.StringComparison.OrdinalIgnoreCase) ||
				string.Equals(memberTarget.MemberName, "cook", System.StringComparison.OrdinalIgnoreCase) ||
				string.Equals(memberTarget.MemberName, "take", System.StringComparison.OrdinalIgnoreCase) ||
				string.Equals(memberTarget.MemberName, "clean", System.StringComparison.OrdinalIgnoreCase)) {
				if (evaluatedArgs.Length != 0)
					throw new ValidationError($"{memberTarget.MemberName}() takes no arguments (line {GetLine(invocationExpr)})", GetLine(invocationExpr));

				if (!IsRobotNearOven(oven)) return null;

				if (string.Equals(memberTarget.MemberName, "place", System.StringComparison.OrdinalIgnoreCase)) {
					oven.Place();
					return null;
				}

				if (string.Equals(memberTarget.MemberName, "cook", System.StringComparison.OrdinalIgnoreCase)) {
					oven.Cook();
					return null;
				}

				if (string.Equals(memberTarget.MemberName, "take", System.StringComparison.OrdinalIgnoreCase)) {
					return oven.Take();
				}

				oven.Clean();
				return null;
			}
		}

		var methods = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < methods.Length; i++) {
			var m = methods[i];
			if (!string.Equals(m.Name, memberTarget.MemberName, System.StringComparison.OrdinalIgnoreCase)) continue;
			var parameters = m.GetParameters();
			if (parameters.Length != evaluatedArgs.Length) continue;

			try {
				return m.Invoke(instance, evaluatedArgs);
			} catch (System.Exception ex) {
				throw new ValidationError($"Method call '{memberTarget.MemberName}' failed: {ex.Message} (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
			}
		}

		throw new ValidationError($"Unknown method '{memberTarget.MemberName}' on type '{instance.GetType().Name}' (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
	}

	if (invocationExpr.Target is IdentifierExpression idTarget) {
		var evaluatedArgs = new List<object>();
		for (int i = 0; i < invocationExpr.Arguments.Count; i++) {
			evaluatedArgs.Add(EvaluateExpression(invocationExpr.Arguments[i]));
		}

		if (BuiltinFunctions.IsBuiltin(idTarget.Name)) {
			return BuiltinFunctions.Invoke(idTarget.Name, evaluatedArgs.ToArray(), GetLine(invocationExpr));
		}
	}

	throw new ValidationError($"Unsupported invocation target (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
}

private IRobot ResolveCurrentRobotForDsl() {
	var robot = CommandExecutionContext.CurrentRobot;
	if (robot != null) return robot;

	if (Executor != null) {
		if (Executor.TryGetComponent<IRobot>(out var executorRobot)) return executorRobot;
	}

	return null;
}

private bool IsRobotNearCustomerTable(Customer customer) {
	if (customer == null) return false;
	var table = customer.Table;
	if (table == null) return false;

	var robot = ResolveCurrentRobotForDsl();
	if (robot == null) return false;

	var interactionPoint = TargetResolver.Resolve(robot, table);
	var sqrDistance = (robot.Position - interactionPoint).sqrMagnitude;
	return sqrDistance <= TableInteractionDistance * TableInteractionDistance;
}

private bool IsRobotNearOven(OvenBehavior oven) {
	if (oven == null) return false;

	var robot = ResolveCurrentRobotForDsl();
	if (robot == null) return false;

	var sqrDistance = (robot.Position - oven.transform.position).sqrMagnitude;
	return sqrDistance <= TableInteractionDistance * TableInteractionDistance;
}

private int GetLine(AstNode n) => n != null ? n.Line : -1;
}
