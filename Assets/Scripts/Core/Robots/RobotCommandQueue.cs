using System.Collections.Generic;

public class RobotCommandQueue {
	private readonly Queue<IRobotCommand> queue = new();

	public void Enqueue(IRobotCommand command) {
		queue.Enqueue(command);
	}

	public IRobotCommand Dequeue() {
		return queue.Count > 0 ? queue.Dequeue() : null;
	}

	public bool HasCommands() {
		return queue.Count > 0;
	}

	// Peek without dequeuing
	public IRobotCommand Peek() {
		return queue.Count > 0 ? queue.Peek() : null;
	}
}
