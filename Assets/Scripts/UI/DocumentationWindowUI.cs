using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DocumentationTopicBinding {
	[SerializeField] private string topicId;
	[SerializeField] private string title;
	[SerializeField] private Button button;
	[SerializeField] private TMP_Text buttonLabel;
	[SerializeField] private TextAsset contentAsset;
	[SerializeField] [TextArea(5, 30)] private string fallbackContent;

	public string TopicId => topicId;
	public string Title => title;
	public Button Button => button;
	public TMP_Text ButtonLabel => buttonLabel;
	public TextAsset ContentAsset => contentAsset;
	public string FallbackContent => fallbackContent;
}

public class DocumentationWindowUI : WindowContentUI {
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private TMP_Text contentText;
	[SerializeField] private ScrollRect contentScrollRect;
	[SerializeField] private List<DocumentationTopicBinding> topics = new List<DocumentationTopicBinding>();
	[SerializeField] private string defaultTopicId;

	private string selectedTopicId;

	private void Awake() {
		RegisterTopicButtons();
	}

	protected override void OnWindowBound(GameWindowUI ownerWindow) {
		if (ownerWindow != null) {
			ownerWindow.SetTitle("Dokümantasyon");
		}

		SelectInitialTopic();
	}

	private void RegisterTopicButtons() {
		for (int i = 0; i < topics.Count; i++) {
			DocumentationTopicBinding topic = topics[i];
			if (topic == null || topic.Button == null) {
				continue;
			}

			if (topic.ButtonLabel != null) {
				topic.ButtonLabel.text = string.IsNullOrWhiteSpace(topic.Title) ? "Başlık" : topic.Title;
			}

			topic.Button.onClick.RemoveAllListeners();
			string topicId = topic.TopicId;
			topic.Button.onClick.AddListener(() => SelectTopic(topicId));
		}
	}

	private void SelectInitialTopic() {
		if (!string.IsNullOrWhiteSpace(selectedTopicId)) {
			SelectTopic(selectedTopicId);
			return;
		}

		if (!string.IsNullOrWhiteSpace(defaultTopicId)) {
			SelectTopic(defaultTopicId);
			return;
		}

		for (int i = 0; i < topics.Count; i++) {
			DocumentationTopicBinding topic = topics[i];
			if (topic == null || string.IsNullOrWhiteSpace(topic.TopicId)) {
				continue;
			}

			SelectTopic(topic.TopicId);
			return;
		}

		if (contentText != null) {
			contentText.text = "Dokümantasyon konusu atanmadı.";
		}
	}

	private void SelectTopic(string topicId) {
		DocumentationTopicBinding topic = FindTopic(topicId);
		if (topic == null) {
			return;
		}

		selectedTopicId = topic.TopicId;
		if (titleText != null) {
			titleText.text = string.IsNullOrWhiteSpace(topic.Title) ? "Dokümantasyon" : topic.Title;
		}

		if (contentText != null) {
			if (topic.ContentAsset != null) {
				contentText.text = topic.ContentAsset.text;
			}
			else {
				contentText.text = string.IsNullOrWhiteSpace(topic.FallbackContent)
					? "Bu başlık için içerik atanmadı."
					: topic.FallbackContent;
			}
		}

		if (contentScrollRect != null) {
			Canvas.ForceUpdateCanvases();
			contentScrollRect.verticalNormalizedPosition = 1f;
		}
	}

	private DocumentationTopicBinding FindTopic(string topicId) {
		for (int i = 0; i < topics.Count; i++) {
			DocumentationTopicBinding topic = topics[i];
			if (topic == null) {
				continue;
			}

			if (topic.TopicId == topicId) {
				return topic;
			}
		}

		return null;
	}
}
