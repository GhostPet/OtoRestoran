using System.Text;
using TMPro;
using UnityEngine;

// Component to attach to an in-game code editor TMP_InputField.
// Handles Tab -> spaces and automatic indent on Enter.
public class CodeEditor : MonoBehaviour {
	public TMP_InputField inputField;
	public int TabSize = 4;

	private void OnEnable() {
		if (inputField != null) inputField.onValidateInput += ValidateInput;
	}

	private void OnDisable() {
		if (inputField != null) inputField.onValidateInput -= ValidateInput;
	}

	private char ValidateInput(string text, int charIndex, char addedChar) {
		if (addedChar == '\t') {
			string insert = new string(' ', TabSize);
			InsertTextAt(insert, charIndex);
			return '\0';
		}

		if (addedChar == '\n' || addedChar == '\r') {
			int lineStart = text.LastIndexOf('\n', System.Math.Max(0, charIndex - 1));
			if (lineStart < 0) lineStart = 0; else lineStart = lineStart + 1;
			var sb = new StringBuilder();
			while (lineStart < text.Length) {
				char c = text[lineStart];
				if (c == ' ' || c == '\t') { sb.Append(c); lineStart++; } else break;
			}
			string indent = sb.ToString();
			string insert = "\n" + indent;
			InsertTextAt(insert, charIndex);
			return '\0';
		}

		return addedChar;
	}

	private void InsertTextAt(string insert, int index) {
		var txt = inputField.text ?? string.Empty;
		if (index < 0) index = 0;
		if (index > txt.Length) index = txt.Length;
		string newText = txt.Substring(0, index) + insert + txt.Substring(index);
		inputField.text = newText;
		int newPos = index + insert.Length;
		inputField.stringPosition = newPos;
		inputField.caretPosition = newPos;
		inputField.selectionAnchorPosition = newPos;
		inputField.selectionFocusPosition = newPos;
		inputField.ForceLabelUpdate();
	}
}
