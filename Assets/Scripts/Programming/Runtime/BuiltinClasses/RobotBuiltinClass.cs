using UnityEngine;

public sealed class RobotBuiltinClass : BuiltinObject {
	private readonly IRobot robot;
	private readonly RobotInventory inventory;

	public RobotBuiltinClass(IRobot robot, ScriptInvocationContext context)
		: base(robot as Object, context) {
		this.robot = robot;
		this.inventory = ResolveInventory(robot);
	}

	public override string ClassName => "Robot";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (robot == null) {
			return false;
		}

		if (string.Equals(memberName, "position", System.StringComparison.OrdinalIgnoreCase)) {
			result = robot.Position;
			return true;
		}

		if (string.Equals(memberName, "is_moving", System.StringComparison.OrdinalIgnoreCase)) {
			result = robot.IsMoving;
			return true;
		}

		if (string.Equals(memberName, "inventory_slots", System.StringComparison.OrdinalIgnoreCase)) {
			result = inventory != null ? Wrap(inventory.Slots) : Wrap(null);
			return true;
		}

		if (string.Equals(memberName, "slot_count", System.StringComparison.OrdinalIgnoreCase)) {
			result = inventory != null ? (float)inventory.SlotCount : 0f;
			return true;
		}

		return false;
	}

	private static RobotInventory ResolveInventory(IRobot robot) {
		Component component = robot as Component;
		if (component == null) {
			return null;
		}

		return component.GetComponent<RobotInventory>();
	}
}
