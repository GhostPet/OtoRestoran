using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RobotSpawnPoint : MonoBehaviour, IPlaceableLifecycle {
	private static readonly List<RobotSpawnPoint> allSpawnPoints = new();

	[SerializeField] private string bindingKey;
	[SerializeField] private string displayName;
	[SerializeField] private Transform spawnAnchor;

	public static IReadOnlyList<RobotSpawnPoint> AllSpawnPoints => allSpawnPoints;
	public string BindingKey => string.IsNullOrWhiteSpace(bindingKey) ? name : bindingKey;
	public string ProgramBindingKey => BindingKey + "_" + GetInstanceID();
	public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
	public Vector3 SpawnPosition => spawnAnchor != null ? spawnAnchor.position : transform.position;
	public Quaternion SpawnRotation => spawnAnchor != null ? spawnAnchor.rotation : transform.rotation;

	private void OnValidate() {
		if (string.IsNullOrWhiteSpace(bindingKey)) {
			bindingKey = Guid.NewGuid().ToString("N");
		}
	}

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
