using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

// Basit AST yürütücü: sadece statement olarak CallNode (fonksiyon/komut çağrıları) destekler.
// Komutlar tick-tabanlı olduğu için coroutine ile frame-by-frame yürütme yapar.
public class AstInterpreter : MonoBehaviour {
	public List<FunctionDefNode> Functions;
	public event System.Action<AstInterpreter> ExecutionFinished;
	public event System.Action<AstInterpreter, string> ExecutionFailed;
	// If set, builtin commands will be enqueued to this executor instead of run inline
	public RobotExecutor Executor;
	public IDslWorldAdapter WorldAdapter { get; set; }
	// Assigned unique context id for this interpreter so multiple interpreters
	// can run concurrently without sharing variables/state.
	public string ContextId { get; private set; }
	public bool HasRuntimeError { get; private set; }
	public string RuntimeWarningMessage { get; private set; }
	private DslBuiltinRuntime _builtinRuntime;
	private DslExecutionGuard _executionGuard;
	private DslFacadeFactory _facadeFactory;

	// Başlatmak için çağırın: interpreter.StartExecution(functions);
	public void StartExecution(List<FunctionDefNode> functions, string entryFunctionName = "main") {
		Functions = functions;
		ContextId = System.Guid.NewGuid().ToString();
		CommandExecutionContext.ClearVariables(ContextId);
		_facadeFactory = new DslFacadeFactory();
		_builtinRuntime = CreateBuiltinRuntime();
		_executionGuard = new DslExecutionGuard();
		StartCoroutine(RunExecution(entryFunctionName));
	}

	private IEnumerator RunExecution(string entryFunctionName) {
		string previousContextId = CommandExecutionContext.CurrentContextId;
		int previousLine = CommandExecutionContext.CurrentLine;
		IRobot previousRobot = CommandExecutionContext.CurrentRobot;

		CommandExecutionContext.CurrentContextId = ContextId;
		CommandExecutionContext.CurrentLine = -1;
		CommandExecutionContext.CurrentRobot = null;
		InitializeGlobalScope();

		yield return StartCoroutine(RunFunction(entryFunctionName));

		CommandExecutionContext.CurrentRobot = previousRobot;
		CommandExecutionContext.CurrentLine = previousLine;
		CommandExecutionContext.CurrentContextId = previousContextId;
		FinishExecution();
	}

	private void InitializeGlobalScope() {
		if (_builtinRuntime == null) {
			return;
		}

		foreach (KeyValuePair<string, object> entry in _builtinRuntime.CreateGlobalScope()) {
			CommandExecutionContext.SetVariable(entry.Key, entry.Value);
		}
	}

	private DslBuiltinRuntime CreateBuiltinRuntime() {
		IDslWorldAdapter world = WorldAdapter;
		if (world == null) {
			world = Executor != null ? new CurrentGameDslWorldAdapter(Executor) : new NullDslWorldAdapter();
		}

		return new DslBuiltinRuntime(world, new InterpreterOutputSink(), _facadeFactory ?? new DslFacadeFactory());
	}

	private void FinishExecution() {
		if (!string.IsNullOrEmpty(ContextId)) {
			CommandExecutionContext.ClearVariables(ContextId);
		}
		if (HasRuntimeError && !string.IsNullOrWhiteSpace(RuntimeWarningMessage)) {
			ExecutionFailed?.Invoke(this, RuntimeWarningMessage);
		}
		ExecutionFinished?.Invoke(this);
		Destroy(gameObject);
	}

	private void RequestStopWithWarning(string message) {
		if (HasRuntimeError) {
			return;
		}

		HasRuntimeError = true;
		RuntimeWarningMessage = string.IsNullOrWhiteSpace(message)
			? "Kod çalıştırılırken bilinmeyen bir hata oluştu."
			: message;
		CommandExecutionContext.PublishStatusMessage(RuntimeWarningMessage, true);

		if (Executor != null) {
			Executor.CancelAll();
		}
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
					continue;
				}
				throw new ValidationError($"Cannot index assign to non-list variable '{baseName}' (line {line})", line);
			}
			throw new ValidationError($"Unsupported assignment target (line {line})", line);
		}
	}

	private IEnumerator RunFunction(string name) {
		yield return StartCoroutine(RunFunction(name, null, -1));
	}

	private IEnumerator RunFunction(string name, IList<object> arguments, int callLine) {
		if (HasRuntimeError) {
			yield break;
		}

		var fn = Functions?.Find(f => f.Name == name);
		if (fn == null) {
			RequestStopWithWarning($"Function '{name}' not found.");
			yield break;
		}

		if (!TryBindFunctionArguments(fn, arguments, callLine, out List<string> newVariables, out Dictionary<string, object> previousValues, out HashSet<string> overwrittenNames)) {
			yield break;
		}

		try {
			for (int i = 0; i < fn.Body.Count; i++) {
				if (HasRuntimeError) {
					yield break;
				}

				var stmt = fn.Body[i];
				// Delegate handling to ExecuteStatement which handles nested blocks properly
				yield return StartCoroutine(ExecuteStatement(stmt));
			}
		} finally {
			RestoreFunctionArguments(newVariables, previousValues, overwrittenNames);
		}
	}

	// Executes a single AST statement, handling nested control flow and calls.
	private IEnumerator ExecuteStatement(AstNode stmt) {
		if (HasRuntimeError) {
			yield break;
		}

		_executionGuard?.EnterStatement(GetLine(stmt));

		if (stmt is CallNode call) {
			yield return StartCoroutine(ExecuteCall(call));
			yield break;
		} else if (stmt is InvocationExpression invStmt) {
			try {
				EvaluateExpression(invStmt);
			} catch (ValidationError vex) {
				RequestStopWithWarning(vex.ToString());
			} catch (System.Exception ex) {
				RequestStopWithWarning(ex.Message);
			}
			yield break;
		} else if (stmt is ExpressionNode exprStmt) {
			try {
				EvaluateExpression(exprStmt);
			} catch (ValidationError vex) {
				RequestStopWithWarning(vex.ToString());
			} catch (System.Exception ex) {
				RequestStopWithWarning(ex.Message);
			}
			yield break;
		} else if (stmt is AssignmentNode an) {
			try {
				ApplyAssignment(an);
			} catch (ValidationError vex) {
				RequestStopWithWarning(vex.ToString());
			} catch (System.Exception ex) {
				RequestStopWithWarning(ex.Message);
			}
			yield break;
		} else if (stmt is IfNode ifn) {
			if (!TryEvaluateExpression(ifn.Condition, out object condVal)) {
				yield break;
			}

			bool cond = false;
			if (condVal is bool b) cond = b;
			else if (condVal is float f) cond = f != 0f;
			else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

			var block = cond ? ifn.ThenBlock : ifn.ElseBlock;
			if (block != null) {
				for (int j = 0; j < block.Statements.Count; j++) {
					if (HasRuntimeError) {
						yield break;
					}

					var bs = block.Statements[j];
					yield return StartCoroutine(ExecuteStatement(bs));
				}
			}
			yield break;
		} else if (stmt is WhileNode wn) {
			while (true) {
				_executionGuard?.EnterLoopIteration(GetLine(wn));

				if (HasRuntimeError) {
					break;
				}

				if (!TryEvaluateExpression(wn.Condition, out object condVal)) {
					break;
				}
				bool cond = false;
				if (condVal is bool b) cond = b;
				else if (condVal is float f) cond = f != 0f;
				else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

				if (!cond) break;
				for (int j = 0; j < wn.Body.Statements.Count; j++) {
					if (HasRuntimeError) {
						break;
					}

					var bs = wn.Body.Statements[j]; // Keeping the variable assignment for consistency
					yield return StartCoroutine(ExecuteStatement(bs)); // Keeping the yield for consistency
				}
				yield return null; // allow frames between iterations
			}
			yield break;
		} else if (stmt is ForNode fnn) {
			if (!TryEvaluateExpression(fnn.Iterable, out object iterableVal)) {
				yield break;
			}
			if (iterableVal is IEnumerable ie) {
				foreach (var item in ie) {
					_executionGuard?.EnterLoopIteration(GetLine(fnn));

					if (HasRuntimeError) {
						yield break;
					}

					CommandExecutionContext.SetVariable(fnn.IteratorName, item);
					for (int j = 0; j < fnn.Body.Statements.Count; j++) {
						if (HasRuntimeError) {
							break;
						}

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
		if (HasRuntimeError) {
			yield break;
		}

		var runtimeArgs = new List<object>();
		for (int i = 0; i < call.Arguments.Count; i++) {
			if (!TryEvaluateExpression(call.Arguments[i], out object argValue)) {
				yield break;
			}

			runtimeArgs.Add(argValue);
		}

		if (TryInvokeBuiltin(call.FunctionName, runtimeArgs, call.Line, out DslBuiltinInvocation builtinInvocation)) {
			if (builtinInvocation.IsAsync) {
				yield return StartCoroutine(ExecuteBuiltinInvocationAsync(builtinInvocation));
			}
			yield break;
		}

		var fn = Functions?.Find(f => f.Name == call.FunctionName);
		if (fn == null) {
			RequestStopWithWarning($"Unknown function '{call.FunctionName}' (line {call.Line})");
			yield break;
		}

		int previousLine = CommandExecutionContext.CurrentLine;
		CommandExecutionContext.CurrentLine = call.Line;
		yield return StartCoroutine(RunFunction(call.FunctionName, runtimeArgs, call.Line));
		CommandExecutionContext.CurrentLine = previousLine;
	}

	private IEnumerator ExecuteBuiltinInvocationAsync(DslBuiltinInvocation invocation) {
		if (!invocation.IsAsync) {
			yield break;
		}

		while (!invocation.AsyncOperation.Tick(Time.deltaTime)) {
			if (HasRuntimeError) {
				yield break;
			}

			yield return null;
		}
	}

	private bool TryBindFunctionArguments(FunctionDefNode function, IList<object> arguments, int callLine, out List<string> newVariables, out Dictionary<string, object> previousValues, out HashSet<string> overwrittenNames) {
		newVariables = new List<string>();
		previousValues = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
		overwrittenNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

		IList<object> invocationArguments = arguments ?? System.Array.Empty<object>();
		int expectedCount = function != null && function.Parameters != null ? function.Parameters.Count : 0;
		if (invocationArguments.Count != expectedCount) {
			int line = callLine >= 0 ? callLine : (function != null ? function.Line : -1);
			RequestStopWithWarning($"Function '{function?.Name}' expects {expectedCount} argument(s) but got {invocationArguments.Count} (line {line})");
			return false;
		}

		if (function == null || function.Parameters == null) {
			return true;
		}

		for (int i = 0; i < function.Parameters.Count; i++) {
			string parameterName = function.Parameters[i];
			object parameterValue = invocationArguments[i];

			if (!overwrittenNames.Contains(parameterName) && CommandExecutionContext.TryGetVariable(parameterName, out object previousValue)) {
				previousValues[parameterName] = previousValue;
				overwrittenNames.Add(parameterName);
			} else if (!newVariables.Contains(parameterName)) {
				newVariables.Add(parameterName);
			}

			CommandExecutionContext.SetVariable(parameterName, parameterValue);
		}

		return true;
	}

	private void RestoreFunctionArguments(List<string> newVariables, Dictionary<string, object> previousValues, HashSet<string> overwrittenNames) {
		if (newVariables != null) {
			for (int i = 0; i < newVariables.Count; i++) {
				CommandExecutionContext.RemoveVariable(newVariables[i]);
			}
		}

		if (previousValues == null) {
			return;
		}

		foreach (KeyValuePair<string, object> pair in previousValues) {
			CommandExecutionContext.SetVariable(pair.Key, pair.Value);
		}
	}

	private bool TryEvaluateExpression(ExpressionNode expr, out object value) {
		value = null;

		try {
			value = EvaluateExpression(expr);
			return true;
		} catch (ValidationError vex) {
			RequestStopWithWarning(vex.ToString());
			return false;
		} catch (System.Exception ex) {
			RequestStopWithWarning(ex.Message);
			return false;
		}
	}

	private bool TryInvokeBuiltin(string name, IList<object> args, int line, out DslBuiltinInvocation invocation) {
		invocation = default;
		if (_builtinRuntime == null || !_builtinRuntime.IsBuiltin(name)) {
			return false;
		}

		invocation = _builtinRuntime.Invoke(name, args, line);
		return true;
	}

	private object InvokeBuiltinValue(string name, IList<object> args, int line) {
		if (!TryInvokeBuiltin(name, args, line, out DslBuiltinInvocation invocation)) {
			throw new ValidationError($"Unsupported call expression '{name}' (line {line})", line);
		}

		if (invocation.IsAsync) {
			throw new DslRuntimeError($"Builtin '{name}' cannot be used in an expression (line {line})", line);
		}

		return WrapRuntimeValue(invocation.Value);
	}

	private object WrapRuntimeValue(object value) {
		return _facadeFactory != null ? _facadeFactory.WrapUnknown(value) : value;
	}

	private object EvaluateExpression(ExpressionNode expr) {
		switch (expr) {
			case NumberLiteralExpression n:
				return n.Value;
			case StringLiteralExpression s:
				return s.Value;
			case BooleanLiteralExpression b:
				return b.Value;
			case NullLiteralExpression:
				return null;
			case IdentifierExpression id:
				if (CommandExecutionContext.TryGetVariable(id.Name, out var v)) return WrapRuntimeValue(v);
				throw new ValidationError($"Unknown variable '{id.Name}' (line {GetLine(id)})", GetLine(id));
			case ListLiteralExpression list:
				var evaluated = new List<object>();
				foreach (var el in list.Elements) {
					var val = EvaluateExpression(el);
					evaluated.Add(WrapRuntimeValue(val));
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
				var name = callExpr.FunctionName;
				var evaluatedArgs = new List<object>();
				foreach (var a in callExpr.Arguments) evaluatedArgs.Add(EvaluateExpression(a));
				return InvokeBuiltinValue(name, evaluatedArgs, GetLine(callExpr));

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

				if (target is System.Collections.IList listTarget) {
					if (ii < 0 || ii >= listTarget.Count) throw new ValidationError($"Index out of range (line {GetLine(idxExpr)})", GetLine(idxExpr));
					return WrapRuntimeValue(listTarget[ii]);
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
			return WrapRuntimeValue(prop.GetValue(target));
		}

		var field = type.GetField(memberExpr.MemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
		if (field != null) {
			return WrapRuntimeValue(field.GetValue(target));
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

			if (TryResolveMethod(instance, memberTarget.MemberName, evaluatedArgs, out MethodInfo method, out object[] invocationArguments)) {
				try {
					return WrapRuntimeValue(method.Invoke(instance, invocationArguments));
				} catch (System.Exception ex) {
					Exception realException = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
					throw new ValidationError($"Method call '{memberTarget.MemberName}' failed: {realException.Message} (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
				}
			}

			throw new ValidationError($"Unknown method '{memberTarget.MemberName}' on type '{instance.GetType().Name}' (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
		}

		if (invocationExpr.Target is IdentifierExpression idTarget) {
			var evaluatedArgs = new List<object>();
			for (int i = 0; i < invocationExpr.Arguments.Count; i++) {
				evaluatedArgs.Add(EvaluateExpression(invocationExpr.Arguments[i]));
			}

			if (_builtinRuntime != null && _builtinRuntime.IsBuiltin(idTarget.Name)) {
				return InvokeBuiltinValue(idTarget.Name, evaluatedArgs, GetLine(invocationExpr));
			}
		}

		throw new ValidationError($"Unsupported invocation target (line {GetLine(invocationExpr)})", GetLine(invocationExpr));
	}

	private static bool TryResolveMethod(object instance, string methodName, object[] args, out MethodInfo method, out object[] invocationArguments) {
		method = null;
		invocationArguments = null;
		if (instance == null || string.IsNullOrWhiteSpace(methodName)) {
			return false;
		}

		MethodInfo[] methods = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
		int bestScore = int.MinValue;
		for (int i = 0; i < methods.Length; i++) {
			MethodInfo candidate = methods[i];
			if (!string.Equals(candidate.Name, methodName, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!TryBuildInvocationArguments(candidate, args, out object[] candidateArguments, out int score)) {
				continue;
			}

			if (score > bestScore) {
				bestScore = score;
				method = candidate;
				invocationArguments = candidateArguments;
			}
		}

		return method != null;
	}

	private static bool TryBuildInvocationArguments(MethodInfo method, object[] args, out object[] invocationArguments, out int score) {
		invocationArguments = null;
		score = int.MinValue;
		ParameterInfo[] parameters = method.GetParameters();
		int argumentCount = args != null ? args.Length : 0;
		if (argumentCount > parameters.Length) {
			return false;
		}

		var resolvedArguments = new object[parameters.Length];
		int totalScore = parameters.Length == argumentCount ? 100 : 0;
		for (int i = 0; i < parameters.Length; i++) {
			ParameterInfo parameter = parameters[i];
			if (i < argumentCount) {
				if (!TryConvertArgument(args[i], parameter.ParameterType, out object convertedArgument, out int parameterScore)) {
					return false;
				}

				resolvedArguments[i] = convertedArgument;
				totalScore += parameterScore;
				continue;
			}

			if (!parameter.IsOptional) {
				return false;
			}

			resolvedArguments[i] = parameter.DefaultValue;
		}

		invocationArguments = resolvedArguments;
		score = totalScore;
		return true;
	}

	private static bool TryConvertArgument(object value, Type targetType, out object convertedValue, out int score) {
		convertedValue = value;
		score = 0;
		if (targetType == typeof(object)) {
			score = 1;
			return true;
		}

		if (value == null) {
			if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null) {
				score = 2;
				return true;
			}

			return false;
		}

		Type actualType = value.GetType();
		if (targetType.IsAssignableFrom(actualType)) {
			score = targetType == actualType ? 10 : 8;
			return true;
		}

		Type nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
		if (nullableUnderlyingType != null) {
			if (TryConvertArgument(value, nullableUnderlyingType, out object nullableValue, out int nullableScore)) {
				convertedValue = nullableValue;
				score = nullableScore;
				return true;
			}
		}

		if (targetType == typeof(float)) {
			if (value is int integerValue) {
				convertedValue = (float)integerValue;
				score = 6;
				return true;
			}
		}

		if (targetType == typeof(int)) {
			if (value is float floatValue) {
				int integerValue = (int)floatValue;
				if (Mathf.Approximately(floatValue, integerValue)) {
					convertedValue = integerValue;
					score = 6;
					return true;
				}
			}
		}

		try {
			convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
			score = 4;
			return true;
		} catch {
			return false;
		}
	}

	private int GetLine(AstNode n) => n != null ? n.Line : -1;

	private sealed class InterpreterOutputSink : IDslOutputSink {
		public void Write(string message, bool isError = false) {
			CommandExecutionContext.PublishStatusMessage(message, isError);
		}
	}
}
