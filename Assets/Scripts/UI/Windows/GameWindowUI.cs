using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class GameWindowUI : MonoBehaviour, IPointerDownHandler {
	[SerializeField] private RectTransform windowRect;
	[SerializeField] private Vector2 minSize = new Vector2(320f, 220f);
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private Button closeButton;
	[SerializeField] private RectTransform contentRoot;

 private Canvas parentCanvas;
	private Component contentComponent;
	private string windowKey;

	public event Action<GameWindowUI> Closed;

	public RectTransform WindowRect => windowRect;
	public RectTransform ContentRoot => contentRoot;
	public string WindowKey => windowKey;

	private void Awake() {
		ResolveReferences();
		RegisterCloseButton();
	}

	public void Initialize(WindowAreaUI area, string title) {
       if (area != null) {
			parentCanvas = area.GetComponentInParent<Canvas>();
		}

		ResolveReferences();
		RegisterCloseButton();
		SetTitle(title);
	}

	public void SetWindowKey(string key) {
		windowKey = key;
	}

	public void SetTitle(string title) {
		if (titleText != null) {
			titleText.text = string.IsNullOrWhiteSpace(title) ? "Pencere" : title;
		}
	}

	public void SetContent(Component content) {
		contentComponent = content;
	}

	public T GetContent<T>() where T : Component {
		return contentComponent as T;
	}

	public void CloseWindow() {
		Closed?.Invoke(this);
		Destroy(gameObject);
	}

	public void BringToFront() {
		if (!gameObject.activeInHierarchy) {
			return;
		}

		transform.SetAsLastSibling();
	}

	public void Move(Vector2 delta) {
		ResolveReferences();
		if (windowRect == null) {
			return;
		}

		windowRect.anchoredPosition += delta;
	}

	public void Resize(Vector2 delta, bool resizeLeft, bool resizeRight, bool resizeBottom, bool resizeTop) {
		ResolveReferences();
		if (windowRect == null) {
			return;
		}

		Vector2 currentSize = windowRect.sizeDelta;
		Vector2 targetSize = currentSize;
		if (resizeLeft) {
			targetSize.x -= delta.x;
		}
		else if (resizeRight) {
			targetSize.x += delta.x;
		}

		if (resizeBottom) {
			targetSize.y -= delta.y;
		}
		else if (resizeTop) {
			targetSize.y += delta.y;
		}

		targetSize.x = Mathf.Max(minSize.x, targetSize.x);
		targetSize.y = Mathf.Max(minSize.y, targetSize.y);

		Vector2 appliedDelta = targetSize - currentSize;
		windowRect.sizeDelta = targetSize;

		Vector2 positionDelta = Vector2.zero;
		if (resizeLeft) {
			positionDelta.x -= appliedDelta.x * 0.5f;
		}
		else if (resizeRight) {
			positionDelta.x += appliedDelta.x * 0.5f;
		}

		if (resizeBottom) {
			positionDelta.y -= appliedDelta.y * 0.5f;
		}
		else if (resizeTop) {
			positionDelta.y += appliedDelta.y * 0.5f;
		}

		windowRect.anchoredPosition += positionDelta;
	}

	public void OnPointerDown(PointerEventData eventData) {
		BringToFront();
	}

	public float GetCanvasScaleFactor() {
		ResolveReferences();
		if (parentCanvas == null) {
			return 1f;
		}

		return Mathf.Approximately(parentCanvas.scaleFactor, 0f) ? 1f : parentCanvas.scaleFactor;
	}

	private void ResolveReferences() {
		if (windowRect == null) {
			windowRect = GetComponent<RectTransform>();
		}

		if (parentCanvas == null) {
			parentCanvas = GetComponentInParent<Canvas>();
		}
	}

	private void RegisterCloseButton() {
		if (closeButton == null) {
			return;
		}

		closeButton.onClick.RemoveListener(CloseWindow);
		closeButton.onClick.AddListener(CloseWindow);
	}
}
