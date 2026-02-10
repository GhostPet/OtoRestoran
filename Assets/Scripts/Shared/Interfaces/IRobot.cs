using UnityEngine;

namespace RestAutoRant.Shared.Interfaces {
	public interface IRobot {
		Vector3 Position { get; }
		float MoveSpeed { get; }
		void MoveTo(Vector3 worldPosition);
	}
}