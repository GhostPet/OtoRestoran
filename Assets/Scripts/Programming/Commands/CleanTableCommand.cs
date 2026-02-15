using UnityEngine;

public class CleanTableCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private bool _done = false;

	public bool Tick(params object[] args) {
		if (!_done) {
			Debug.Log($"Cleaning table {args[0]}");
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
