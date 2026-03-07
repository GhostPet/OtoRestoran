using UnityEngine;

public interface IRobot {
	Vector3 Position { get; }
	void MoveTo(Vector3 worldPosition);
	void StartMoveTo(Vector3 worldPosition);
	bool IsMoving { get; }
}