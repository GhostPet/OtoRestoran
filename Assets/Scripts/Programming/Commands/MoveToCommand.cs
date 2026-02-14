using UnityEngine;

public class MoveToCommand : IRobotCommand {

	private Vector3? resolvedTarget;
	private TableLogic tableTarget;
	private bool executed;

	public MoveToCommand(Vector3 target) {
		resolvedTarget = target;
	}

	public MoveToCommand(TableLogic table) {
		tableTarget = table;
		resolvedTarget = null;
	}

	public bool Tick(IRobot robot, float deltaTime) {
		if (robot == null || executed)
			return true;

		Vector3 target = resolvedTarget.HasValue
			? resolvedTarget.Value
			: TargetResolver.Resolve(robot, tableTarget);

		robot.MoveTo(target);
		executed = true;
		return true;
	}
}
