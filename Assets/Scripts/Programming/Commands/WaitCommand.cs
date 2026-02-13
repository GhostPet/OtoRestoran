namespace RestAutoRant.Programming.Commands {
	public class WaitCommand : IRobotCommand {
		private float remaining;

		public WaitCommand(float seconds) {
			remaining = seconds;
		}

		public bool Tick(IRobot robot, float deltaTime) {
			remaining -= deltaTime;

			UnityEngine.Debug.Log($"Wait: {remaining:F2}s");

			return remaining <= 0f;
		}
	}
}
