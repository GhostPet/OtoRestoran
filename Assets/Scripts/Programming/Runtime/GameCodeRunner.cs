using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// In-game component to run user code attached to a UI panel.
// Replace previous debug-only CodeTest with this component and wire it to your UI.
public class GameCodeRunner : MonoBehaviour {
	public List<TMP_InputField> codeInputs;
	public List<Button> runButtons;
	public List<RobotExecutor> executors;
	[SerializeField] private bool executionEnabled = true;

	// Track active interpreters per executor so stopping/starting one does not
	// affect others.
	private readonly Dictionary<RobotExecutor, AstInterpreter> _interpreters =
		new(EqualityComparer<RobotExecutor>.Default);
	private AstInterpreter _fallbackInterpreter;
	private int _fallbackInterpreterIndex = -1;

	private readonly Dictionary<Button, UnityAction> _buttonListeners
		= new(EqualityComparer<Button>.Default);
	private readonly Dictionary<RobotExecutor, int> _editorIndices
		= new(EqualityComparer<RobotExecutor>.Default);

	public bool ExecutionEnabled => executionEnabled;

	private void OnDestroy() {
		// Remove listeners added via RegisterEditor
		foreach (var kv in _buttonListeners) {
			var btn = kv.Key;
			var act = kv.Value;
			if (btn != null && act != null) btn.onClick.RemoveListener(act);
		}
		_buttonListeners.Clear();
	}

	// Called by UI code when a new editor window is created. Wires the run button to
	// start/stop the interpreter for the given index mapping.
	public void RegisterEditor(TMP_InputField input, Button runButton, RobotExecutor executor) {
		codeInputs ??= new List<TMP_InputField>();
		runButtons ??= new List<Button>();
		executors ??= new List<RobotExecutor>();

		codeInputs.Add(input);
		runButtons.Add(runButton);
		executors.Add(executor);

		int index = codeInputs.Count - 1;
		if (executor != null) {
			_editorIndices[executor] = index;
		}

		void action() => ToggleRunForIndex(index);
		if (runButton != null) {
			runButton.onClick.AddListener(action);
			_buttonListeners[runButton] = action;
			UpdateButtonLabel(index, "Run");
		}

		RefreshRunButtonStates();
	}

	public void ToggleRunForIndex(int index) {
		if (!executionEnabled) {
			Debug.LogWarning("GameCodeRunner is disabled because the restaurant is closed.");
			return;
		}

		// Validate index
		if (codeInputs == null || index < 0 || index >= codeInputs.Count) {
			Debug.LogWarning($"ToggleRunForIndex: invalid index {index}");
			return;
		}

		// If there is an executor for this index, use per-executor interpreter
		RobotExecutor targetExecutor = (executors != null && index < executors.Count) ? executors[index] : null;

		// If already running for this executor/index, stop it
		if (targetExecutor != null && _interpreters.ContainsKey(targetExecutor)) {
			StopExecutionFor(targetExecutor);
			UpdateButtonLabel(index, "Run");
			return;
		}

		// Otherwise start this editor: stop all other interpreters first
		StopAllExecution();

		string code = codeInputs[index] != null ? codeInputs[index].text ?? string.Empty : string.Empty;
		TryStartExecution(index, code);
	}

	public bool TryRunCode(RobotExecutor executor, string code) {
		if (!executionEnabled) {
			Debug.LogWarning("GameCodeRunner is disabled because the restaurant is closed.");
			return false;
		}

		if (executor == null) {
			Debug.LogWarning("TryRunCode: executor is null.");
			return false;
		}

		int index = -1;
		if (_editorIndices.TryGetValue(executor, out int mappedIndex)) {
			index = mappedIndex;
		} else if (executors != null) {
			index = executors.IndexOf(executor);
		}

		if (index < 0) {
			Debug.LogWarning("TryRunCode: executor için kayıtlı editör bulunamadı.");
			return false;
		}

		if (codeInputs != null && index < codeInputs.Count && codeInputs[index] != null) {
			codeInputs[index].text = code ?? string.Empty;
		}

		StopAllExecution();
		return TryStartExecution(index, code ?? string.Empty);
	}

	private bool TryStartExecution(int index, string code) {
		// Tokenize and parse
		Lexer lexer = new(code);
		List<Token> tokens;
		try {
			tokens = lexer.Tokenize();
		} catch (System.Exception ex) {
			Debug.LogWarning($"Lexer error for editor {index}: {ex.Message}");
			return false;
		}

		Parser parser = new(tokens);
		List<FunctionDefNode> functions;
		try {
			functions = parser.Parse();
		} catch (System.Exception ex) {
			Debug.LogWarning($"Parser error for editor {index}: {ex.Message}");
			return false;
		}

		RobotExecutor targetExecutor = (executors != null && index < executors.Count) ? executors[index] : null;

		if (targetExecutor != null) {
			var interpObj = new GameObject($"AstInterpreter_{targetExecutor.name}");
			var interp = interpObj.AddComponent<AstInterpreter>();
			interp.Executor = targetExecutor;
			interp.ExecutionFinished += OnInterpreterFinished;
			interp.ExecutionFailed += OnInterpreterFailed;
			_interpreters[targetExecutor] = interp;
			interp.StartExecution(functions, "main");
		} else {
			// Fallback: inline interpreter for this editor
			var fallbackObj = new GameObject($"AstInterpreter_editor_{index}");
			var fallbackInterp = fallbackObj.AddComponent<AstInterpreter>();
			fallbackInterp.ExecutionFinished += OnInterpreterFinished;
			fallbackInterp.ExecutionFailed += OnInterpreterFailed;
			_fallbackInterpreter = fallbackInterp;
			_fallbackInterpreterIndex = index;
			fallbackInterp.StartExecution(functions, "main");
		}

		UpdateButtonLabel(index, "Stop");
		return true;
	}

	private void UpdateButtonLabel(int index, string label) {
		if (runButtons == null || index < 0 || index >= runButtons.Count) return;
		var btn = runButtons[index];
		if (btn == null) return;
		// Try to update TMP text if present
		var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
		if (tmp != null) tmp.text = label;
		else {
			var txt = btn.GetComponentInChildren<UnityEngine.UI.Text>();
			if (txt != null) txt.text = label;
		}
	}

	// Optional helper to stop running interpreters (destroys interpreter GameObjects)
	public void StopAllExecution() {
		// Destroy all tracked interpreters and clear their contexts individually.
		foreach (var kv in _interpreters) {
			var interp = kv.Value;
			if (interp != null) {
				interp.ExecutionFinished -= OnInterpreterFinished;
				interp.ExecutionFailed -= OnInterpreterFailed;
				if (!string.IsNullOrEmpty(interp.ContextId)) CommandExecutionContext.ClearVariables(interp.ContextId);
				Destroy(interp.gameObject);
			}
		}
		_interpreters.Clear();

		if (_fallbackInterpreter != null) {
			_fallbackInterpreter.ExecutionFinished -= OnInterpreterFinished;
			_fallbackInterpreter.ExecutionFailed -= OnInterpreterFailed;
			if (!string.IsNullOrEmpty(_fallbackInterpreter.ContextId)) CommandExecutionContext.ClearVariables(_fallbackInterpreter.ContextId);
			Destroy(_fallbackInterpreter.gameObject);
			_fallbackInterpreter = null;
			_fallbackInterpreterIndex = -1;
		}

		RefreshRunButtonStates();
	}

	public void SetExecutionEnabled(bool enabled) {
		executionEnabled = enabled;
		if (!executionEnabled) {
			StopAllExecution();
		}

		RefreshRunButtonStates();
	}

	public void SetExecutors(List<RobotExecutor> newExecutors) {
		executors ??= new List<RobotExecutor>();
		executors.Clear();

		if (newExecutors != null) {
			for (int i = 0; i < newExecutors.Count; i++) {
				if (newExecutors[i] != null) executors.Add(newExecutors[i]);
			}
		}

		RefreshRunButtonStates();
	}

	private void RefreshRunButtonStates() {
		if (runButtons == null) return;

		for (int i = 0; i < runButtons.Count; i++) {
			var button = runButtons[i];
			if (button == null) continue;
			button.interactable = executionEnabled;
			if (!executionEnabled) UpdateButtonLabel(i, "Run");
		}
	}

	// Stop execution only for a single executor; does not affect others.
	public void StopExecutionFor(RobotExecutor executor) {
		if (executor == null) return;
		if (_interpreters.TryGetValue(executor, out var interp)) {
			interp.ExecutionFinished -= OnInterpreterFinished;
			interp.ExecutionFailed -= OnInterpreterFailed;
			if (!string.IsNullOrEmpty(interp.ContextId)) CommandExecutionContext.ClearVariables(interp.ContextId);
			if (interp != null) Destroy(interp.gameObject);
			_interpreters.Remove(executor);
		}
	}

	private void OnInterpreterFinished(AstInterpreter finishedInterpreter) {
		if (finishedInterpreter == null) return;

		RobotExecutor matchedExecutor = null;
		foreach (var kv in _interpreters) {
			if (kv.Value == finishedInterpreter) {
				matchedExecutor = kv.Key;
				break;
			}
		}

		if (matchedExecutor != null) {
			finishedInterpreter.ExecutionFinished -= OnInterpreterFinished;
			finishedInterpreter.ExecutionFailed -= OnInterpreterFailed;
			_interpreters.Remove(matchedExecutor);

			if (executors != null) {
				for (int i = 0; i < executors.Count; i++) {
					if (executors[i] == matchedExecutor) {
						UpdateButtonLabel(i, "Run");
						break;
					}
				}
			}
			return;
		}

		if (_fallbackInterpreter == finishedInterpreter) {
			finishedInterpreter.ExecutionFinished -= OnInterpreterFinished;
			finishedInterpreter.ExecutionFailed -= OnInterpreterFailed;
			_fallbackInterpreter = null;
			if (_fallbackInterpreterIndex >= 0) {
				UpdateButtonLabel(_fallbackInterpreterIndex, "Run");
			}
			_fallbackInterpreterIndex = -1;
		}
	}

	private void OnInterpreterFailed(AstInterpreter interpreter, string warningMessage) {
		if (interpreter == null || string.IsNullOrWhiteSpace(warningMessage)) {
			return;
		}

		Debug.LogWarning($"Script execution stopped: {warningMessage}");
	}
}
