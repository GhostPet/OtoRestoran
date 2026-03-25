using UnityEngine;

public class ServeOrderCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private bool _done = false;
	private const float TableInteractionDistance = 1.75f;

	public bool Tick(params object[] args) {
		int line = CommandExecutionContext.CurrentLine;
		if (args == null || args.Length != 1) {
			throw new InvalidArgumentCountError("serve", ExpectedArgumentCount, args == null ? 0 : args.Length, line);
		}

		if (args[0] is not Order order || order == null) {
			throw new InvalidAssignmentError("serve", "argument must be an Order", line);
		}

		if (order.Customer == null) {
			throw new InvalidAssignmentError("serve", "order.customer is null", line);
		}

		if (!_done) {
			var table = order.Customer.Table;
			var robot = CommandExecutionContext.CurrentRobot;
			if (table == null || robot == null) {
				Debug.Log($"[ServeOrderCommand] Serve skipped: robot or customer table missing (line {line})");
				_done = true;
				return false;
			}

			var interactionPoint = TargetResolver.Resolve(robot, table);
			var sqrDistance = (robot.Position - interactionPoint).sqrMagnitude;
			if (sqrDistance > TableInteractionDistance * TableInteractionDistance) {
				Debug.Log($"[ServeOrderCommand] Serve skipped: robot is not near table {table.name} (line {line})");
				_done = true;
				return false;
			}

			RobotInventory inventory = ResolveInventory(robot);
			if (inventory == null) {
				Debug.Log($"[ServeOrderCommand] Serve skipped: robot inventory missing (line {line})");
				_done = true;
				return false;
			}

			if (!order.IsFulfilledBy(inventory)) {
				Debug.Log($"[ServeOrderCommand] Serve skipped: robot inventory does not satisfy order for {order.Customer.name} (line {line})");
				_done = true;
				return false;
			}

			bool served = order.Customer.TryServeOrder(inventory);
			if (!served) {
				Debug.Log($"[ServeOrderCommand] Customer not ready for serving: {order.Customer.name} (line {line})");
				_done = true;
				return false;
			}

			Debug.Log($"[ServeOrderCommand] Served order for customer {order.Customer.name} (line {line})");
			_done = true;
			return false;
		}

		_done = false;
		return true;
	}

	public void Reset() {
		_done = false;
	}

	private static RobotInventory ResolveInventory(IRobot robot) {
		Component robotComponent = robot as Component;
		if (robotComponent == null) {
			return null;
		}

		return robotComponent.GetComponent<RobotInventory>();
	}
}
