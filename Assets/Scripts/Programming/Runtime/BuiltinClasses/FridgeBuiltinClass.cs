public sealed class FridgeBuiltinClass : BuiltinObject {
	private readonly FridgeBehavior fridge;

	public FridgeBuiltinClass(FridgeBehavior fridge, ScriptInvocationContext context)
		   : base(fridge, context) {
		this.fridge = fridge;
	}

	public override string ClassName => "Fridge";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (fridge == null) {
			return false;
		}

		if (string.Equals(memberName, "name", System.StringComparison.OrdinalIgnoreCase)) {
			result = fridge.name;
			return true;
		}

		if (string.Equals(memberName, "position", System.StringComparison.OrdinalIgnoreCase)) {
			result = fridge.transform.position;
			return true;
		}

		if (string.Equals(memberName, "items", System.StringComparison.OrdinalIgnoreCase)) {
			result = Wrap(fridge.CreateSnapshot());
			return true;
		}

		if (string.Equals(memberName, "inventory", System.StringComparison.OrdinalIgnoreCase)) {
			result = fridge.Inventory;
			return true;
		}

		return false;
	}

	public override bool TryInvoke(string memberName, object[] args, int line, out object result) {
		result = null;
		if (fridge == null) {
			return false;
		}

		if (string.Equals(memberName, "take", System.StringComparison.OrdinalIgnoreCase)) {
			if (args == null || args.Length < 1 || args.Length > 2) {
				throw new ValidationError($"take() takes 1 or 2 arguments (line {line})", line);
			}

			if (!Context.IsRobotNearFridge(fridge)) {
				result = false;
				return true;
			}

			ItemSO item = ReadItemArgument(args[0], memberName, line);
			int quantity = args.Length == 2 ? ReadQuantityArgument(args[1], memberName, line) : 1;
			result = fridge.TryTake(item, quantity, Context.RobotInventory);
			return true;
		}

		return false;
	}
}
