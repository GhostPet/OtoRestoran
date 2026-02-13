using UnityEngine;

namespace RestAutoRant.Programming.Commands {
	public class MoveToCommand : IRobotCommand {
		private Vector3 target;
		public MoveToCommand(Vector3 target) {
			this.target = target;
		}

		public bool Tick(IRobot robot, float deltaTime) {

			robot.MoveTo(target);

			return true;
		}
	}
}
