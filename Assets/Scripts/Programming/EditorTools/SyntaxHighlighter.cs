using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class SyntaxHighlighter : MonoBehaviour {
	private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal) {
		"def", "if", "elif", "else", "while", "for", "in", "return"
	};

	private static readonly HashSet<string> LiteralKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		"true", "false", "null"
	};

	[SerializeField] private TMP_InputField sourceInputField;
	[SerializeField] private TMP_Text targetText;
	[SerializeField] private bool refreshOnInputChanged = true;
	[SerializeField] private Color defaultColor = new Color32(224, 224, 224, 255);
	[SerializeField] private Color keywordColor = new Color32(86, 156, 214, 255);
	[SerializeField] private Color builtinColor = new Color32(78, 201, 176, 255);
	[SerializeField] private Color stringColor = new Color32(214, 157, 133, 255);
	[SerializeField] private Color numberColor = new Color32(181, 206, 168, 255);
	[SerializeField] private Color commentColor = new Color32(106, 153, 85, 255);
	[SerializeField] private Color operatorColor = new Color32(212, 212, 212, 255);
	[SerializeField] private Color bracketColor = new Color32(255, 215, 0, 255);

	private void OnEnable() {
		Subscribe();
		Refresh();
	}

	private void OnDisable() {
		Unsubscribe();
	}

	public void Bind(TMP_InputField inputField, TMP_Text outputText) {
		Unsubscribe();
		sourceInputField = inputField;
		targetText = outputText;
		Subscribe();
		Refresh();
	}

	public void Refresh() {
		if (targetText == null) {
			return;
		}

		string source = sourceInputField != null ? sourceInputField.text : targetText.text;
		targetText.text = Highlight(source);
	}

	public string Highlight(string source) {
		if (string.IsNullOrEmpty(source)) {
			return " ";
		}

		StringBuilder builder = new StringBuilder(source.Length + 64);
		int index = 0;
		while (index < source.Length) {
			char current = source[index];

			if (TryReadLineComment(source, ref index, builder) || TryReadBlockComment(source, ref index, builder)) {
				continue;
			}

			if (current == '\'' || current == '"') {
				AppendColored(builder, ReadStringLiteral(source, ref index), stringColor);
				continue;
			}

			if (IsNumberStart(source, index)) {
				AppendColored(builder, ReadNumber(source, ref index), numberColor);
				continue;
			}

			if (char.IsLetter(current) || current == '_') {
				string identifier = ReadIdentifier(source, ref index);
				AppendColored(builder, identifier, GetIdentifierColor(identifier));
				continue;
			}

			if (IsBracket(current)) {
				AppendColored(builder, current.ToString(), bracketColor);
				index++;
				continue;
			}

			if (IsOperatorStart(current)) {
				AppendColored(builder, ReadOperator(source, ref index), operatorColor);
				continue;
			}

			builder.Append(EscapeRichText(current.ToString()));
			index++;
		}

		return builder.ToString();
	}

	private void Subscribe() {
		if (!isActiveAndEnabled || !refreshOnInputChanged || sourceInputField == null) {
			return;
		}

		sourceInputField.onValueChanged.RemoveListener(HandleInputChanged);
		sourceInputField.onValueChanged.AddListener(HandleInputChanged);
	}

	private void Unsubscribe() {
		if (sourceInputField == null) {
			return;
		}

		sourceInputField.onValueChanged.RemoveListener(HandleInputChanged);
	}

	private void HandleInputChanged(string _) {
		Refresh();
	}

	private bool TryReadLineComment(string source, ref int index, StringBuilder builder) {
		if (index >= source.Length) {
			return false;
		}

		if (source[index] == '#') {
			AppendColored(builder, ReadUntilNewLine(source, ref index), commentColor);
			return true;
		}

		if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/') {
			AppendColored(builder, ReadUntilNewLine(source, ref index), commentColor);
			return true;
		}

		return false;
	}

	private bool TryReadBlockComment(string source, ref int index, StringBuilder builder) {
		if (index + 1 >= source.Length || source[index] != '/' || source[index + 1] != '*') {
			return false;
		}

		int start = index;
		index += 2;
		while (index < source.Length) {
			if (source[index] == '*' && index + 1 < source.Length && source[index + 1] == '/') {
				index += 2;
				break;
			}

			index++;
		}

		AppendColored(builder, source.Substring(start, index - start), commentColor);
		return true;
	}

	private string ReadUntilNewLine(string source, ref int index) {
		int start = index;
		while (index < source.Length && source[index] != '\n') {
			index++;
		}

		return source.Substring(start, index - start);
	}

	private string ReadStringLiteral(string source, ref int index) {
		int start = index;
		char quote = source[index];
		index++;

		while (index < source.Length) {
			char current = source[index];
			if (current == '\\') {
				index += Mathf.Min(2, source.Length - index);
				continue;
			}

			index++;
			if (current == quote) {
				break;
			}
		}

		return source.Substring(start, index - start);
	}

	private string ReadNumber(string source, ref int index) {
		int start = index;
		bool seenDot = false;

		if (source[index] == '.') {
			seenDot = true;
			index++;
		}

		while (index < source.Length) {
			char current = source[index];
			if (char.IsDigit(current)) {
				index++;
				continue;
			}

			if (current == '.' && !seenDot) {
				seenDot = true;
				index++;
				continue;
			}

			break;
		}

		return source.Substring(start, index - start);
	}

	private string ReadIdentifier(string source, ref int index) {
		int start = index;
		while (index < source.Length) {
			char current = source[index];
			if (!char.IsLetterOrDigit(current) && current != '_') {
				break;
			}

			index++;
		}

		return source.Substring(start, index - start);
	}

	private string ReadOperator(string source, ref int index) {
		int start = index;
		index++;

		if (index < source.Length) {
			char current = source[start];
			char next = source[index];
			if ((current == '=' || current == '!' || current == '<' || current == '>') && next == '=') {
				index++;
			}
		}

		return source.Substring(start, index - start);
	}

	private bool IsNumberStart(string source, int index) {
		char current = source[index];
		if (char.IsDigit(current)) {
			return true;
		}

		return current == '.' && index + 1 < source.Length && char.IsDigit(source[index + 1]);
	}

	private bool IsBracket(char value) {
		return value == '(' || value == ')' || value == '[' || value == ']' || value == '{' || value == '}';
	}

	private bool IsOperatorStart(char value) {
		return value == '+' || value == '-' || value == '*' || value == '/' || value == '=' || value == '!' || value == '<' || value == '>' || value == ':' || value == '.' || value == ',';
	}

	private Color GetIdentifierColor(string identifier) {
		if (Keywords.Contains(identifier) || LiteralKeywords.Contains(identifier)) {
			return keywordColor;
		}

		if (BuiltinFunctions.IsBuiltin(identifier)) {
			return builtinColor;
		}

		if (BuiltinCommandRegistry.IsBuiltin(identifier)) {
			return builtinColor;
		}

		return defaultColor;
	}

	private void AppendColored(StringBuilder builder, string value, Color color) {
		builder.Append("<color=#");
		builder.Append(ColorUtility.ToHtmlStringRGBA(color));
		builder.Append('>');
		builder.Append(EscapeRichText(value));
		builder.Append("</color>");
	}

	private string EscapeRichText(string value) {
		if (string.IsNullOrEmpty(value)) {
			return string.Empty;
		}

		return value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;");
	}
}
