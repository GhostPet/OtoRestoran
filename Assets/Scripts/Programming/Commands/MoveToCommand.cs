using System.Collections.Generic;
using UnityEngine;

public class MoveToCommand : IRobotCommand {
	private bool _done;

    // Return the execution line provided by the interpreter context.
    // We no longer try to extract line info from `args` — the interpreter must set
    // CommandExecutionContext.CurrentLine before calling Tick.
    private int GetExecutionLine() {
        return CommandExecutionContext.CurrentLine;
    }

    public int ExpectedArgumentCount => 1; // Tek argüman: tuple, Vector3 veya Transform

	public bool Tick(params object[] args) {

		// Two-phase tick pattern like other commands:
		// - first call: validate + start action, return false
		// - next call: finish and return true
		if (!_done) {
			if (args.Length != 1) {
				int line = GetExecutionLine();
				throw new InvalidArgumentCountError(
					"move_to",
					ExpectedArgumentCount, args.Length, line);
			}

			Vector3 target;
			var arg = args[0];

			if (arg is List<float> list) {
				if (list.Count < 2 || list.Count > 3) {
					int line = GetExecutionLine();
					throw new InvalidArgumentCountError(
						"move_to", 2, list.Count, line);
					}

				float x = list[0];
				float z = list[1];
				target = new Vector3(x, 0f, z);
			}
			else if (arg is Vector3 vec) {
				target = vec;
			}
			else if (arg is Transform t) {
				target = t.position;
			} else {
				int line = GetExecutionLine();
				throw new InvalidAssignmentError(
					"move_to",
					"argument must be (x,z) tuple, (x,y,z) tuple, Vector3, or Transform",
					line);
			}

			// Start moving (for now we just log and mark started)
			int startLine = GetExecutionLine();
			Debug.Log($"[MoveToCommand] Starting move_to to {target} (line {startLine})");
			// TODO: integrate with Robot.MoveTo and set _done when arrived
			_done = true;
			return false;
		}
		else {
			// finish
			_done = false;
			Debug.Log($"[MoveToCommand] Completed move_to (line {GetExecutionLine()})");
			return true;
		}
	}

	public void Reset() => _done = false;
}
