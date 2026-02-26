using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpenCodeEditor : MonoBehaviour {
	public GameObject windowPrefab;
	public GameCodeRunner gameCodeRunner;

	// imageName: optional name (GameObject.name) of the Image to target among children. If null, first Image found is used.
	public void OpenEditor(Sprite robotSprite, RobotExecutor robot) {
		GameObject window = Instantiate(windowPrefab, this.transform, true);

		RectTransform rect = window.GetComponent<RectTransform>();
		rect.anchoredPosition = Vector2.zero;
		rect.localScale = Vector2.one;

		// Change the robot sprite in the editor window.
		Image[] images = window.GetComponentsInChildren<Image>(true);
		Image img = null;
		foreach (var i in images) {
			if (i.gameObject.name.ToLowerInvariant().Contains("robotimage")) {
				img = i;
				break;
			}
		}
		if (!img) {
			img = window.GetComponentInChildren<Image>(true);
		}
		img.sprite = robotSprite;

		// Add the editor and the robot to the GameCodeRunner.
		CodeEditor editor = window.GetComponentInChildren<CodeEditor>(true);
		Button[] buttons = window.GetComponentsInChildren<Button>(true);
		Button runBtn = null;
		foreach (var b in buttons) {
			if (b.gameObject.name.ToLowerInvariant().Contains("startbutton")) {
				runBtn = b;
				break;
			}
		}
		if (!runBtn) {
			runBtn = window.GetComponentInChildren<Button>(true);
		}

		TMP_InputField input = editor.gameObject.GetComponent<TMP_InputField>();

		// Register the editor with the GameCodeRunner so it wires the run listener now
		gameCodeRunner.RegisterEditor(input, runBtn, robot);
	}
}