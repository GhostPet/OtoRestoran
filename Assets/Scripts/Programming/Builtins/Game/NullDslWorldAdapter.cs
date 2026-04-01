using System;
using System.Collections.Generic;

public sealed class NullDslWorldAdapter : IDslWorldAdapter {
	public IDslRobotAdapter GetCurrentRobot() {
		return null;
	}

	public IReadOnlyList<IDslRobotAdapter> GetRobots() {
		return Array.Empty<IDslRobotAdapter>();
	}

	public IReadOnlyList<IDslTableAdapter> GetTables() {
		return Array.Empty<IDslTableAdapter>();
	}

	public IReadOnlyList<IDslFurnaceAdapter> GetFurnaces() {
		return Array.Empty<IDslFurnaceAdapter>();
	}

	public IReadOnlyList<IDslFridgeAdapter> GetFridges() {
		return Array.Empty<IDslFridgeAdapter>();
	}

	public IReadOnlyList<IDslTrashcanAdapter> GetTrashcans() {
		return Array.Empty<IDslTrashcanAdapter>();
	}

	public IReadOnlyList<IDslOrderAdapter> GetOrders() {
		return Array.Empty<IDslOrderAdapter>();
	}

	public IDslItemAdapter FindItem(string itemName) {
		return null;
	}
}
