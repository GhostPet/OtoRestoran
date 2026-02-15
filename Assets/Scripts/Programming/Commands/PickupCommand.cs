using UnityEngine;

public class PickupCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private bool _done = false;

	public bool Tick(params object[] args) {
		if (!_done) {
			int line = CommandExecutionContext.CurrentLine;
			Debug.Log($"Picking up {args[0]} (line {line})");
			_done = true;
			return false;
		}

		_done = false;
		return true;
	}

	public void Reset() {
		_done = false;
	}
}
