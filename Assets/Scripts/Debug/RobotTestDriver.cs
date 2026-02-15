using UnityEngine;

namespace RestAutoRant.Debug {
	public class RobotTestDriver : MonoBehaviour {
		public RobotExecutor executor;

		private void Start() {
			UnityEngine.Debug.Log("TEST START");

			/*executor.CommandQueue.Enqueue(new MoveToCommand(new Vector3(1, 1, 1)));
			executor.CommandQueue.Enqueue(new WaitCommand(2f));
			executor.CommandQueue.Enqueue(new MoveToCommand(new Vector3(5, 1, 2)));*/
		}
	}
}
