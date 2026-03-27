using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobotProgramWindowUI : WindowContentUI {
	[SerializeField] private TMP_Text windowTitleText;
	[SerializeField] private TMP_InputField programNameInput;
	[SerializeField] private TMP_InputField codeInput;
	[SerializeField] private TMP_Text renderedCodeText;
	[SerializeField] private SyntaxHighlighter syntaxHighlighter;
	[SerializeField] private TMP_Text lineNumbersText;
	[SerializeField] private bool hideCodeInputText = true;
	[SerializeField] private Button runButton;
	[SerializeField] private TMP_Text runButtonText;
	[SerializeField] private TMP_Text statusText;

	private CodeEditorWindowUI owner;
	private RobotSpawnPoint spawnPoint;
	private string codeId;
	private bool suppressAutoSave;

	private void Awake() {
		ResolveEditorTools();
		RegisterUiEvents();
	}

	private void OnEnable() {
		RefreshRuntimeState();
	}

	private void Update() {
		RefreshRuntimeState();
	}

	public void Bind(CodeEditorWindowUI windowOwner, RobotSpawnPoint targetSpawnPoint, RobotCodeEntry codeEntry) {
		owner = windowOwner;
		spawnPoint = targetSpawnPoint;
		codeId = codeEntry != null ? codeEntry.CodeId : string.Empty;
		suppressAutoSave = true;

		if (programNameInput != null) {
			programNameInput.text = codeEntry != null ? codeEntry.DisplayName : string.Empty;
		}

		if (codeInput != null) {
			codeInput.text = codeEntry != null ? codeEntry.Code : string.Empty;
		}

		suppressAutoSave = false;

		RefreshTitle();
		RefreshEditorVisuals();
		RefreshRuntimeState();
	}

	public void RefreshFromCode(RobotCodeEntry codeEntry) {
		if (codeEntry == null) {
			return;
		}

		codeId = codeEntry.CodeId;
		suppressAutoSave = true;
		if (programNameInput != null) {
			programNameInput.text = codeEntry.DisplayName;
		}

		if (codeInput != null) {
			codeInput.text = codeEntry.Code;
		}
		suppressAutoSave = false;

		RefreshTitle();
		RefreshEditorVisuals();
		RefreshRuntimeState();
	}

	protected override void OnWindowBound(GameWindowUI ownerWindow) {
		ResolveEditorTools();
		RefreshTitle();
		RefreshEditorVisuals();
	}

	private void RegisterUiEvents() {
		if (runButton != null) {
			runButton.onClick.RemoveListener(HandleRunClicked);
			runButton.onClick.AddListener(HandleRunClicked);
		}

		if (programNameInput != null) {
			programNameInput.onValueChanged.RemoveListener(HandleProgramNameChanged);
			programNameInput.onValueChanged.AddListener(HandleProgramNameChanged);
		}

		if (codeInput != null) {
			codeInput.onValueChanged.RemoveListener(HandleCodeChanged);
			codeInput.onValueChanged.AddListener(HandleCodeChanged);
		}
	}

	private void HandleRunClicked() {
		if (owner == null || spawnPoint == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		SaveCurrentCode();
		owner.RunCode(spawnPoint, codeId);
	}

	private void HandleProgramNameChanged(string value) {
		SaveCurrentCode();
		RefreshTitle();
	}

	private void HandleCodeChanged(string value) {
		RefreshEditorVisuals();
		SaveCurrentCode();
	}

	private void RefreshTitle() {
		string robotName = spawnPoint != null ? spawnPoint.DisplayName : "Robot";
		string codeName = GetProgramName();
		if (windowTitleText != null) {
			windowTitleText.text = robotName + " - " + codeName;
		}

		if (Window != null) {
			Window.SetTitle(robotName + " - " + codeName);
		}
	}

	private void RefreshRuntimeState() {
		bool canRun = owner != null && spawnPoint != null && owner.CanRunCode(spawnPoint);
		bool isRunning = owner != null && !string.IsNullOrWhiteSpace(codeId) && owner.IsCodeRunning(codeId);

		if (runButton != null) {
			runButton.interactable = canRun;
		}

		if (runButtonText != null) {
			runButtonText.text = isRunning ? "Durdur" : "Çalıştır";
		}

		if (statusText == null) {
			return;
		}

		if (!canRun) {
			statusText.text = owner != null && owner.IsRestaurantOpen
				? "Aktif robot bekleniyor."
				: "Hazırlık aşamasında kod yazılabilir, çalıştırılamaz.";
		}
	}

	private void RefreshEditorVisuals() {
		string code = GetCode();

		if (renderedCodeText != null) {
			ResolveEditorTools();
			if (syntaxHighlighter != null) {
				renderedCodeText.text = syntaxHighlighter.Highlight(code);
			} else {
				renderedCodeText.text = string.IsNullOrEmpty(code) ? " " : code;
			}
		}

		if (lineNumbersText != null) {
			lineNumbersText.text = BuildLineNumbers(code);
		}

		if (hideCodeInputText && codeInput != null && codeInput.textComponent != null) {
			Color textColor = codeInput.textComponent.color;
			textColor.a = 0f;
			codeInput.textComponent.color = textColor;
		}
	}

	private void ResolveEditorTools() {
		if (syntaxHighlighter == null && renderedCodeText != null) {
			syntaxHighlighter = renderedCodeText.GetComponent<SyntaxHighlighter>();
		}

		if (syntaxHighlighter == null) {
			syntaxHighlighter = GetComponent<SyntaxHighlighter>();
		}

		if (syntaxHighlighter != null) {
			syntaxHighlighter.Bind(codeInput, renderedCodeText);
		}
	}

	private void SaveCurrentCode() {
		if (suppressAutoSave || owner == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		owner.SaveCode(codeId, GetProgramName(), GetCode());
	}

	private string BuildLineNumbers(string code) {
		int lineCount = 1;
		for (int i = 0; i < code.Length; i++) {
			if (code[i] == '\n') {
				lineCount++;
			}
		}

		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		for (int i = 1; i <= lineCount; i++) {
			if (i > 1) {
				builder.Append('\n');
			}

			builder.Append(i);
		}

		return builder.ToString();
	}

	private string GetProgramName() {
		if (programNameInput == null || string.IsNullOrWhiteSpace(programNameInput.text)) {
			return "Yeni Kod";
		}

		return programNameInput.text.Trim();
	}

	private string GetCode() {
		return codeInput != null ? codeInput.text : string.Empty;
	}

	private void SetStatus(string message) {
		if (statusText != null) {
			statusText.text = message;
		}
	}

	public void SetStatusMessage(string message) {
		SetStatus(message);
	}
}
