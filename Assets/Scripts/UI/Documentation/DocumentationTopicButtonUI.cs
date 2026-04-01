using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentationTopicButtonUI : MonoBehaviour {
	[SerializeField] private Button button;
	[SerializeField] private TMP_Text labelText;
	[SerializeField] private Graphic selectedHighlight;
	[SerializeField] private Color selectedTextColor = Color.white;
	[SerializeField] private Color normalTextColor = Color.black;
	[SerializeField] private int indentationWidth = 24;

	private Action clickHandler;

	public void Bind(string title, Action onClicked, bool isSelected, int indentationLevel) {
		clickHandler = onClicked;

		if (labelText != null) {
			string displayTitle = string.IsNullOrWhiteSpace(title) ? "Konu" : title;
			labelText.text = BuildIndentedTitle(displayTitle, indentationLevel);
		}

		if (button != null) {
			button.onClick.RemoveListener(HandleClicked);
			button.onClick.AddListener(HandleClicked);
		}

		SetSelected(isSelected);
	}

	public void SetSelected(bool isSelected) {
		if (selectedHighlight != null) {
			selectedHighlight.gameObject.SetActive(isSelected);
		}

		if (labelText != null) {
			labelText.color = isSelected ? selectedTextColor : normalTextColor;
		}
	}

	private void HandleClicked() {
		clickHandler?.Invoke();
	}

	private string BuildIndentedTitle(string title, int indentationLevel) {
		int indentation = Mathf.Max(0, indentationLevel) * Mathf.Max(0, indentationWidth / 6);
		if (indentation <= 0) {
			return title;
		}

		return new string('\u00A0', indentation) + title;
	}
}
