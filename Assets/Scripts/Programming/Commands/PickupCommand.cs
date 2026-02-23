using UnityEngine;

public class PickupCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private bool _done = false;

	public bool Tick(params object[] args) {

		int line = CommandExecutionContext.CurrentLine;
		var robot = CommandExecutionContext.CurrentRobot;


		if (robot != null) {
			// If robot has to be at object first, ensure not moving
			if (robot.IsMoving) {
				// still moving, try again next tick
				return false;
			}

			// simulate pickup on robot (no-op for now)
			Debug.Log($"[PickupCommand] Robot picking up {args[0]} (line {line})");
			return true;
		}

		Debug.Log($"Picking up {args[0]} (line {line})");
		return true;
	}

	public void Reset() {
		_done = false;
	}
}
