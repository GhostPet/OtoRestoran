using UnityEngine;


public class RobotController : MonoBehaviour, IRobot {
	[SerializeField] private float moveSpeed = 2f;

	public Vector3 Position => transform.position;
	public float MoveSpeed => moveSpeed;

	public void MoveTo(Vector3 worldPosition) {
		transform.position = worldPosition;
	}
}

