using UnityEngine;
using RestAutoRant.Shared;
using RestAutoRant.Shared.Interfaces;

namespace RestAutoRant.Programming.Commands {
	public class MoveToCommand : IRobotCommand {
		private Vector3 target;
		private const float threshold = 0.05f;

		public MoveToCommand(Vector3 target) {
			this.target = target;
		}

		public bool Tick(IRobot robot, float deltaTime) {
			Vector3 current = robot.Position;

			if (Vector3.Distance(current, target) <= threshold)
				return true;

			Vector3 next = Vector3.MoveTowards(
				current,
				target,
				robot.MoveSpeed * deltaTime
			);

			robot.MoveTo(next);

			return false;
		}
	}
}
