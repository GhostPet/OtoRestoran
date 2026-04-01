public class DocumentationTopicDefinition {
	public DocumentationTopicDefinition(string topicId, string title, string rawMarkdown, int sortOrder, string sourcePath, string parentTopicId) {
		TopicId = topicId;
		Title = title;
		RawMarkdown = rawMarkdown;
		SortOrder = sortOrder;
		SourcePath = sourcePath;
       ParentTopicId = parentTopicId;
	}

	public string TopicId { get; }
	public string Title { get; }
	public string RawMarkdown { get; }
	public int SortOrder { get; }
	public string SourcePath { get; }
 public string ParentTopicId { get; }
}
