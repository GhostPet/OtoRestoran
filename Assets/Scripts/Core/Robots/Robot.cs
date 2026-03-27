using UnityEngine;

public class Robot : MonoBehaviour {
	[SerializeField] private Renderer[] targetRenderers;
	[SerializeField] private Collider[] targetColliders;

	private RobotSpawnPoint spawnPoint;

	private void Awake() {
		CacheTargets();
	}

	public void BindSpawnPoint(RobotSpawnPoint targetSpawnPoint) {
		spawnPoint = targetSpawnPoint;
		SnapToSpawnPoint();
	}

	public void SnapToSpawnPoint() {
		if (spawnPoint == null) {
			return;
		}

		transform.SetPositionAndRotation(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);
	}

	public void SetVisible(bool isVisible) {
		CacheTargets();

		for (int i = 0; i < targetRenderers.Length; i++) {
			Renderer targetRenderer = targetRenderers[i];
			if (targetRenderer != null) {
				targetRenderer.enabled = isVisible;
			}
		}

		for (int i = 0; i < targetColliders.Length; i++) {
			Collider targetCollider = targetColliders[i];
			if (targetCollider != null) {
				targetCollider.enabled = isVisible;
			}
		}
	}

	private void CacheTargets() {
		if (targetRenderers == null || targetRenderers.Length == 0) {
			targetRenderers = GetComponentsInChildren<Renderer>(true);
		}

		if (targetColliders == null || targetColliders.Length == 0) {
			targetColliders = GetComponentsInChildren<Collider>(true);
		}
	}
}
