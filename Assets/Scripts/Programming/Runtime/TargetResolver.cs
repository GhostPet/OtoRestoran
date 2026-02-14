using UnityEngine;

public static class TargetResolver {

	public static Vector3 Resolve(IRobot robot, TableLogic table) {
		if (robot == null || table == null)
			return Vector3.zero;

		Transform servePoint = table.GetNearestServePoint(robot.Position);
		if (servePoint != null)
			return servePoint.position;

		return table.Location.position;
	}
}
