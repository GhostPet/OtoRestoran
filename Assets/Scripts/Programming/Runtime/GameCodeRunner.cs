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

	private readonly Dictionary<string, AstInterpreter> _interpreters =
		   new(System.StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, RobotExecutor> _codeExecutors =
		new(System.StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<RobotExecutor, string> _activeCodeIdsByExecutor =
		new(EqualityComparer<RobotExecutor>.Default);
	private readonly Dictionary<string, string> _contextCodeIds =
		new(System.StringComparer.OrdinalIgnoreCase);
	private AstInterpreter _fallbackInterpreter;
	private int _fallbackInterpreterIndex = -1;

	private readonly Dictionary<Button, UnityAction> _buttonListeners
		= new(EqualityComparer<Button>.Default);
	private readonly Dictionary<RobotExecutor, int> _editorIndices
		= new(EqualityComparer<RobotExecutor>.Default);
	private readonly Dictionary<int, string> _editorCodeIds = new();

	public event System.Action<string, string, bool> StatusMessageReceived;

	private void OnEnable() {
		CommandExecutionContext.StatusMessagePublished -= HandleStatusMessagePublished;
		CommandExecutionContext.StatusMessagePublished += HandleStatusMessagePublished;
	}

	private void OnDisable() {
		CommandExecutionContext.StatusMessagePublished -= HandleStatusMessagePublished;
	}

	public bool ExecutionEnabled => executionEnabled;

	private void OnDestroy() {
		CommandExecutionContext.StatusMessagePublished -= HandleStatusMessagePublished;
		_statusCleanup();
	}

	private void _statusCleanup() {
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
		_editorCodeIds[index] = "editor_" + index;
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
		string codeId;
		if (!_editorCodeIds.TryGetValue(index, out codeId)) {
			codeId = "editor_" + index;
			_editorCodeIds[index] = codeId;
		}

		if (!executionEnabled) {
			NotifyStatus(codeId, "Restoran kapalıyken kod çalıştırılamaz.", true);
			return;
		}

		// Validate index
		if (codeInputs == null || index < 0 || index >= codeInputs.Count) {
			return;
		}

		// If there is an executor for this index, use per-executor interpreter
		RobotExecutor targetExecutor = (executors != null && index < executors.Count) ? executors[index] : null;

		if (IsExecutionRunning(codeId)) {
			StopExecution(codeId);
			UpdateButtonLabel(index, "Run");
			return;
		}

		string code = codeInputs[index] != null ? codeInputs[index].text ?? string.Empty : string.Empty;
		TryRunCode(codeId, targetExecutor, code);
	}

	public bool TryRunCode(string codeId, RobotExecutor executor, string code) {
		if (string.IsNullOrWhiteSpace(codeId)) {
			return false;
		}

		if (!executionEnabled) {
			NotifyStatus(codeId, "Restoran kapalıyken kod çalıştırılamaz.", true);
			return false;
		}

		if (executor == null) {
			NotifyStatus(codeId, "Aktif robot bulunamadı.", true);
			return false;
		}

		if (IsExecutionRunning(codeId)) {
			StopExecution(codeId);
			NotifyStatus(codeId, "Kod durduruldu.", false);
			return true;
		}

		string activeCodeId;
		if (_activeCodeIdsByExecutor.TryGetValue(executor, out activeCodeId) &&
			!string.IsNullOrWhiteSpace(activeCodeId) &&
			!string.Equals(activeCodeId, codeId, System.StringComparison.OrdinalIgnoreCase)) {
			NotifyStatus(activeCodeId, "Aynı robotta başka bir kod çalıştırıldığı için durduruldu.", false);
			StopExecution(activeCodeId);
		}

		int index = GetIndexForExecutor(executor);
		if (codeInputs != null && index >= 0 && index < codeInputs.Count && codeInputs[index] != null) {
			codeInputs[index].text = code ?? string.Empty;
		}

		return TryStartExecution(codeId, executor, index, code ?? string.Empty);
	}

	private int RegisterRuntimeExecutor(RobotExecutor executor) {
		if (executor == null) {
			return -1;
		}

		codeInputs ??= new List<TMP_InputField>();
		runButtons ??= new List<Button>();
		executors ??= new List<RobotExecutor>();

		executors.Add(executor);
		codeInputs.Add(null);
		runButtons.Add(null);

		int index = executors.Count - 1;
		_editorIndices[executor] = index;
		return index;
	}

	private bool TryStartExecution(string codeId, RobotExecutor executor, int index, string code) {
		// Tokenize and parse
		Lexer lexer = new(code);
		List<Token> tokens;
		try {
			tokens = lexer.Tokenize();
		} catch (System.Exception ex) {
			NotifyStatus(codeId, ex.Message, true);
			return false;
		}

		Parser parser = new(tokens);
		List<FunctionDefNode> functions;
		try {
			functions = parser.Parse();
		} catch (System.Exception ex) {
			NotifyStatus(codeId, ex.Message, true);
			return false;
		}

		if (executor != null) {
			var interpObj = new GameObject($"AstInterpreter_{executor.name}_{codeId}");
			var interp = interpObj.AddComponent<AstInterpreter>();
			interp.Executor = executor;
			interp.ExecutionFinished += OnInterpreterFinished;
			interp.ExecutionFailed += OnInterpreterFailed;
			_interpreters[codeId] = interp;
			_codeExecutors[codeId] = executor;
			_activeCodeIdsByExecutor[executor] = codeId;
			interp.StartExecution(functions, "main");
			if (!string.IsNullOrWhiteSpace(interp.ContextId)) {
				_contextCodeIds[interp.ContextId] = codeId;
			}
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
		List<string> activeCodeIds = new List<string>(_interpreters.Keys);
		for (int i = 0; i < activeCodeIds.Count; i++) {
			StopExecution(activeCodeIds[i]);
		}

		_contextCodeIds.Clear();
		_codeExecutors.Clear();
		_activeCodeIdsByExecutor.Clear();

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
		_editorIndices.Clear();

		if (newExecutors != null) {
			for (int i = 0; i < newExecutors.Count; i++) {
				RobotExecutor executor = newExecutors[i];
				if (executor == null) {
					continue;
				}

				executors.Add(executor);
				_editorIndices[executor] = executors.Count - 1;
			}
		}

		RefreshRunButtonStates();
	}

	public bool IsExecutionRunning(RobotExecutor executor) {
		if (executor == null) {
			return false;
		}

		return _activeCodeIdsByExecutor.ContainsKey(executor);
	}

	public bool IsExecutionRunning(string codeId) {
		return !string.IsNullOrWhiteSpace(codeId) && _interpreters.ContainsKey(codeId);
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
		if (executor == null) {
			return;
		}

		string codeId;
		if (_activeCodeIdsByExecutor.TryGetValue(executor, out codeId)) {
			StopExecution(codeId);
		}
	}

	public void StopExecution(string codeId) {
		if (string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		AstInterpreter interpreter;
		if (!_interpreters.TryGetValue(codeId, out interpreter)) {
			return;
		}

		int index = -1;
		RobotExecutor executor = null;
		if (_codeExecutors.TryGetValue(codeId, out executor)) {
			index = GetIndexForExecutor(executor);
			string activeCodeId;
			if (_activeCodeIdsByExecutor.TryGetValue(executor, out activeCodeId) &&
				string.Equals(activeCodeId, codeId, System.StringComparison.OrdinalIgnoreCase)) {
				_activeCodeIdsByExecutor.Remove(executor);
			}
		}

		interpreter.ExecutionFinished -= OnInterpreterFinished;
		interpreter.ExecutionFailed -= OnInterpreterFailed;
		if (!string.IsNullOrEmpty(interpreter.ContextId)) {
			_contextCodeIds.Remove(interpreter.ContextId);
			CommandExecutionContext.ClearVariables(interpreter.ContextId);
		}
		if (interpreter != null) {
			Destroy(interpreter.gameObject);
		}

		_interpreters.Remove(codeId);
		_codeExecutors.Remove(codeId);

		if (index >= 0) {
			UpdateButtonLabel(index, "Run");
		}
	}

	private void OnInterpreterFinished(AstInterpreter finishedInterpreter) {
		if (finishedInterpreter == null) return;

		string matchedCodeId = null;
		foreach (var kv in _interpreters) {
			if (kv.Value == finishedInterpreter) {
				matchedCodeId = kv.Key;
				break;
			}
		}

		if (!string.IsNullOrWhiteSpace(matchedCodeId)) {
			RobotExecutor executor = null;
			int index = -1;
			if (_codeExecutors.TryGetValue(matchedCodeId, out executor)) {
				index = GetIndexForExecutor(executor);
				string activeCodeId;
				if (_activeCodeIdsByExecutor.TryGetValue(executor, out activeCodeId) &&
					string.Equals(activeCodeId, matchedCodeId, System.StringComparison.OrdinalIgnoreCase)) {
					_activeCodeIdsByExecutor.Remove(executor);
				}
			}

			if (!string.IsNullOrWhiteSpace(finishedInterpreter.ContextId)) {
				_contextCodeIds.Remove(finishedInterpreter.ContextId);
			}
			finishedInterpreter.ExecutionFinished -= OnInterpreterFinished;
			finishedInterpreter.ExecutionFailed -= OnInterpreterFailed;
			_interpreters.Remove(matchedCodeId);
			_codeExecutors.Remove(matchedCodeId);

			if (index >= 0) {
				UpdateButtonLabel(index, "Run");
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
	}

	private RobotExecutor GetExecutorForIndex(int index) {
		if (executors == null || index < 0 || index >= executors.Count) {
			return null;
		}

		return executors[index];
	}

	private int GetIndexForExecutor(RobotExecutor executor) {
		int index;
		if (executor != null && _editorIndices.TryGetValue(executor, out index)) {
			return index;
		}

		if (executor != null && executors != null) {
			return executors.IndexOf(executor);
		}

		return -1;
	}

	private void HandleStatusMessagePublished(string contextId, string message, bool isError) {
		if (string.IsNullOrWhiteSpace(contextId) || string.IsNullOrWhiteSpace(message)) {
			return;
		}

		string codeId;
		if (!_contextCodeIds.TryGetValue(contextId, out codeId)) {
			return;
		}

		NotifyStatus(codeId, message, isError);
	}

	private void NotifyStatus(string codeId, string message, bool isError) {
		if (string.IsNullOrWhiteSpace(message)) {
			return;
		}

		StatusMessageReceived?.Invoke(codeId, message, isError);
	}
}
