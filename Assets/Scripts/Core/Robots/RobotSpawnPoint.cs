using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RobotSpawnPoint : MonoBehaviour, IPlaceableLifecycle {
	private static readonly List<RobotSpawnPoint> allSpawnPoints = new();

	[SerializeField] private Transform spawnAnchor;

	public static IReadOnlyList<RobotSpawnPoint> AllSpawnPoints => allSpawnPoints;
	public Vector3 SpawnPosition => spawnAnchor != null ? spawnAnchor.position : transform.position;
	public Quaternion SpawnRotation => spawnAnchor != null ? spawnAnchor.rotation : transform.rotation;

	private void OnEnable() {
		if (!allSpawnPoints.Contains(this)) allSpawnPoints.Add(this);
	}

	private void OnDisable() {
		allSpawnPoints.Remove(this);
	}

	public void OnPlaced(PlaceableObject placedObject) {
	}

	public void OnRemoved(PlaceableObject placedObject) {
	}
}
