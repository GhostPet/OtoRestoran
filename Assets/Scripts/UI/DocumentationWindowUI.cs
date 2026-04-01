using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentationWindowUI : WindowContentUI {
	[SerializeField] private TMP_Text contentText;
	[SerializeField] private ScrollRect contentScrollRect;
	[SerializeField] private Transform topicButtonContainer;
	[SerializeField] private DocumentationTopicButtonUI topicButtonPrefab;
	[SerializeField] private string docsFolderPath = "Docs";
	[SerializeField] private string defaultTopicId;

	private readonly List<DocumentationTopicDefinition> resolvedTopics = new();
	private readonly Dictionary<string, DocumentationTopicDefinition> topicLookup = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, DocumentationFormattedContent> formattedContentCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> topicIndentationLookup = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<DocumentationTopicButtonUI> spawnedTopicButtons = new();
	private string selectedTopicId;
	private GameWindowUI boundWindow;
	private DocumentationContentLinkHandler contentLinkHandler;
	private int copiedCodeBlockIndex = -1;

	private void Awake() {
		EnsureContentLinkHandler();
		ReloadTopics();
	}

	protected override void OnWindowBound(GameWindowUI ownerWindow) {
		boundWindow = ownerWindow;
		EnsureContentLinkHandler();
		UpdateWindowTitle(null);

		ReloadTopics();
		SelectInitialTopic();
	}

	private void OnDestroy() {
		ClearDynamicButtons();
	}

	private void ReloadTopics() {
		LoadTopics();
		BuildDynamicTopicButtons();
	}

	private void LoadTopics() {
		resolvedTopics.Clear();
		topicLookup.Clear();
		formattedContentCache.Clear();
		topicIndentationLookup.Clear();

		string folderPath = ResolveDocumentationFolderPath();
		if (!string.IsNullOrWhiteSpace(folderPath)) {
			List<DocumentationTopicDefinition> loadedTopics = DocumentationContentLoader.LoadFromFolder(folderPath);
			for (int i = 0; i < loadedTopics.Count; i++) {
				AddTopic(loadedTopics[i]);
			}
		}

		RebuildResolvedTopics();
	}

	private string ResolveDocumentationFolderPath() {
		if (string.IsNullOrWhiteSpace(docsFolderPath)) {
			return string.Empty;
		}

		string normalizedPath = docsFolderPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
		return Path.Combine(Application.streamingAssetsPath, normalizedPath);
	}

	private void AddTopic(DocumentationTopicDefinition topic) {
		if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId) || topicLookup.ContainsKey(topic.TopicId)) {
			return;
		}

		resolvedTopics.Add(topic);
		topicLookup.Add(topic.TopicId, topic);
	}

	private void RebuildResolvedTopics() {
		if (resolvedTopics.Count == 0) {
			return;
		}

		var sortedTopics = new List<DocumentationTopicDefinition>(resolvedTopics);
		sortedTopics.Sort(CompareTopics);

		var rootTopics = new List<DocumentationTopicDefinition>();
		var childTopicsByParent = new Dictionary<string, List<DocumentationTopicDefinition>>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < sortedTopics.Count; i++) {
			DocumentationTopicDefinition topic = sortedTopics[i];
			string parentTopicId = GetValidParentTopicId(topic);
			if (string.IsNullOrWhiteSpace(parentTopicId)) {
				rootTopics.Add(topic);
				continue;
			}

			if (!childTopicsByParent.TryGetValue(parentTopicId, out List<DocumentationTopicDefinition> childTopics)) {
				childTopics = new List<DocumentationTopicDefinition>();
				childTopicsByParent[parentTopicId] = childTopics;
			}

			childTopics.Add(topic);
		}

		for (int i = 0; i < rootTopics.Count; i++) {
			if (childTopicsByParent.TryGetValue(rootTopics[i].TopicId, out List<DocumentationTopicDefinition> childTopics)) {
				childTopics.Sort(CompareTopics);
			}
		}

		var orderedTopics = new List<DocumentationTopicDefinition>(sortedTopics.Count);
		var visitedTopicIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < rootTopics.Count; i++) {
			AppendTopicHierarchy(rootTopics[i], 0, childTopicsByParent, orderedTopics, visitedTopicIds);
		}

		for (int i = 0; i < sortedTopics.Count; i++) {
			DocumentationTopicDefinition topic = sortedTopics[i];
			if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId) || visitedTopicIds.Contains(topic.TopicId)) {
				continue;
			}

			AppendTopicHierarchy(topic, 0, childTopicsByParent, orderedTopics, visitedTopicIds);
		}

		resolvedTopics.Clear();
		resolvedTopics.AddRange(orderedTopics);
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

	private void BuildDynamicTopicButtons() {
		ClearDynamicButtons();
		if (topicButtonContainer == null || topicButtonPrefab == null) {
			return;
		}

		for (int i = 0; i < resolvedTopics.Count; i++) {
			DocumentationTopicDefinition topic = resolvedTopics[i];
			if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId)) {
				continue;
			}

			DocumentationTopicButtonUI button = Instantiate(topicButtonPrefab, topicButtonContainer);
			string topicId = topic.TopicId;
			button.Bind(topic.Title, () => SelectTopic(topicId), topicId == selectedTopicId, GetTopicIndentation(topicId));
			spawnedTopicButtons.Add(button);
		}
	}

	private void ClearDynamicButtons() {
		for (int i = 0; i < spawnedTopicButtons.Count; i++) {
			DocumentationTopicButtonUI button = spawnedTopicButtons[i];
			if (button != null) {
				Destroy(button.gameObject);
			}
		}

		spawnedTopicButtons.Clear();
	}

	private void SelectInitialTopic() {
		if (!string.IsNullOrWhiteSpace(selectedTopicId) && FindTopic(selectedTopicId) != null) {
			SelectTopic(selectedTopicId);
			return;
		}

		if (!string.IsNullOrWhiteSpace(defaultTopicId) && FindTopic(defaultTopicId) != null) {
			SelectTopic(defaultTopicId);
			return;
		}

		for (int i = 0; i < resolvedTopics.Count; i++) {
			DocumentationTopicDefinition topic = resolvedTopics[i];
			if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId)) {
				continue;
			}

			SelectTopic(topic.TopicId);
			return;
		}

		selectedTopicId = string.Empty;
		UpdateWindowTitle(null);

		if (contentText != null) {
			contentText.text = "`StreamingAssets/Docs` altında okunabilir bir markdown dokümanı bulunamadı.";
		}
	}

	private void SelectTopic(string topicId) {
		DocumentationTopicDefinition topic = FindTopic(topicId);
		if (topic == null) {
			return;
		}

		selectedTopicId = topic.TopicId;
		copiedCodeBlockIndex = -1;
		UpdateWindowTitle(topic);
		SetDisplayedContent(topic, copiedCodeBlockIndex, false);

		RefreshDynamicButtonSelection();

		if (contentScrollRect != null) {
			Canvas.ForceUpdateCanvases();
			contentScrollRect.verticalNormalizedPosition = 1f;
		}
	}

	private DocumentationFormattedContent GetFormattedContent(DocumentationTopicDefinition topic) {
		if (topic == null) {
			return DocumentationFormattedContent.Empty;
		}

		if (formattedContentCache.TryGetValue(topic.TopicId, out DocumentationFormattedContent cached)) {
			return cached;
		}

		DocumentationFormattedContent formatted = DocumentationMarkdownFormatter.Format(topic.RawMarkdown);
		formattedContentCache[topic.TopicId] = formatted;
		return formatted;
	}

	public void HandleContentLink(string linkId) {
		if (string.IsNullOrWhiteSpace(linkId)) {
			return;
		}

		if (!linkId.StartsWith("copy:", StringComparison.OrdinalIgnoreCase)) {
			return;
		}

		DocumentationTopicDefinition topic = FindTopic(selectedTopicId);
		DocumentationFormattedContent formattedContent = GetFormattedContent(topic);
		if (formattedContent == null) {
			return;
		}

		string indexText = linkId["copy:".Length..];
		if (!int.TryParse(indexText, out int codeBlockIndex)) {
			return;
		}

		string codeBlock = formattedContent.GetCodeBlock(codeBlockIndex);
		if (string.IsNullOrEmpty(codeBlock)) {
			return;
		}

		GUIUtility.systemCopyBuffer = codeBlock;
		copiedCodeBlockIndex = codeBlockIndex;
		SetDisplayedContent(topic, copiedCodeBlockIndex, true);
	}

	private void RefreshDynamicButtonSelection() {
		for (int i = 0; i < spawnedTopicButtons.Count; i++) {
			DocumentationTopicButtonUI button = spawnedTopicButtons[i];
			if (button == null) {
				continue;
			}

			DocumentationTopicDefinition topic = i < resolvedTopics.Count ? resolvedTopics[i] : null;
			button.SetSelected(topic != null && string.Equals(topic.TopicId, selectedTopicId, StringComparison.OrdinalIgnoreCase));
		}
	}

	private DocumentationTopicDefinition FindTopic(string topicId) {
		if (string.IsNullOrWhiteSpace(topicId)) {
			return null;
		}

		if (topicLookup.TryGetValue(topicId, out DocumentationTopicDefinition topic)) {
			return topic;
		}

		return null;
	}

	private void AppendTopicHierarchy(DocumentationTopicDefinition topic, int indentationLevel, Dictionary<string, List<DocumentationTopicDefinition>> childTopicsByParent, List<DocumentationTopicDefinition> orderedTopics, HashSet<string> visitedTopicIds) {
		if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId) || !visitedTopicIds.Add(topic.TopicId)) {
			return;
		}

		orderedTopics.Add(topic);
		topicIndentationLookup[topic.TopicId] = indentationLevel;

		if (!childTopicsByParent.TryGetValue(topic.TopicId, out List<DocumentationTopicDefinition> childTopics)) {
			return;
		}

		for (int i = 0; i < childTopics.Count; i++) {
			AppendTopicHierarchy(childTopics[i], indentationLevel + 1, childTopicsByParent, orderedTopics, visitedTopicIds);
		}
	}

	private int GetTopicIndentation(string topicId) {
		if (string.IsNullOrWhiteSpace(topicId) || !topicIndentationLookup.TryGetValue(topicId, out int indentation)) {
			return 0;
		}

		return Mathf.Max(0, indentation);
	}

	private string GetValidParentTopicId(DocumentationTopicDefinition topic) {
		if (topic == null || string.IsNullOrWhiteSpace(topic.ParentTopicId)) {
			return string.Empty;
		}

		DocumentationTopicDefinition parentTopic = FindTopic(topic.ParentTopicId);
		if (parentTopic == null || string.Equals(parentTopic.TopicId, topic.TopicId, StringComparison.OrdinalIgnoreCase)) {
			return string.Empty;
		}

		return parentTopic.TopicId;
	}

	private void UpdateWindowTitle(DocumentationTopicDefinition topic) {
		if (boundWindow == null) {
			return;
		}

		boundWindow.SetTitle(topic == null || string.IsNullOrWhiteSpace(topic.Title) ? "Dokümantasyon" : topic.Title);
	}

	private void SetDisplayedContent(DocumentationTopicDefinition topic, int copiedBlockIndex, bool preserveScrollPosition) {
		if (contentText == null) {
			return;
		}

		float scrollPosition = 1f;
		if (preserveScrollPosition && contentScrollRect != null) {
			scrollPosition = contentScrollRect.verticalNormalizedPosition;
		}

		DocumentationFormattedContent formattedContent = copiedBlockIndex >= 0
			? DocumentationMarkdownFormatter.Format(topic.RawMarkdown, copiedBlockIndex)
			: GetFormattedContent(topic);

		contentText.text = formattedContent.DisplayText;

		if (preserveScrollPosition && contentScrollRect != null) {
			Canvas.ForceUpdateCanvases();
			contentScrollRect.verticalNormalizedPosition = scrollPosition;
		}
	}

	private void EnsureContentLinkHandler() {
		if (contentText == null) {
			return;
		}

		if (contentLinkHandler == null) {
			contentLinkHandler = contentText.GetComponent<DocumentationContentLinkHandler>();
		}

		if (contentLinkHandler == null) {
			contentLinkHandler = contentText.gameObject.AddComponent<DocumentationContentLinkHandler>();
		}

		contentLinkHandler.Initialize(contentText, this);
	}
}
