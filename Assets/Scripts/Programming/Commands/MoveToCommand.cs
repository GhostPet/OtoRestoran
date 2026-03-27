using System.Collections;
using UnityEngine;

public class MoveToCommand : IRobotCommand, ICompletable {
	private bool _done;
	private bool _started = false;
	public bool IsCompleted { get => _done; set => _done = value; }

	// Return the execution line provided by the interpreter context.
	// We no longer try to extract line info from `args` — the interpreter must set
	// CommandExecutionContext.CurrentLine before calling Tick.
	private int GetExecutionLine() {
		return CommandExecutionContext.CurrentLine;
	}

	public int ExpectedArgumentCount => 1;

	public bool Tick(params object[] args) {

		// Accept either:
		// - a single argument which is a list/Vector3/Transform, or
		// - two numeric args (x, z) or three numeric args (x, y, z)
		if (args.Length < 1 || args.Length > 3) {
			int line = GetExecutionLine();
			throw new InvalidArgumentCountError("move_to", ExpectedArgumentCount, args.Length, line);
		}

		Vector3 target;
		if (args.Length == 1) {
			var arg = BuiltinClassRegistry.UnwrapValue(args[0]);

			if (arg is IList rawList) {
				int count = rawList.Count;
				if (count < 2 || count > 3) {
					int line = GetExecutionLine();
					throw new InvalidArgumentCountError("move_to", 2, count, line);
				}

				int lineForErr = GetExecutionLine();
				float ToFloat(object o) {
					o = BuiltinClassRegistry.UnwrapValue(o);
					if (o is float f) return f;
					if (o is int i) return (float)i;
					if (o is double d) return (float)d;
					throw new InvalidAssignmentError("move_to", "coordinates must be numbers", lineForErr);
				}

				float x = ToFloat(rawList[0]);
				if (count == 2) {
					float z = ToFloat(rawList[1]);
					target = new Vector3(x, 0f, z);
				} else {
					float y = ToFloat(rawList[1]);
					float z = ToFloat(rawList[2]);
					target = new Vector3(x, y, z);
				}
			} else if (arg is Vector3 vec) {
				target = vec;
			} else if (arg is TableBehavior table) {
				var currentRobot = CommandExecutionContext.CurrentRobot;
				// Determine a reference position to select the nearest serve point on the table.
				Vector3 referencePos;
				if (currentRobot != null) {
					referencePos = currentRobot.Position;
				} else {
					// No robot available in context: use the table location as a fallback reference
					if (table.Location != null) referencePos = table.Location.position;
					else referencePos = table.transform.position;
				}

				var servePoint = table.GetNearestServePoint(referencePos);
				if (servePoint != null) target = servePoint.position;
				else if (table.Location != null) target = table.Location.position;
				else target = table.transform.position;
			} else if (arg is BaseRestaurantObject restaurantObject) {
				target = restaurantObject.transform.position;
			} else if (arg is Component component) {
				target = component.transform.position;
			} else if (arg is GameObject gameObject) {
				target = gameObject.transform.position;
			} else if (arg is Transform t) {
				target = t.position;
			} else {
				int line = GetExecutionLine();
				throw new InvalidAssignmentError("move_to", "argument must be (x,z) tuple, (x,y,z) tuple, Vector3, restaurant object, Component, GameObject, TableBehavior, or Transform", line);
			}
		} else {
			// args.Length == 2 or 3 -> expect numeric coordinate arguments
			int lineForErr = GetExecutionLine();
			float ToFloatObj(object o) {
				o = BuiltinClassRegistry.UnwrapValue(o);
				if (o is float f) return f;
				if (o is int i) return (float)i;
				if (o is double d) return (float)d;
				throw new InvalidAssignmentError("move_to", "coordinates must be numbers", lineForErr);
			}

			float x = ToFloatObj(args[0]);
			if (args.Length == 2) {
				float z = ToFloatObj(args[1]);
				target = new Vector3(x, 0f, z);
			} else {
				float y = ToFloatObj(args[1]);
				float z = ToFloatObj(args[2]);
				target = new Vector3(x, y, z);
			}
		}

		// Execute: if a robot is present in the execution context, use it.
		var robot = CommandExecutionContext.CurrentRobot;
		if (robot != null) {
			// If not started yet, tell robot to start moving and wait until arrival
			if (!_started) {
				robot.StartMoveTo(target);
				_started = true;
				Debug.Log($"[MoveToCommand] Started robot move_to {target} (line {GetExecutionLine()})");
				return false; // still running
			}

			// If started, check if robot still moving
			if (robot.IsMoving) {
				return false;
			}

			// finished
			_started = false;
			_done = true;
			Debug.Log($"[MoveToCommand] Robot arrived at {target} (line {GetExecutionLine()})");
			return true;
		}

		// No robot: just log and complete
		Debug.Log($"[MoveToCommand] No robot available, would move to {target} (line {GetExecutionLine()})");
		_done = true;
		return true;
	}

	public void Reset() { _done = false; _started = false; }
}
