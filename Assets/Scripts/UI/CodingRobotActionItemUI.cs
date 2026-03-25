using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodingRobotActionItemUI : MonoBehaviour {
	[SerializeField] private Button headerButton;
	[SerializeField] private TMP_Text headerText;
	[SerializeField] private Button newCodeButton;
	[SerializeField] private TMP_Text emptyStateText;
	[SerializeField] private GameObject contentRoot;
	[SerializeField] private Transform codeContainer;
	[SerializeField] private CodingProgramActionItemUI codeItemPrefab;

	private readonly List<CodingProgramActionItemUI> spawnedCodes = new List<CodingProgramActionItemUI>();
	private GameplayActionPanelUI owner;
	private RobotSpawnPoint spawnPoint;
	private bool expanded;

	public void Bind(GameplayActionPanelUI panelOwner, RobotSpawnPoint targetSpawnPoint, List<RobotCodeEntry> codes, bool isExpanded, bool canRun) {
		owner = panelOwner;
		spawnPoint = targetSpawnPoint;
		expanded = isExpanded;

		if (headerText != null) {
			headerText.text = targetSpawnPoint != null ? targetSpawnPoint.DisplayName : "Robot";
		}

		if (headerButton != null) {
			headerButton.onClick.RemoveListener(HandleHeaderClicked);
			headerButton.onClick.AddListener(HandleHeaderClicked);
		}

		if (newCodeButton != null) {
			newCodeButton.onClick.RemoveListener(HandleNewCodeClicked);
			newCodeButton.onClick.AddListener(HandleNewCodeClicked);
		}

		RebuildCodes(codes, canRun);
		ApplyExpandedState();
	}

	private void RebuildCodes(List<RobotCodeEntry> codes, bool canRun) {
		ClearCodes();
		if (codeContainer == null || codeItemPrefab == null) {
			return;
		}

		bool hasCodes = codes != null && codes.Count > 0;
		if (emptyStateText != null) {
			emptyStateText.gameObject.SetActive(!hasCodes);
			emptyStateText.text = "Kayıtlı kod yok. Yeni kod oluştur.";
		}

		if (!hasCodes) {
			return;
		}

		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry codeEntry = codes[i];
			if (codeEntry == null) {
				continue;
			}

			CodingProgramActionItemUI item = Instantiate(codeItemPrefab, codeContainer);
			item.Bind(owner, spawnPoint, codeEntry, canRun);
			spawnedCodes.Add(item);
		}
	}

	private void ApplyExpandedState() {
		if (contentRoot != null) {
			contentRoot.SetActive(expanded);
		}
	}

	private void HandleHeaderClicked() {
		if (owner != null && spawnPoint != null) {
			owner.ToggleRobotExpansion(spawnPoint);
		}
	}

	private void HandleNewCodeClicked() {
		if (owner != null && spawnPoint != null) {
			owner.CreateCode(spawnPoint);
		}
	}

	private void ClearCodes() {
		for (int i = 0; i < spawnedCodes.Count; i++) {
			CodingProgramActionItemUI item = spawnedCodes[i];
			if (item != null) {
				Destroy(item.gameObject);
			}
		}

		spawnedCodes.Clear();
	}
}
