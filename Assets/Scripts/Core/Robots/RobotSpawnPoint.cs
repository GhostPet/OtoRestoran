using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RobotSpawnPoint : MonoBehaviour, IPlaceableLifecycle {
	private static readonly List<RobotSpawnPoint> allSpawnPoints = new();

	[SerializeField] private string bindingKey;
	[SerializeField] private string displayName;
	[SerializeField] private Transform spawnAnchor;

	private string programBindingKey;
	private bool isPlaced;
	private RobotCodeRegistry robotCodeRegistry;
	private RobotSpawnManager robotSpawnManager;

	public static IReadOnlyList<RobotSpawnPoint> AllSpawnPoints => allSpawnPoints;
	public string BindingKey => string.IsNullOrWhiteSpace(bindingKey) ? name : bindingKey;
	public string ProgramBindingKey {
		get {
			EnsureProgramBindingKey();
			return programBindingKey;
		}
	}
	public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
	public Vector3 SpawnPosition => spawnAnchor != null ? spawnAnchor.position : transform.position;
	public Quaternion SpawnRotation => spawnAnchor != null ? spawnAnchor.rotation : transform.rotation;

	private void Awake() {
		EnsureProgramBindingKey();
	}

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
		if (isPlaced) {
			return;
		}

		isPlaced = true;
		ResolveReferences();

		if (robotCodeRegistry != null) {
			robotCodeRegistry.HandleSpawnPointPlaced(this);
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.HandleSpawnPointPlaced(this);
		}
	}

	public void OnRemoved(PlaceableObject placedObject) {
		if (!isPlaced) {
			return;
		}

		isPlaced = false;
		ResolveReferences();

		if (robotCodeRegistry != null) {
			robotCodeRegistry.HandleSpawnPointRemoved(this);
		}

		if (robotSpawnManager != null) {
			robotSpawnManager.HandleSpawnPointRemoved(this);
		}
	}

	private void EnsureProgramBindingKey() {
		if (string.IsNullOrWhiteSpace(programBindingKey)) {
			programBindingKey = BindingKey + "_" + Guid.NewGuid().ToString("N");
		}
	}

	private void ResolveReferences() {
		if (robotCodeRegistry == null) {
			robotCodeRegistry = FindAnyObjectByType<RobotCodeRegistry>();
		}

		if (robotSpawnManager == null) {
			robotSpawnManager = FindAnyObjectByType<RobotSpawnManager>();
		}
	}
}
