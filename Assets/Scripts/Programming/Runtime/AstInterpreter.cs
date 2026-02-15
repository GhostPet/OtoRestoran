using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basit AST yürütücü: sadece statement olarak CallNode (fonksiyon/komut çağrıları) destekler.
// Komutlar tick-tabanlı olduğu için coroutine ile frame-by-frame yürütme yapar.
public class AstInterpreter : MonoBehaviour {
	public List<FunctionDefNode> Functions;

	// Başlatmak için çağırın: interpreter.StartExecution(functions);
	public void StartExecution(List<FunctionDefNode> functions, string entryFunctionName = "main") {
		Functions = functions;
		Debug.Log($"[AstInterpreter] Starting execution of function '{entryFunctionName}'");
		StartCoroutine(RunFunction(entryFunctionName));
	}

	private IEnumerator RunFunction(string name) {
		var fn = Functions?.Find(f => f.Name == name);
		if (fn == null) {
			Debug.LogError($"[AstInterpreter] Function '{name}' not found.");
			yield break;
		}

		for (int i = 0; i < fn.Body.Count; i++) {
			var stmt = fn.Body[i];
			Debug.Log($"[AstInterpreter] Executing statement {i} type={stmt.GetType().Name} line={GetLine(stmt)}");
			if (stmt is CallNode call) {
				yield return StartCoroutine(ExecuteCall(call));
			} else if (stmt is AssignmentNode an) {
				// assignment
				int line = GetLine(an);
				object val = null;
				try { val = EvaluateExpression(an.Value); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); continue; }
				CommandExecutionContext.SetVariable(an.VariableName, val);
				Debug.Log($"[AstInterpreter] Assigned {an.VariableName} = {val} (line {line})");
			} else if (stmt is IfNode ifn) {
				// Evaluate condition (very simple truthiness)
				object condVal = null;
				try {
					condVal = EvaluateExpression(ifn.Condition);
				} catch (ValidationError vex) {
					Debug.LogError(vex.ToString());
					continue;
				}

				bool cond = false;
				if (condVal is bool b) cond = b;
				else if (condVal is float f) cond = f != 0f;
				else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

				var block = cond ? ifn.ThenBlock : ifn.ElseBlock;
				if (block != null) {
					// Execute block statements sequentially
					for (int j = 0; j < block.Statements.Count; j++) {
						var bs = block.Statements[j];
						Debug.Log($"[AstInterpreter] Executing if-block stmt type={bs.GetType().Name} line={GetLine(bs)}");
					if (bs is CallNode c) {
						yield return StartCoroutine(ExecuteCall(c));
					} else if (bs is AssignmentNode an_if) {
						object v_if = null; try { v_if = EvaluateExpression(an_if.Value); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); continue; }
						CommandExecutionContext.SetVariable(an_if.VariableName, v_if);
						Debug.Log($"[AstInterpreter] Assigned {an_if.VariableName} = {v_if} (line {GetLine(an_if)})");
					} else {
						Debug.LogWarning($"[AstInterpreter] Unsupported stmt in if-block: {bs.GetType().Name}");
					}
					}
				}
			} else if (stmt is WhileNode wn) {
				// while loop
				while (true) {
					object condVal;
					try { condVal = EvaluateExpression(wn.Condition); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); break; }
					bool cond = false;
					if (condVal is bool b) cond = b;
					else if (condVal is float f) cond = f != 0f;
					else if (condVal is string s) cond = !string.IsNullOrEmpty(s);

					if (!cond) break;
					// execute body
					for (int j = 0; j < wn.Body.Statements.Count; j++) {
						var bs = wn.Body.Statements[j];
						if (bs is CallNode c) yield return StartCoroutine(ExecuteCall(c));
						else if (bs is AssignmentNode an2) {
							object v = null; try { v = EvaluateExpression(an2.Value); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); continue; }
							CommandExecutionContext.SetVariable(an2.VariableName, v);
							Debug.Log($"[AstInterpreter] Assigned {an2.VariableName} = {v} (line {GetLine(an2)})");
						} else {
							Debug.LogWarning($"[AstInterpreter] Unsupported stmt in while-body: {bs.GetType().Name}");
						}
					}
					yield return null; // allow frames between iterations
				}
			} else if (stmt is ForNode fnn) {
				// for iterator in iterable
				object iterableVal = null; try { iterableVal = EvaluateExpression(fnn.Iterable); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); continue; }
				if (iterableVal is System.Collections.IEnumerable ie) {
					foreach (var item in ie) {
						CommandExecutionContext.SetVariable(fnn.IteratorName, item);
						// execute body
						for (int j = 0; j < fnn.Body.Statements.Count; j++) {
							var bs = fnn.Body.Statements[j];
							if (bs is CallNode c) yield return StartCoroutine(ExecuteCall(c));
							else if (bs is AssignmentNode an3) {
								object v2 = null; try { v2 = EvaluateExpression(an3.Value); } catch (ValidationError vex) { Debug.LogError(vex.ToString()); continue; }
								CommandExecutionContext.SetVariable(an3.VariableName, v2);
								Debug.Log($"[AstInterpreter] Assigned {an3.VariableName} = {v2} (line {GetLine(an3)})");
							} else {
								Debug.LogWarning($"[AstInterpreter] Unsupported stmt in for-body: {bs.GetType().Name}");
							}
						}
						yield return null;
					}
				} else {
					Debug.LogWarning($"[AstInterpreter] For iterable not enumerable: {iterableVal}");
				}
			} else {
				Debug.LogWarning($"[AstInterpreter] Unsupported statement type: {stmt.GetType().Name} (line {GetLine(stmt)})");
			}
		}
	}

	private IEnumerator ExecuteCall(CallNode call) {
		Debug.Log($"[AstInterpreter] ExecuteCall: {call.FunctionName} at line {call.Line}, args={call.Arguments.Count}");
		// Argümanları değerlendirme
		var runtimeArgs = new List<object>();
		foreach (var argExpr in call.Arguments) {
			runtimeArgs.Add(EvaluateExpression(argExpr));
		}

		// Komutu bul
		if (BuiltinCommandRegistry.IsBuiltin(call.FunctionName)) {
			IRobotCommand command = BuiltinCommandRegistry.GetCommand(call.FunctionName);
			command.Reset();

			// Set the current execution line so commands can read it
			CommandExecutionContext.CurrentLine = call.Line;

			while (true) {
				bool done;
				try {
					done = command.Tick(runtimeArgs.ToArray());
				} catch (ValidationError vex) {
					// Eğer komut ValidationError fırlatırsa satır bilgisini göster
					Debug.LogError(vex.ToString());
					yield break;
				} catch (System.Exception ex) {
					Debug.LogError($"[AstInterpreter] Runtime error in '{call.FunctionName}' (line {call.Line}): {ex.Message}");
					yield break;
				}

				if (done) break;
				yield return null; // bir sonraki frame
			}

			// Clear current line after finishing
			CommandExecutionContext.CurrentLine = -1;

			yield break;
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
				return id.Name;
			case ListLiteralExpression list:
				// Beklenen: move_to((0,0,5)) gibi; elemanlar sayı ise List<float>
				var floats = new List<float>();
				foreach (var el in list.Elements) {
					var val = EvaluateExpression(el);
					if (val is float f) floats.Add(f);
					else if (val is double d) floats.Add((float)d);
					else if (val is int i) floats.Add(i);
					else {
						// Uygun olmayan liste elemanı
						throw new ValidationError($"Invalid list element in tuple at line {GetLine(list)}", GetLine(list));
					}
				}
				return floats;

			case BinaryExpression bin:
				var l = EvaluateExpression(bin.Left);
				var r = EvaluateExpression(bin.Right);
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
						if (l is float lf && r is float rf) return lf + rf;
						if (l is string ls && r is string rs) return ls + rs;
						break;
					case TokenType.Minus:
						if (l is float lf2 && r is float rf2) return lf2 - rf2;
						break;
					case TokenType.Star:
						if (l is float lf3 && r is float rf3) return lf3 * rf3;
						break;
					case TokenType.Slash:
						if (l is float lf4 && r is float rf4) return lf4 / rf4;
						break;
				}
				throw new ValidationError($"Unsupported binary operation or operand types at line {GetLine(bin)}", GetLine(bin));
			case CallNode callExpr:
				// İç içe çağrılar desteklenecekse burada çağrıyı çalıştırıp bir sonuç döndürebilirsiniz.
				// Basit durumda, nested call'ları identifier/arg olarak kullanmıyoruz → hata:
				throw new ValidationError($"Nested call expressions not supported here (line {GetLine(callExpr)})", GetLine(callExpr));
			default:
				throw new ValidationError($"Unsupported expression type {expr?.GetType().Name}", GetLine(expr));
		}
	}

	private int GetLine(AstNode n) => n != null ? n.Line : -1;
}
