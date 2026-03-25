using System.Collections.Generic;
using UnityEngine;

public class RobotSpawnManager : MonoBehaviour {
	[Header("Robot Spawn")]
	[SerializeField] private GameObject robotPrefab;
	[SerializeField] private Transform spawnedRobotParent;

	private readonly Dictionary<RobotSpawnPoint, GameObject> spawnedRobots = new();

	public int SpawnedRobotCount => spawnedRobots.Count;
	public event System.Action SpawnedRobotsChanged;

	public void SpawnRobots() {
		CleanupDestroyedEntries();

		if (robotPrefab == null) {
			Debug.LogWarning("[RobotSpawnManager] Robot prefab is not assigned.");
			return;
		}

		var spawnPoints = RobotSpawnPoint.AllSpawnPoints;
		bool changed = false;
		for (int i = 0; i < spawnPoints.Count; i++) {
			var spawnPoint = spawnPoints[i];
			if (spawnPoint == null) continue;
			if (spawnedRobots.ContainsKey(spawnPoint) && spawnedRobots[spawnPoint] != null) continue;

			Transform parent = spawnedRobotParent != null ? spawnedRobotParent : null;
			var robotInstance = Instantiate(robotPrefab, spawnPoint.SpawnPosition, spawnPoint.SpawnRotation, parent);
			robotInstance.name = spawnPoint.DisplayName + "_Robot";
			spawnedRobots[spawnPoint] = robotInstance;
			changed = true;
		}

		if (changed) {
			SpawnedRobotsChanged?.Invoke();
		}
	}

	public void DespawnRobots() {
		bool changed = spawnedRobots.Count > 0;
		foreach (var pair in spawnedRobots) {
			if (pair.Value != null) {
				Destroy(pair.Value);
			}
		}

		spawnedRobots.Clear();
		if (changed) {
			SpawnedRobotsChanged?.Invoke();
		}
	}

	public List<RobotExecutor> GetActiveExecutors() {
		CleanupDestroyedEntries();

		var executors = new List<RobotExecutor>();
		foreach (var pair in spawnedRobots) {
			if (pair.Value == null) continue;

			if (!pair.Value.TryGetComponent<RobotExecutor>(out var executor)) executor = pair.Value.GetComponentInChildren<RobotExecutor>();
			if (executor != null) executors.Add(executor);
		}

		return executors;
	}

	public bool TryGetExecutor(RobotSpawnPoint spawnPoint, out RobotExecutor executor) {
		executor = null;
		if (spawnPoint == null) {
			return false;
		}

		CleanupDestroyedEntries();

		GameObject robotInstance;
		if (!spawnedRobots.TryGetValue(spawnPoint, out robotInstance) || robotInstance == null) {
			return false;
		}

		if (!robotInstance.TryGetComponent<RobotExecutor>(out executor)) {
			executor = robotInstance.GetComponentInChildren<RobotExecutor>();
		}

		return executor != null;
	}

	private void CleanupDestroyedEntries() {
		var removedKeys = new List<RobotSpawnPoint>();
		foreach (var pair in spawnedRobots) {
			if (pair.Key == null || pair.Value == null) {
				removedKeys.Add(pair.Key);
			}
		}

		for (int i = 0; i < removedKeys.Count; i++) {
			spawnedRobots.Remove(removedKeys[i]);
		}

		if (removedKeys.Count > 0) {
			SpawnedRobotsChanged?.Invoke();
		}
	}
}
