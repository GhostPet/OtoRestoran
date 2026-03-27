public sealed class FurnaceBuiltinClass : BuiltinObject {
	private readonly OvenBehavior oven;

	public FurnaceBuiltinClass(OvenBehavior oven, ScriptInvocationContext context)
		: base(oven, context) {
		this.oven = oven;
	}

	public override string ClassName => "Furnace";

	public override bool TryGetMember(string memberName, out object result) {
		result = null;
		if (oven == null) {
			return false;
		}

		if (string.Equals(memberName, "state", System.StringComparison.OrdinalIgnoreCase)) {
			result = oven.State.ToString();
			return true;
		}

		if (string.Equals(memberName, "active_recipe", System.StringComparison.OrdinalIgnoreCase)) {
			result = oven.ActiveRecipe;
			return true;
		}

		return false;
	}

	public override bool TryInvoke(string memberName, object[] args, int line, out object result) {
		result = null;
		if (oven == null) {
			return false;
		}

		if (!string.Equals(memberName, "place", System.StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(memberName, "cook", System.StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(memberName, "take", System.StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(memberName, "clean", System.StringComparison.OrdinalIgnoreCase)) {
			return false;
		}

		if (!Context.IsRobotNearOven(oven)) {
			result = string.Equals(memberName, "take", System.StringComparison.OrdinalIgnoreCase) ? false : null;
			return true;
		}

		if (string.Equals(memberName, "place", System.StringComparison.OrdinalIgnoreCase)) {
			if (args == null || args.Length == 0) {
				result = oven.Place(Context.RobotInventory);
				return true;
			}

			result = oven.Place(Context.RobotInventory, ReadItemArguments(args, memberName, line));
			return true;
		}

		EnsureNoArguments(memberName, args, line);

		if (string.Equals(memberName, "cook", System.StringComparison.OrdinalIgnoreCase)) {
			oven.Cook();
			return true;
		}

		if (string.Equals(memberName, "take", System.StringComparison.OrdinalIgnoreCase)) {
			result = oven.Take(Context.RobotInventory);
			return true;
		}

		oven.Clean();
		return true;
	}
}
