using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public static class DocumentationMarkdownFormatter {
	private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
	private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
	private static readonly Regex ItalicRegex = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
	private static readonly Regex OrderedListRegex = new(@"^(?<index>\d+)\.\s+(?<content>.+)$", RegexOptions.Compiled);

	private const string HeadingOneColor = "#F3D28A";
	private const string HeadingTwoColor = "#95D6FF";
	private const string HeadingThreeColor = "#AEE4BB";
	private const string HeadingFourColor = "#D8CCFF";
	private const string BodyTextColor = "#EEF3F8";
	private const string MutedTextColor = "#B9C5D5";
	private const string RuleColor = "#5F7085";
	private const string QuoteAccentColor = "#92C5FF";
	private const string QuoteTextColor = "#DDE7F2";
	private const string QuoteBackgroundColor = "#22324788";
	private const string InlineCodeTextColor = "#FFE59C";
	private const string InlineCodeBackgroundColor = "#27384C99";
	private const string CodeHeaderTextColor = "#9FD9FF";
	private const string CodeLinkTextColor = "#FFE08A";
	private const string CodeCopiedTextColor = "#9FE3B1";
	private const string CodeHeaderBackgroundColor = "#182331CC";
	private const string CodeBorderColor = "#607A93";
	private const string CodeLineNumberColor = "#7F96AD";
	private const string CodeTextColor = "#F2F7FC";
	private const string CodeBackgroundColor = "#121A2499";

	public static DocumentationFormattedContent Format(string markdown) {
		return Format(markdown, -1);
	}

	public static DocumentationFormattedContent Format(string markdown, int copiedCodeBlockIndex) {
		if (string.IsNullOrWhiteSpace(markdown)) {
			return DocumentationFormattedContent.Empty;
		}

		string normalized = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
		string[] lines = normalized.Split('\n');
		var builder = new StringBuilder();
		var currentCodeLines = new List<string>();
		var codeBlocks = new List<string>();
		bool inCodeBlock = false;

		for (int i = 0; i < lines.Length; i++) {
			string line = lines[i];
			string trimmedLine = line.Trim();

			if (trimmedLine.StartsWith("```")) {
				if (inCodeBlock) {
					AppendCodeBlock(builder, currentCodeLines, codeBlocks, copiedCodeBlockIndex);
					currentCodeLines.Clear();
				}

				inCodeBlock = !inCodeBlock;
				continue;
			}

			if (inCodeBlock) {
				currentCodeLines.Add(line);
				continue;
			}

			AppendFormattedLine(builder, line, trimmedLine);
		}

		if (inCodeBlock) {
			AppendCodeBlock(builder, currentCodeLines, codeBlocks, copiedCodeBlockIndex);
		}

		return new DocumentationFormattedContent(builder.ToString().Trim(), codeBlocks);
	}

	private static void AppendFormattedLine(StringBuilder builder, string originalLine, string trimmedLine) {
		if (builder == null) {
			return;
		}

		if (string.IsNullOrWhiteSpace(trimmedLine)) {
			builder.AppendLine();
			return;
		}

		if (TryAppendHeading(builder, trimmedLine, "#### ", HeadingFourColor, 102)) {
			return;
		}

		if (TryAppendHeading(builder, trimmedLine, "### ", HeadingThreeColor, 108)) {
			return;
		}

		if (TryAppendHeading(builder, trimmedLine, "## ", HeadingTwoColor, 118)) {
			return;
		}

		if (TryAppendHeading(builder, trimmedLine, "# ", HeadingOneColor, 132)) {
			return;
		}

		if (trimmedLine.StartsWith("> ")) {
			builder.AppendLine("<mark=" + QuoteBackgroundColor + "><color=" + QuoteAccentColor + ">▌ </color><color=" + QuoteTextColor + "><i>" + FormatInline(trimmedLine[2..].Trim()) + "</i></color></mark>");
			return;
		}

		if (trimmedLine == "---") {
			builder.AppendLine("<color=" + RuleColor + ">────────────────────────</color>");
			return;
		}

		if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ")) {
			AppendListItem(builder, originalLine, trimmedLine[2..].Trim(), "•");
			return;
		}

		Match orderedListMatch = OrderedListRegex.Match(trimmedLine);
		if (orderedListMatch.Success) {
			AppendListItem(builder, originalLine, orderedListMatch.Groups["content"].Value.Trim(), orderedListMatch.Groups["index"].Value + ".");
			return;
		}

		builder.AppendLine("<color=" + BodyTextColor + ">" + FormatInline(originalLine.TrimEnd()) + "</color>");
	}

	private static bool TryAppendHeading(StringBuilder builder, string trimmedLine, string prefix, string color, int sizePercent) {
		if (!trimmedLine.StartsWith(prefix)) {
			return false;
		}

		builder.AppendLine("<size=" + sizePercent + "%><color=" + color + "><b>" + FormatInline(trimmedLine[prefix.Length..].Trim()) + "</b></color></size>");
		return true;
	}

	private static void AppendListItem(StringBuilder builder, string originalLine, string content, string marker) {
		int indentationLevel = GetIndentationLevel(originalLine);
		builder.AppendLine("<color=" + BodyTextColor + ">" + GetIndentPrefix(indentationLevel) + marker + " " + FormatInline(content) + "</color>");
	}

	private static void AppendCodeBlock(StringBuilder builder, List<string> codeLines, List<string> codeBlocks, int copiedCodeBlockIndex) {
		if (builder == null || codeBlocks == null) {
			return;
		}

		string rawCode = string.Join("\n", codeLines).TrimEnd();
		int codeBlockIndex = codeBlocks.Count;
		bool isCopied = codeBlockIndex == copiedCodeBlockIndex;
		codeBlocks.Add(rawCode);
		int lineNumberWidth = GetLineNumberWidth(codeLines.Count);
		int codeWidth = GetCodeBlockWidth(codeLines, lineNumberWidth);

		builder.AppendLine(BuildCodeHeaderLine(codeBlockIndex, codeWidth, isCopied));
		builder.AppendLine(BuildCodeDividerLine(codeWidth));

		if (codeLines.Count == 0) {
			builder.AppendLine(BuildEmptyCodeLine(codeWidth));
		} else {
			for (int i = 0; i < codeLines.Count; i++) {
				builder.AppendLine(BuildCodeContentLine(codeLines[i], i + 1, lineNumberWidth, codeWidth));
			}
		}

		builder.AppendLine(BuildCodeDividerLine(codeWidth));
		builder.AppendLine();
	}

	private static string BuildCodeHeaderLine(int codeBlockIndex, int codeWidth, bool isCopied) {
		string title = "Kod Örneği";
		string actionText = isCopied ? "Kopyalandı" : "Kodu kopyala";
		int gapWidth = Math.Max(3, codeWidth - title.Length - actionText.Length);
		string actionMarkup = isCopied
			? "<color=" + CodeCopiedTextColor + "><b>" + actionText + "</b></color>"
			: "<link=\"copy:" + codeBlockIndex + "\"><color=" + CodeLinkTextColor + "><u>" + actionText + "</u></color></link>";

		return "<mark=" + CodeHeaderBackgroundColor + "><mspace=0.52em><color=" + CodeHeaderTextColor + "><b>" + title + "</b></color>" + new string(' ', gapWidth) + actionMarkup + "</mspace></mark>";
	}

	private static string BuildCodeDividerLine(int codeWidth) {
		return "<color=" + CodeBorderColor + ">" + new string('─', Math.Max(18, codeWidth + 2)) + "</color>";
	}

	private static string BuildCodeContentLine(string codeLine, int lineNumber, int lineNumberWidth, int codeWidth) {
		int visibleCodeWidth = Math.Max(1, codeWidth - lineNumberWidth - 3);
		string paddedLine = PadRight(codeLine, visibleCodeWidth);
		string paddedLineNumber = lineNumber.ToString().PadLeft(lineNumberWidth);
		return "<mark=" + CodeBackgroundColor + "><mspace=0.52em><color=" + CodeLineNumberColor + ">" + paddedLineNumber + " │ </color><color=" + CodeTextColor + ">" + EscapeRichText(paddedLine) + "</color></mspace></mark>";
	}

	private static string BuildEmptyCodeLine(int codeWidth) {
		string emptyText = PadRight("(boş kod bloğu)", Math.Max(16, codeWidth));
		return "<mark=" + CodeBackgroundColor + "><mspace=0.52em><color=" + MutedTextColor + ">" + EscapeRichText(emptyText) + "</color></mspace></mark>";
	}

	private static string FormatInline(string text) {
		string escaped = EscapeRichText(text);
		escaped = InlineCodeRegex.Replace(escaped, "<mark=" + InlineCodeBackgroundColor + "><color=" + InlineCodeTextColor + "><b> $1 </b></color></mark>");
		escaped = BoldRegex.Replace(escaped, "<b>$1</b>");
		escaped = ItalicRegex.Replace(escaped, "<i>$1</i>");
		return escaped;
	}

	private static int GetIndentationLevel(string line) {
		if (string.IsNullOrEmpty(line)) {
			return 0;
		}

		int whitespaceWidth = 0;
		for (int i = 0; i < line.Length; i++) {
			char character = line[i];
			if (character == ' ') {
				whitespaceWidth++;
				continue;
			}

			if (character == '\t') {
				whitespaceWidth += 4;
				continue;
			}

			break;
		}

		return whitespaceWidth / 2;
	}

	private static string GetIndentPrefix(int indentationLevel) {
		if (indentationLevel <= 0) {
			return string.Empty;
		}

		return new string('\u00A0', indentationLevel * 4);
	}

	private static int GetLineNumberWidth(int lineCount) {
		return Math.Max(2, lineCount.ToString().Length);
	}

	private static int GetCodeBlockWidth(List<string> codeLines, int lineNumberWidth) {
		int width = 24;
		for (int i = 0; i < codeLines.Count; i++) {
			string codeLine = codeLines[i] ?? string.Empty;
			width = Math.Max(width, codeLine.Length + lineNumberWidth + 3);
		}

		return Math.Max(width, "Kod Örneği   Kodu kopyala".Length + 2);
	}

	private static string PadRight(string value, int totalWidth) {
		string safeValue = value ?? string.Empty;
		if (safeValue.Length >= totalWidth) {
			return safeValue;
		}

		return safeValue + new string(' ', totalWidth - safeValue.Length);
	}

	private static string EscapeRichText(string text) {
		if (string.IsNullOrEmpty(text)) {
			return string.Empty;
		}

		string normalized = WebUtility.HtmlDecode(text);
		return normalized.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}
