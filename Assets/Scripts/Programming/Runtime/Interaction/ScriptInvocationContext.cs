using UnityEngine;

public readonly struct ScriptInvocationContext {
	private const float DefaultInteractionDistance = 1.75f;

	public ScriptInvocationContext(IRobot robot, RobotExecutor executor) {
		Robot = robot;
		Executor = executor;
		RobotInventory = ResolveRobotInventory(robot);
	}

	public IRobot Robot { get; }

	public RobotExecutor Executor { get; }

	public RobotInventory RobotInventory { get; }

	public bool IsRobotNearCustomerTable(Customer customer) {
		if (customer == null || Robot == null) {
			return false;
		}

		TableBehavior table = customer.Table;
		if (table == null) {
			return false;
		}

		Vector3 interactionPoint = TargetResolver.Resolve(Robot, table);
		float sqrDistance = (Robot.Position - interactionPoint).sqrMagnitude;
		return sqrDistance <= DefaultInteractionDistance * DefaultInteractionDistance;
	}

	public bool IsRobotNearOven(OvenBehavior oven) {
		if (oven == null || Robot == null) {
			return false;
		}

		float sqrDistance = (Robot.Position - oven.transform.position).sqrMagnitude;
		return sqrDistance <= DefaultInteractionDistance * DefaultInteractionDistance;
	}

	public bool IsRobotNearFridge(StorageInventory fridge) {
		if (fridge == null || Robot == null) {
			return false;
		}

		float sqrDistance = (Robot.Position - fridge.transform.position).sqrMagnitude;
		return sqrDistance <= DefaultInteractionDistance * DefaultInteractionDistance;
	}

	public bool IsRobotNearFridge(FridgeBehavior fridge) {
		if (fridge == null || Robot == null) {
			return false;
		}

		float sqrDistance = (Robot.Position - fridge.transform.position).sqrMagnitude;
		return sqrDistance <= DefaultInteractionDistance * DefaultInteractionDistance;
	}

	public static ScriptInvocationContext Create(RobotExecutor executor) {
		IRobot robot = ResolveCurrentRobot(executor);
		return new ScriptInvocationContext(robot, executor);
	}

	private static IRobot ResolveCurrentRobot(RobotExecutor executor) {
		IRobot currentRobot = CommandExecutionContext.CurrentRobot;
		if (currentRobot != null) {
			return currentRobot;
		}

		if (executor != null && executor.TryGetComponent<IRobot>(out IRobot executorRobot)) {
			return executorRobot;
		}

		return null;
	}

	private static RobotInventory ResolveRobotInventory(IRobot robot) {
		Component robotComponent = robot as Component;
		if (robotComponent == null) {
			return null;
		}

		return robotComponent.GetComponent<RobotInventory>();
	}
}
