using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class DocumentationContentLoader {
	private static readonly Regex BlockStartRegex = new(@"<!--\s*doc:(?<metadata>.*?)-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex BlockEndRegex = new(@"<!--\s*enddoc\s*-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex MetadataPairRegex = new("(?<key>[A-Za-z0-9_-]+)\\s*=\\s*(?:\"(?<quoted>[^\"]*)\"|(?<plain>[^;]+))", RegexOptions.Compiled);
	private static readonly Regex HeadingRegex = new(@"^\s*#\s+(?<title>.+?)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

	public static List<DocumentationTopicDefinition> LoadFromFolder(string folderPath) {
		var topics = new List<DocumentationTopicDefinition>();
		if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) {
			return topics;
		}

		string[] filePaths = Directory.GetFiles(folderPath, "*.md", SearchOption.AllDirectories);
		Array.Sort(filePaths, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < filePaths.Length; i++) {
			string filePath = filePaths[i];
			string markdown = File.ReadAllText(filePath, Encoding.UTF8);
			List<DocumentationTopicDefinition> fileTopics = ParseFile(filePath, markdown, i * 100);
			for (int topicIndex = 0; topicIndex < fileTopics.Count; topicIndex++) {
				topics.Add(fileTopics[topicIndex]);
			}
		}

		topics.Sort(CompareTopics);
		return topics;
	}

	private static List<DocumentationTopicDefinition> ParseFile(string filePath, string markdown, int fallbackOrderBase) {
		var topics = new List<DocumentationTopicDefinition>();
		if (string.IsNullOrWhiteSpace(markdown)) {
			return topics;
		}

		Match startMatch = BlockStartRegex.Match(markdown);
		if (!startMatch.Success) {
			topics.Add(CreateTopic(filePath, markdown, null, fallbackOrderBase));
			return topics;
		}

		int blockIndex = 0;
		int searchIndex = 0;
		while (true) {
			startMatch = BlockStartRegex.Match(markdown, searchIndex);
			if (!startMatch.Success) {
				break;
			}

			int contentStartIndex = startMatch.Index + startMatch.Length;
			Match endMatch = BlockEndRegex.Match(markdown, contentStartIndex);
			int contentEndIndex = endMatch.Success ? endMatch.Index : markdown.Length;
			string blockMarkdown = markdown[contentStartIndex..contentEndIndex].Trim();
			Dictionary<string, string> metadata = ParseMetadata(startMatch.Groups["metadata"].Value);
			topics.Add(CreateTopic(filePath, blockMarkdown, metadata, fallbackOrderBase + blockIndex));
			blockIndex++;

			if (!endMatch.Success) {
				break;
			}

			searchIndex = endMatch.Index + endMatch.Length;
		}

		return topics;
	}

	private static DocumentationTopicDefinition CreateTopic(string filePath, string markdown, Dictionary<string, string> metadata, int fallbackOrder) {
		string title = GetMetadata(metadata, "title");
		if (string.IsNullOrWhiteSpace(title)) {
			title = ExtractHeading(markdown);
		}
		if (string.IsNullOrWhiteSpace(title)) {
			title = HumanizeName(Path.GetFileNameWithoutExtension(filePath));
		}

		string topicId = GetMetadata(metadata, "id");
		if (string.IsNullOrWhiteSpace(topicId)) {
			topicId = Slugify(title);
		}
		if (string.IsNullOrWhiteSpace(topicId)) {
			topicId = Slugify(Path.GetFileNameWithoutExtension(filePath));
		}

		string parentTopicId = GetMetadata(metadata, "parent");
		if (string.IsNullOrWhiteSpace(parentTopicId)) {
			parentTopicId = GetMetadata(metadata, "parentId");
		}
		if (!string.IsNullOrWhiteSpace(parentTopicId)) {
			parentTopicId = Slugify(parentTopicId);
		}
		if (string.Equals(parentTopicId, topicId, StringComparison.OrdinalIgnoreCase)) {
			parentTopicId = string.Empty;
		}

		int sortOrder = fallbackOrder;
		string orderText = GetMetadata(metadata, "order");
		if (!string.IsNullOrWhiteSpace(orderText)) {
			if (int.TryParse(orderText, out int parsedOrder)) {
				sortOrder = parsedOrder;
			}
		}

		string relativeSource = filePath;
     return new DocumentationTopicDefinition(topicId, title, markdown.Trim(), sortOrder, relativeSource, parentTopicId);
	}

	private static Dictionary<string, string> ParseMetadata(string metadataText) {
		var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(metadataText)) {
			return metadata;
		}

		MatchCollection matches = MetadataPairRegex.Matches(metadataText);
		for (int i = 0; i < matches.Count; i++) {
			Match match = matches[i];
			string key = match.Groups["key"].Value.Trim();
			string value = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["plain"].Value;
			if (string.IsNullOrWhiteSpace(key)) {
				continue;
			}

			metadata[key] = value.Trim();
		}

		return metadata;
	}

	private static string GetMetadata(Dictionary<string, string> metadata, string key) {
		if (metadata == null || string.IsNullOrWhiteSpace(key)) {
			return string.Empty;
		}

		if (metadata.TryGetValue(key, out string value)) {
			return value;
		}

		return string.Empty;
	}

	private static string ExtractHeading(string markdown) {
		if (string.IsNullOrWhiteSpace(markdown)) {
			return string.Empty;
		}

		Match match = HeadingRegex.Match(markdown);
		return match.Success ? match.Groups["title"].Value.Trim() : string.Empty;
	}

	private static string HumanizeName(string value) {
		if (string.IsNullOrWhiteSpace(value)) {
			return "Dokümantasyon";
		}

		string normalized = value.Replace('-', ' ').Replace('_', ' ').Trim();
		return normalized.Length == 0 ? "Dokümantasyon" : normalized;
	}

	private static string Slugify(string value) {
		if (string.IsNullOrWhiteSpace(value)) {
			return string.Empty;
		}

		var builder = new StringBuilder();
		bool previousDash = false;
		for (int i = 0; i < value.Length; i++) {
			char character = char.ToLowerInvariant(value[i]);
			if (char.IsLetterOrDigit(character)) {
				builder.Append(character);
				previousDash = false;
				continue;
			}

			if (previousDash) {
				continue;
			}

			builder.Append('-');
			previousDash = true;
		}

		return builder.ToString().Trim('-');
	}

	private static int CompareTopics(DocumentationTopicDefinition left, DocumentationTopicDefinition right) {
		if (ReferenceEquals(left, right)) {
			return 0;
		}

		if (left == null) {
			return 1;
		}

		if (right == null) {
			return -1;
		}

		int orderCompare = left.SortOrder.CompareTo(right.SortOrder);
		if (orderCompare != 0) {
			return orderCompare;
		}

		return string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
	}
}
