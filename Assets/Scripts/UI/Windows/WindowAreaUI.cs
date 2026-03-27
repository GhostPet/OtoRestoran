using System.Collections.Generic;
using UnityEngine;

public class WindowAreaUI : MonoBehaviour {
	[SerializeField] private RectTransform windowRoot;
	[SerializeField] private GameWindowUI windowFramePrefab;

	private readonly Dictionary<string, GameWindowUI> singletonWindows = new Dictionary<string, GameWindowUI>();

	private void Awake() {
		if (windowRoot == null) {
			windowRoot = GetComponent<RectTransform>();
		}
	}

	public T OpenSingletonWindow<T>(string windowKey, string title, T contentPrefab) where T : WindowContentUI {
		if (string.IsNullOrWhiteSpace(windowKey) || contentPrefab == null) {
			return null;
		}

		GameWindowUI frame;
		if (singletonWindows.TryGetValue(windowKey, out frame)) {
			if (frame == null) {
				singletonWindows.Remove(windowKey);
			} else {
				frame.SetTitle(title);
				frame.BringToFront();
				T existingContent = frame.GetContent<T>();
				return existingContent;
			}
		}

		T content = CreateWindow(title, contentPrefab, out frame);
		if (frame != null) {
			frame.SetWindowKey(windowKey);
			frame.Closed -= HandleSingletonWindowClosed;
			frame.Closed += HandleSingletonWindowClosed;
			singletonWindows[windowKey] = frame;
		}

		return content;
	}

	public T OpenTransientWindow<T>(string title, T contentPrefab) where T : WindowContentUI {
		GameWindowUI frame;
		return CreateWindow(title, contentPrefab, out frame);
	}

	private T CreateWindow<T>(string title, T contentPrefab, out GameWindowUI frame) where T : WindowContentUI {
		frame = null;
		if (windowFramePrefab == null || contentPrefab == null) {
			return null;
		}

		Transform parent = windowRoot != null ? windowRoot : transform;
		frame = Instantiate(windowFramePrefab, parent);
		frame.Initialize(this, title);

		RectTransform contentRoot = frame.ContentRoot;
		if (contentRoot == null) {
			Destroy(frame.gameObject);
			frame = null;
			return null;
		}

		T content = Instantiate(contentPrefab, contentRoot);
		RectTransform contentRect = content.GetComponent<RectTransform>();
		if (contentRect != null) {
			contentRect.anchorMin = Vector2.zero;
			contentRect.anchorMax = Vector2.one;
			contentRect.offsetMin = Vector2.zero;
			contentRect.offsetMax = Vector2.zero;
		}

		content.BindWindow(frame);
		frame.SetContent(content);
		frame.BringToFront();
		return content;
	}

	private void HandleSingletonWindowClosed(GameWindowUI closedWindow) {
		if (closedWindow == null) {
			return;
		}

		string key = closedWindow.WindowKey;
		if (!string.IsNullOrWhiteSpace(key) && singletonWindows.ContainsKey(key) && singletonWindows[key] == closedWindow) {
			singletonWindows.Remove(key);
		}
	}
}
