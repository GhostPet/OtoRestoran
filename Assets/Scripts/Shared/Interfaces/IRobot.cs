using UnityEngine;

public interface IRobot {
	Vector3 Position { get; }
	float MoveSpeed { get; }
	void MoveTo(Vector3 worldPosition);
}