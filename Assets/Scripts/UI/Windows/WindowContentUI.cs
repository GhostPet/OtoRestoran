using UnityEngine;

public abstract class WindowContentUI : MonoBehaviour {
	private GameWindowUI window;

	public GameWindowUI Window => window;

	public void BindWindow(GameWindowUI ownerWindow) {
		window = ownerWindow;
		OnWindowBound(ownerWindow);
	}

	protected virtual void OnWindowBound(GameWindowUI ownerWindow) {
	}
}
