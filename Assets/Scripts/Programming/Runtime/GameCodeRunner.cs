using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// In-game component to run user code attached to a UI panel.
// Replace previous debug-only CodeTest with this component and wire it to your UI.
public class GameCodeRunner : MonoBehaviour {
	public TMP_InputField codeInput;
	public Button runButton;
	public RobotExecutor executor;

	private void Awake() {
		if (runButton != null) runButton.onClick.AddListener(Run);
	}

	private void OnDestroy() {
		if (runButton != null) runButton.onClick.RemoveListener(Run);
	}

	// Starts execution of the code currently present in the input field.
	public void Run() {
		string code = codeInput != null ? codeInput.text ?? string.Empty : string.Empty;

		// Tokenize and parse
		Lexer lexer = new(code);
		List<Token> tokens;
		try {
			tokens = lexer.Tokenize();
		} catch (System.Exception ex) {
			Debug.LogError($"Lexer error: {ex.Message}");
			return;
		}

		Parser parser = new(tokens);
		List<FunctionDefNode> functions;
		try {
			functions = parser.Parse();
		} catch (System.Exception ex) {
			Debug.LogError($"Parser error: {ex.Message}");
			return;
		}

		// Create interpreter in-scene and set executor reference so builtins enqueue to the robot
		var interpreterObj = new GameObject("AstInterpreter");
		var interpreter = interpreterObj.AddComponent<AstInterpreter>();
		interpreter.Executor = executor;

		CommandExecutionContext.ClearVariables();
		interpreter.StartExecution(functions, "main");
	}

	// Optional helper to stop running interpreter (destroys interpreter GameObject)
	public void StopAllExecution() {
		var interp = GameObject.Find("AstInterpreter");
		if (interp != null) Destroy(interp);
	}
}
