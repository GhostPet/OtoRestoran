using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotController : MonoBehaviour, IRobot {

	private NavMeshAgent _agent;

	private void Awake() {
		_agent = GetComponent<NavMeshAgent>();
		if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
		_agent.updateRotation = true;
		_agent.updatePosition = true;
	}

	public Vector3 Position => transform.position;

	public bool IsMoving {
		get {
			if (_agent == null) return false;
			if (_agent.pathPending) return true;
			return _agent.remainingDistance > _agent.stoppingDistance;
		}
	}

	public void StartMoveTo(Vector3 worldPosition) {
		if (_agent == null) {
			transform.position = worldPosition;
			return;
		}
		_agent.SetDestination(worldPosition);
	}

	public void MoveTo(Vector3 worldPosition) {
		// immediate fallback using agent Warp to avoid walking
		if (_agent != null) {
			_agent.Warp(worldPosition);
			_agent.ResetPath();
		} else {
			transform.position = worldPosition;
		}
	}

	public void MoveTo(Transform target) {
		MoveTo(target.position);
	}
}

