using System.Collections.Generic;
using RestAutoRant.Shared;

namespace RestAutoRant.Core.Robots {
	public class RobotCommandQueue {
		private readonly Queue<IRobotCommand> queue = new Queue<IRobotCommand>();

		public void Enqueue(IRobotCommand command) {
			queue.Enqueue(command);
		}

		public IRobotCommand Dequeue() {
			return queue.Count > 0 ? queue.Dequeue() : null;
		}

		public bool HasCommands() {
			return queue.Count > 0;
		}
	}
}
