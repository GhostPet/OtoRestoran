using UnityEngine;
using UnityEngine.UI;

public class Robot_Test_MenuItem_Button : MonoBehaviour
{
	public Sprite robotSprite;
	public RobotExecutor robotExecutor;

	public Button button;
	public OpenCodeEditor openCodeEditor;

	public void Awake() {
		if (button != null) {
			button.onClick.AddListener(OnButtonClick);
		}
	}

	public void OnButtonClick() {
		openCodeEditor.OpenEditor(robotSprite, robotExecutor);
	}
}
