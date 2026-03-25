using System.Collections.Generic;
using UnityEngine;

public class CodeEditorWindowUI : MonoBehaviour {
	[SerializeField] private GameCodeRunner gameCodeRunner;
	[SerializeField] private RobotSpawnManager robotSpawnManager;
	[SerializeField] private RestaurantOperationsController operationsController;
	[SerializeField] private RobotCodeRegistry robotCodeRegistry;
	[SerializeField] private WindowAreaUI windowArea;
	[SerializeField] private RobotProgramWindowUI codeWindowContentPrefab;

	private readonly Dictionary<string, RobotProgramWindowUI> openCodeWindows = new Dictionary<string, RobotProgramWindowUI>();

	public bool IsRestaurantOpen => operationsController != null && operationsController.IsRestaurantOpen;

	private void OnEnable() {
		Subscribe();
	}

	private void OnDisable() {
		Unsubscribe();
	}

	public void SetReferences(
		GameCodeRunner runner,
		RobotSpawnManager spawnManager,
		RestaurantOperationsController operations,
		RobotCodeRegistry codeRegistry,
		WindowAreaUI area) {
		Unsubscribe();

		gameCodeRunner = runner;
		robotSpawnManager = spawnManager;
		operationsController = operations;
		robotCodeRegistry = codeRegistry;
		windowArea = area;

		Subscribe();
		RefreshOpenWindows();
	}

	public RobotCodeEntry CreateCode(RobotSpawnPoint spawnPoint) {
		if (robotCodeRegistry == null || spawnPoint == null) {
			return null;
		}

		RobotCodeEntry codeEntry = robotCodeRegistry.CreateCode(spawnPoint.ProgramBindingKey, spawnPoint.DisplayName + " Kod");
		if (codeEntry != null) {
			OpenCodeWindow(spawnPoint, codeEntry.CodeId);
		}

		return codeEntry;
	}

	public void OpenCodeWindow(RobotSpawnPoint spawnPoint, string codeId) {
		if (spawnPoint == null || string.IsNullOrWhiteSpace(codeId) || robotCodeRegistry == null || windowArea == null || codeWindowContentPrefab == null) {
			return;
		}

		RobotCodeEntry codeEntry;
		if (!robotCodeRegistry.TryGetCode(codeId, out codeEntry)) {
			return;
		}

		RobotProgramWindowUI content = windowArea.OpenSingletonWindow(codeId, BuildWindowTitle(spawnPoint, codeEntry), codeWindowContentPrefab);
		if (content == null) {
			return;
		}

		content.Bind(this, spawnPoint, codeEntry);
		openCodeWindows[codeId] = content;
	}

	public void SaveCode(string codeId, string displayName, string code) {
		if (robotCodeRegistry == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		robotCodeRegistry.SaveCode(codeId, displayName, code);
	}

	public bool RunCode(RobotSpawnPoint spawnPoint, string codeId) {
		if (gameCodeRunner == null || robotCodeRegistry == null || spawnPoint == null || string.IsNullOrWhiteSpace(codeId)) {
			return false;
		}

		RobotCodeEntry codeEntry;
		if (!robotCodeRegistry.TryGetCode(codeId, out codeEntry)) {
			return false;
		}

		RobotExecutor executor;
		if (!TryResolveExecutor(spawnPoint, out executor)) {
			return false;
		}

		if (gameCodeRunner.IsExecutionRunning(executor)) {
			gameCodeRunner.StopExecutionFor(executor);
			RefreshOpenWindows();
			return true;
		}

		bool started = gameCodeRunner.TryRunCode(executor, codeEntry.Code);
		RefreshOpenWindows();
		return started;
	}

	public void DeleteCode(string codeId) {
		if (robotCodeRegistry == null || string.IsNullOrWhiteSpace(codeId)) {
			return;
		}

		if (!robotCodeRegistry.DeleteCode(codeId)) {
			return;
		}

		RobotProgramWindowUI content;
		if (openCodeWindows.TryGetValue(codeId, out content) && content != null && content.Window != null) {
			content.Window.CloseWindow();
		}

		openCodeWindows.Remove(codeId);
	}

	public bool CanRunCode(RobotSpawnPoint spawnPoint) {
		if (!IsRestaurantOpen || gameCodeRunner == null || !gameCodeRunner.ExecutionEnabled) {
			return false;
		}

		RobotExecutor executor;
		return TryResolveExecutor(spawnPoint, out executor);
	}

	public bool IsCodeRunning(RobotSpawnPoint spawnPoint) {
		if (gameCodeRunner == null) {
			return false;
		}

		RobotExecutor executor;
		if (!TryResolveExecutor(spawnPoint, out executor)) {
			return false;
		}

		return gameCodeRunner.IsExecutionRunning(executor);
	}

	private bool TryResolveExecutor(RobotSpawnPoint spawnPoint, out RobotExecutor executor) {
		executor = null;
		if (spawnPoint == null || robotSpawnManager == null) {
			return false;
		}

		return robotSpawnManager.TryGetExecutor(spawnPoint, out executor);
	}

	private string BuildWindowTitle(RobotSpawnPoint spawnPoint, RobotCodeEntry codeEntry) {
		string robotName = spawnPoint != null ? spawnPoint.DisplayName : "Robot";
		string codeName = codeEntry != null ? codeEntry.DisplayName : "Kod";
		return robotName + " - " + codeName;
	}

	private void Subscribe() {
		if (!isActiveAndEnabled) {
			return;
		}

		if (robotCodeRegistry != null) {
			robotCodeRegistry.CodesChanged -= HandleCodesChanged;
			robotCodeRegistry.CodesChanged += HandleCodesChanged;
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.SpawnedRobotsChanged -= HandleSpawnedRobotsChanged;
			robotSpawnManager.SpawnedRobotsChanged += HandleSpawnedRobotsChanged;
		}
	}

	private void Unsubscribe() {
		if (robotCodeRegistry != null) {
			robotCodeRegistry.CodesChanged -= HandleCodesChanged;
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.SpawnedRobotsChanged -= HandleSpawnedRobotsChanged;
		}
	}

	private void HandleCodesChanged() {
		RefreshOpenWindows();
	}

	private void HandleSpawnedRobotsChanged() {
		RefreshOpenWindows();
	}

	private void RefreshOpenWindows() {
		if (robotCodeRegistry == null) {
			return;
		}

		List<string> removedKeys = null;
		foreach (KeyValuePair<string, RobotProgramWindowUI> pair in openCodeWindows) {
			RobotProgramWindowUI content = pair.Value;
			if (content == null) {
				if (removedKeys == null) {
					removedKeys = new List<string>();
				}

				removedKeys.Add(pair.Key);
				continue;
			}

			RobotCodeEntry codeEntry;
			if (!robotCodeRegistry.TryGetCode(pair.Key, out codeEntry)) {
				if (content.Window != null) {
					content.Window.CloseWindow();
				}

				if (removedKeys == null) {
					removedKeys = new List<string>();
				}

				removedKeys.Add(pair.Key);
				continue;
			}

			content.RefreshFromCode(codeEntry);
		}

		if (removedKeys == null) {
			return;
		}

		for (int i = 0; i < removedKeys.Count; i++) {
			openCodeWindows.Remove(removedKeys[i]);
		}
	}
}
