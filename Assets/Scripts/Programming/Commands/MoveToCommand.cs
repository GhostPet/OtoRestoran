using UnityEngine;
using RestAutoRant.Shared.Interfaces;

namespace RestAutoRant.Programming.Commands {
	public class MoveToCommand : IRobotCommand {
		private readonly Vector3 _targetPosition;

		public MoveToCommand(Vector3 targetPosition) {
			_targetPosition = targetPosition;
		}

		public bool CanExecute(IRobot robot) {
			return robot != null;
		}

		public void Execute(IRobot robot) {
			robot.MoveTo(_targetPosition);
		}

		public bool IsCompleted => true;
	}
}