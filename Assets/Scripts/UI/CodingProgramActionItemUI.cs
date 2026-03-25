using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodingProgramActionItemUI : MonoBehaviour {
	[SerializeField] private Button editButton;
	[SerializeField] private Button runButton;
	[SerializeField] private Button deleteButton;
	[SerializeField] private TMP_Text titleText;

	private GameplayActionPanelUI owner;
	private RobotSpawnPoint spawnPoint;
	private string codeId;

	public void Bind(GameplayActionPanelUI panelOwner, RobotSpawnPoint targetSpawnPoint, RobotCodeEntry codeEntry, bool canRun) {
		owner = panelOwner;
		spawnPoint = targetSpawnPoint;
		codeId = codeEntry != null ? codeEntry.CodeId : string.Empty;

		if (titleText != null) {
			titleText.text = codeEntry != null ? codeEntry.DisplayName : "Kod";
		}

		if (editButton != null) {
			editButton.onClick.RemoveListener(HandleEditClicked);
			editButton.onClick.AddListener(HandleEditClicked);
		}

		if (runButton != null) {
			runButton.onClick.RemoveListener(HandleRunClicked);
			runButton.onClick.AddListener(HandleRunClicked);
			runButton.interactable = canRun;
		}

		if (deleteButton != null) {
			deleteButton.onClick.RemoveListener(HandleDeleteClicked);
			deleteButton.onClick.AddListener(HandleDeleteClicked);
		}
	}

	private void HandleEditClicked() {
		if (owner != null && spawnPoint != null && !string.IsNullOrWhiteSpace(codeId)) {
			owner.OpenCode(spawnPoint, codeId);
		}
	}

	private void HandleRunClicked() {
		if (owner != null && spawnPoint != null && !string.IsNullOrWhiteSpace(codeId)) {
			owner.RunCode(spawnPoint, codeId);
		}
	}

	private void HandleDeleteClicked() {
		if (owner != null && spawnPoint != null && !string.IsNullOrWhiteSpace(codeId)) {
			owner.DeleteCode(spawnPoint, codeId);
		}
	}
}
