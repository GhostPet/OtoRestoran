using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class CodeEditor : MonoBehaviour {
	private TMP_InputField inputField;
	public int TabSize = 4;

	private void Awake() {
		inputField = GetComponent<TMP_InputField>();
	}

	private void OnEnable() {
		if (inputField != null) inputField.onValidateInput += ValidateInput;
	}

	private void OnDisable() {
		if (inputField != null) inputField.onValidateInput -= ValidateInput;
	}

	private char ValidateInput(string text, int charIndex, char addedChar) {
		if (addedChar == '\t') {
			string insert = new(' ', TabSize);
			InsertTextAt(insert, charIndex);
			return '\0';
		}

		if (addedChar == '\n' || addedChar == '\r') {
			int lineStart = text.LastIndexOf('\n', System.Math.Max(0, charIndex - 1));
			if (lineStart < 0) lineStart = 0; else lineStart++;
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
		string newText = txt[..index] + insert + txt[index..];
		inputField.text = newText;
		int newPos = index + insert.Length;
		inputField.stringPosition = newPos;
		inputField.caretPosition = newPos;
		inputField.selectionAnchorPosition = newPos;
		inputField.selectionFocusPosition = newPos;
		inputField.ForceLabelUpdate();
	}
}
