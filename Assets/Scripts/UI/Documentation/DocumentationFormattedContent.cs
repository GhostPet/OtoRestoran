using System.Collections.Generic;

public sealed class DocumentationFormattedContent {
	public static readonly DocumentationFormattedContent Empty = new("Bu başlık için içerik atanmadı.", new List<string>());

	private readonly List<string> codeBlocks;

	public DocumentationFormattedContent(string displayText, List<string> codeBlocks) {
		DisplayText = string.IsNullOrWhiteSpace(displayText) ? "Bu başlık için içerik atanmadı." : displayText;
		this.codeBlocks = codeBlocks ?? new List<string>();
	}

	public string DisplayText { get; }

	public string GetCodeBlock(int index) {
		if (index < 0 || index >= codeBlocks.Count) {
			return string.Empty;
		}

		return codeBlocks[index];
	}
}