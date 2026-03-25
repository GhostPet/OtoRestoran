public sealed class RobotInventorySlotBuiltinClass : BuiltinObject {
	private readonly RobotInventorySlot slot;

	public RobotInventorySlotBuiltinClass(RobotInventorySlot slot, ScriptInvocationContext context)
		: base(slot, context) {
		this.slot = slot;
	}

	public override string ClassName => "RobotSlot";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (slot == null) {
			return false;
		}

		if (string.Equals(memberName, "item", System.StringComparison.OrdinalIgnoreCase)) {
			result = slot.Item;
			return true;
		}

		if (string.Equals(memberName, "quantity", System.StringComparison.OrdinalIgnoreCase)) {
			result = (float)slot.Quantity;
			return true;
		}

		if (string.Equals(memberName, "is_empty", System.StringComparison.OrdinalIgnoreCase)) {
			result = slot.IsEmpty;
			return true;
		}

		return false;
	}
}
