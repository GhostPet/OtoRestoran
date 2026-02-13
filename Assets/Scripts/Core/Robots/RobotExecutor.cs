using UnityEngine;

public class RobotExecutor : MonoBehaviour {
	private IRobot robot;
	private RobotCommandQueue queue;
	private IRobotCommand current;

	void Awake() {
		robot = GetComponent<IRobot>();
		queue = new RobotCommandQueue();
	}

	void Update() {
		if (robot == null)
			return;

		if (current == null && queue.HasCommands())
			current = queue.Dequeue();

		if (current == null)
			return;

		if (current.Tick(robot, Time.deltaTime))
			current = null;
	}

	public RobotCommandQueue CommandQueue => queue;
}