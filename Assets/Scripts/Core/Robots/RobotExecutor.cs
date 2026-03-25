using UnityEngine;

[RequireComponent(typeof(IRobot))]
public class RobotExecutor : MonoBehaviour {
	private IRobot robot;
	private RobotCommandQueue queue;
	private IRobotCommand current;

	void Awake() {
		robot = GetComponent<IRobot>();
		queue = new RobotCommandQueue();
	}

	void Update() {
		if (robot == null)
			return;

		if (current == null) {
			// Only dequeue next command when there is no current running command
			// and the next enqueued command (if any) is not an ICompletable already completed.
			var next = queue.Peek();
			if (next != null) {
				if (next is ICompletable comp) {
					if (!comp.IsCompleted) {
						current = queue.Dequeue();
					}
				} else {
					current = queue.Dequeue();
				}
			}
		}

		if (current == null)
			return;


		// Set current robot in execution context for commands that need runtime robot.
		CommandExecutionContext.CurrentRobot = robot;

		if (current != null) {
			try {
				// Execute current; completion is driven by the command's own IsCompleted when available.
				bool tickResult = current.Tick(robot, Time.deltaTime);
				if (current is ICompletable comp) {
					if (comp.IsCompleted) {
						current = null;
					}
				} else {
					// legacy commands: if Tick returned true, consider completed
					if (tickResult) current = null;
				}
			} catch (ValidationError vex) {
				Debug.LogWarning($"[RobotExecutor] {vex}");
				CancelAll();
			} catch (System.Exception ex) {
				Debug.LogWarning($"[RobotExecutor] Runtime error: {ex.Message}");
				CancelAll();
			}
		}

		CommandExecutionContext.CurrentRobot = null;
	}

	public RobotCommandQueue CommandQueue => queue;

	// Allow enqueuing EnqueuedCommand wrappers
	public void Enqueue(IRobotCommand cmd) {
		queue.Enqueue(cmd);
	}

	public void CancelAll() {
		current = null;
		if (queue != null) {
			queue.Clear();
		}
	}
}