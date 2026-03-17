using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler {
	public RectTransform window;

	public float dragSpeed = 1.5f;

	private Vector2 startMousePosition;
	private Vector2 startWindowPosition;
	private Canvas canvas;
	private RectTransform canvasRect;

	void Awake() {
		canvas = GetComponentInParent<Canvas>();
		if (canvas != null)
			canvasRect = canvas.GetComponent<RectTransform>();
	}

	public void OnBeginDrag(PointerEventData eventData) {
		window.SetAsLastSibling();

		startMousePosition = eventData.position;
		startWindowPosition = window.anchoredPosition;
	}

	public void OnDrag(PointerEventData eventData) {
		Vector2 mouseDifference =
			(eventData.position - startMousePosition) / canvas.scaleFactor;

		Vector2 desired = startWindowPosition + mouseDifference * dragSpeed;

		// Clamp so window stays inside canvas
		if (canvasRect != null) {
			Vector2 windowSize = window.rect.size;

			Vector2 parentMin = canvasRect.rect.min;
			Vector2 parentMax = canvasRect.rect.max;

			Vector2 minAnchored = parentMin + Vector2.Scale(window.pivot, windowSize);
			Vector2 maxAnchored = parentMax - Vector2.Scale(Vector2.one - window.pivot, windowSize);

			float minX = Mathf.Min(minAnchored.x, maxAnchored.x);
			float maxX = Mathf.Max(minAnchored.x, maxAnchored.x);
			float minY = Mathf.Min(minAnchored.y, maxAnchored.y);
			float maxY = Mathf.Max(minAnchored.y, maxAnchored.y);

			desired.x = Mathf.Clamp(desired.x, minX, maxX);
			desired.y = Mathf.Clamp(desired.y, minY, maxY);
		}

		window.anchoredPosition = desired;
	}
}