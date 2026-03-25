using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobotProgramWindowUI : WindowContentUI {
	[SerializeField] private TMP_Text windowTitleText;
	[SerializeField] private TMP_InputField programNameInput;
	[SerializeField] private TMP_InputField codeInput;
	[SerializeField] private Button saveButton;
	[SerializeField] private Button runButton;
	[SerializeField] private TMP_Text runButtonText;
	[SerializeField] private TMP_Text statusText;

	private CodeEditorWindowUI owner;
	private RobotSpawnPoint spawnPoint;
	private string codeId;

	private void Awake() {
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

		if (programNameInput != null) {
			programNameInput.text = codeEntry != null ? codeEntry.DisplayName : string.Empty;
		}

		if (codeInput != null) {
			codeInput.text = codeEntry != null ? codeEntry.Code : string.Empty;
		}

		RefreshTitle();
		RefreshRuntimeState();
	}

	public void RefreshFromCode(RobotCodeEntry codeEntry) {
		if (codeEntry == null) {
			return;
		}

		codeId = codeEntry.CodeId;
		if (programNameInput != null) {
			programNameInput.text = codeEntry.DisplayName;
		}

		if (codeInput != null) {
			codeInput.text = codeEntry.Code;
		}

		RefreshTitle();
		RefreshRuntimeState();
	}

	protected override void OnWindowBound(GameWindowUI ownerWindow) {
		RefreshTitle();
	}

	private void RegisterUiEvents() {
		if (saveButton != null) {
			saveButton.onClick.RemoveListener(HandleSaveClicked);
			saveButton.onClick.AddListener(HandleSaveClicked);
		}

		if (runButton != null) {
			runButton.onClick.RemoveListener(HandleRunClicked);
			runButton.onClick.AddListener(HandleRunClicked);
		}

		if (programNameInput != null) {
			programNameInput.onValueChanged.RemoveListener(HandleProgramNameChanged);
			programNameInput.onValueChanged.AddListener(HandleProgramNameChanged);
		}
	}

	private void HandleSaveClicked() {
		if (owner == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		owner.SaveCode(codeId, GetProgramName(), GetCode());
		SetStatus("Kod kaydedildi.");
		RefreshTitle();
	}

	private void HandleRunClicked() {
		if (owner == null || spawnPoint == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		owner.SaveCode(codeId, GetProgramName(), GetCode());
		bool started = owner.RunCode(spawnPoint, codeId);
		SetStatus(started ? "Kod çalıştırıldı." : "Kod çalıştırılamadı.");
	}

	private void HandleProgramNameChanged(string value) {
		RefreshTitle();
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
		bool isRunning = owner != null && spawnPoint != null && owner.IsCodeRunning(spawnPoint);

		if (runButton != null) {
			runButton.interactable = canRun;
		}

		if (runButtonText != null) {
			runButtonText.text = isRunning ? "Durdur" : "Çalıştır";
		}

		if (statusText != null && !canRun) {
			statusText.text = owner != null && owner.IsRestaurantOpen
				? "Aktif robot bekleniyor."
				: "Hazırlık aşamasında kod yazılabilir, çalıştırılamaz.";
		}
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
}
