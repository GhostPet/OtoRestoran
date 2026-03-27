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
	private RectTransform parentRect;
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

		windowRect.anchoredPosition = ClampPosition(windowRect.sizeDelta, windowRect.anchoredPosition + delta);
	}

	public void Resize(Vector2 delta, bool resizeLeft, bool resizeRight, bool resizeBottom, bool resizeTop) {
		ResolveReferences();
		if (windowRect == null || parentRect == null) {
			return;
		}

		Vector2 currentSize = windowRect.sizeDelta;
		Vector2 currentPosition = windowRect.anchoredPosition;
		Vector2 pivot = windowRect.pivot;
		Rect parentBounds = parentRect.rect;
		float parentLeft = -parentBounds.width * 0.5f;
		float parentRight = parentBounds.width * 0.5f;
		float parentBottom = -parentBounds.height * 0.5f;
		float parentTop = parentBounds.height * 0.5f;
		float minWidth = Mathf.Min(minSize.x, parentBounds.width);
		float minHeight = Mathf.Min(minSize.y, parentBounds.height);

		float left = currentPosition.x - (currentSize.x * pivot.x);
		float right = left + currentSize.x;
		float bottom = currentPosition.y - (currentSize.y * pivot.y);
		float top = bottom + currentSize.y;

		if (resizeLeft) {
			left += delta.x;
		} else if (resizeRight) {
			right += delta.x;
		}

		if (resizeBottom) {
			bottom += delta.y;
		} else if (resizeTop) {
			top += delta.y;
		}

		if (resizeLeft) {
			left = Mathf.Clamp(left, parentLeft, right - minWidth);
		} else if (resizeRight) {
			right = Mathf.Clamp(right, left + minWidth, parentRight);
		}

		if (resizeBottom) {
			bottom = Mathf.Clamp(bottom, parentBottom, top - minHeight);
		} else if (resizeTop) {
			top = Mathf.Clamp(top, bottom + minHeight, parentTop);
		}

		Vector2 targetSize = new Vector2(right - left, top - bottom);
		Vector2 targetPosition = new Vector2(
			left + (targetSize.x * pivot.x),
			bottom + (targetSize.y * pivot.y));

		windowRect.sizeDelta = targetSize;
		windowRect.anchoredPosition = targetPosition;
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

		if (parentRect == null && windowRect != null) {
			parentRect = windowRect.parent as RectTransform;
		}
	}

	private Vector2 GetMaxSize() {
		if (parentRect == null) {
			return new Vector2(float.MaxValue, float.MaxValue);
		}

		Rect parentBounds = parentRect.rect;
		return new Vector2(Mathf.Max(1f, parentBounds.width), Mathf.Max(1f, parentBounds.height));
	}

	private Vector2 ClampPosition(Vector2 size, Vector2 targetPosition) {
		if (parentRect == null || windowRect == null) {
			return targetPosition;
		}

		Rect parentBounds = parentRect.rect;
		Vector2 pivot = windowRect.pivot;

		float minX = (-parentBounds.width * 0.5f) + (size.x * pivot.x);
		float maxX = (parentBounds.width * 0.5f) - (size.x * (1f - pivot.x));
		float minY = (-parentBounds.height * 0.5f) + (size.y * pivot.y);
		float maxY = (parentBounds.height * 0.5f) - (size.y * (1f - pivot.y));

		if (minX > maxX) {
			targetPosition.x = 0f;
		} else {
			targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
		}

		if (minY > maxY) {
			targetPosition.y = 0f;
		} else {
			targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
		}

		return targetPosition;
	}

	private void RegisterCloseButton() {
		if (closeButton == null) {
			return;
		}

		closeButton.onClick.RemoveListener(CloseWindow);
		closeButton.onClick.AddListener(CloseWindow);
	}
}
