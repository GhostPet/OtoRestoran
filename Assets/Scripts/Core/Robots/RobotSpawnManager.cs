using System.Collections.Generic;
using UnityEngine;

public class RobotSpawnManager : MonoBehaviour {
	[Header("Robot Spawn")]
	[SerializeField] private GameObject robotPrefab;
	[SerializeField] private Transform spawnedRobotParent;

	private readonly Dictionary<RobotSpawnPoint, GameObject> spawnedRobots = new();
	private bool robotsVisible;

	public int SpawnedRobotCount => spawnedRobots.Count;
	public event System.Action SpawnedRobotsChanged;

	public void SpawnRobots() {
		SetRobotsVisible(true);
		SyncSpawnedRobots();
	}

	public void SyncSpawnedRobots() {
		CleanupDestroyedEntries();

		if (robotPrefab == null) {
			Debug.LogWarning("[RobotSpawnManager] Robot prefab is not assigned.");
			return;
		}

		var spawnPoints = RobotSpawnPoint.AllSpawnPoints;
		var activeSpawnPoints = new HashSet<RobotSpawnPoint>(spawnPoints);
		bool changed = false;

		var removedSpawnPoints = new List<RobotSpawnPoint>();
		foreach (var pair in spawnedRobots) {
			if (pair.Key == null || activeSpawnPoints.Contains(pair.Key)) {
				continue;
			}

			removedSpawnPoints.Add(pair.Key);
		}

		for (int i = 0; i < removedSpawnPoints.Count; i++) {
			if (RemoveRobotForSpawnPoint(removedSpawnPoints[i])) {
				changed = true;
			}
		}

		for (int i = 0; i < spawnPoints.Count; i++) {
			var spawnPoint = spawnPoints[i];
			if (EnsureRobotForSpawnPoint(spawnPoint)) {
				changed = true;
			}
		}

		if (changed) {
			SpawnedRobotsChanged?.Invoke();
		}
	}

	public void SetRobotsVisible(bool isVisible) {
		robotsVisible = isVisible;
		CleanupDestroyedEntries();

		foreach (var pair in spawnedRobots) {
			ApplyVisibility(pair.Value, isVisible);
		}
	}

	public void HandleSpawnPointPlaced(RobotSpawnPoint spawnPoint) {
		if (EnsureRobotForSpawnPoint(spawnPoint)) {
			SpawnedRobotsChanged?.Invoke();
		}
	}

	public void HandleSpawnPointRemoved(RobotSpawnPoint spawnPoint) {
		if (RemoveRobotForSpawnPoint(spawnPoint)) {
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

	private bool EnsureRobotForSpawnPoint(RobotSpawnPoint spawnPoint) {
		if (spawnPoint == null || robotPrefab == null) {
			return false;
		}

		GameObject robotInstance;
		if (spawnedRobots.TryGetValue(spawnPoint, out robotInstance) && robotInstance != null) {
			UpdateRobotTransform(robotInstance, spawnPoint);
			ApplyVisibility(robotInstance, robotsVisible);
			return false;
		}

		Transform parent = spawnedRobotParent != null ? spawnedRobotParent : null;
		robotInstance = Instantiate(robotPrefab, spawnPoint.SpawnPosition, spawnPoint.SpawnRotation, parent);
		robotInstance.name = spawnPoint.DisplayName + "_Robot";
		spawnedRobots[spawnPoint] = robotInstance;
		BindRobot(robotInstance, spawnPoint);
		ApplyVisibility(robotInstance, robotsVisible);
		return true;
	}

	private bool RemoveRobotForSpawnPoint(RobotSpawnPoint spawnPoint) {
		if (spawnPoint == null) {
			return false;
		}

		GameObject robotInstance;
		if (!spawnedRobots.TryGetValue(spawnPoint, out robotInstance)) {
			return false;
		}

		spawnedRobots.Remove(spawnPoint);
		if (robotInstance != null) {
			Destroy(robotInstance);
		}

		return true;
	}

	private void BindRobot(GameObject robotInstance, RobotSpawnPoint spawnPoint) {
		if (robotInstance == null || spawnPoint == null) {
			return;
		}

		Robot robot;
		if (!robotInstance.TryGetComponent<Robot>(out robot)) {
			robot = robotInstance.GetComponentInChildren<Robot>(true);
		}

		if (robot != null) {
			robot.BindSpawnPoint(spawnPoint);
		}
	}

	private void UpdateRobotTransform(GameObject robotInstance, RobotSpawnPoint spawnPoint) {
		if (robotInstance == null || spawnPoint == null) {
			return;
		}

		robotInstance.transform.SetPositionAndRotation(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);
		BindRobot(robotInstance, spawnPoint);
	}

	private void ApplyVisibility(GameObject robotInstance, bool isVisible) {
		if (robotInstance == null) {
			return;
		}

		Robot robot;
		if (!robotInstance.TryGetComponent<Robot>(out robot)) {
			robot = robotInstance.GetComponentInChildren<Robot>(true);
		}

		if (robot != null) {
			robot.SetVisible(isVisible);
		}
	}
}
