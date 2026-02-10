using RestAutoRant.Shared.Interfaces;

namespace RestAutoRant.Shared {
	public interface IRobotCommand {
		bool Tick(IRobot robot, float deltaTime);
	}
}
