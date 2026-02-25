using UnityEngine;
using UnityEngine.EventSystems;

public class WindowResize : MonoBehaviour, IBeginDragHandler, IDragHandler
{
	public RectTransform window;

	[Tooltip("If true, force the window RectTransform pivot/anchors to top-left so resizing from the bottom-right will expand only to the right and down. Disable if you manage anchors/pivot in the inspector.")]
	public bool forceTopLeftPivot = true;

	public float resizeSpeed = 2.5f;   // Hız (2-3 ideal)

	public float minWidth = 250f;      // Minimum genişlik
	public float minHeight = 150f;     // Minimum yükseklik

	private Vector2 lastMousePosition;

	public void OnBeginDrag(PointerEventData eventData)
	{
		lastMousePosition = eventData.position;
	}

	private void Start() {
		if (forceTopLeftPivot && window != null) {
			// Set anchors and pivot to top-left so sizeDelta changes expand to right/down only.
			window.pivot = new Vector2(0f, 1f);
			window.anchorMin = window.anchorMax = new Vector2(0f, 1f);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 currentMousePosition = eventData.position;
		Vector2 difference = (currentMousePosition - lastMousePosition) * resizeSpeed;

		Vector2 newSize = window.sizeDelta + new Vector2(difference.x, -difference.y);

		// Minimum boyut kontrolü
		if (newSize.x < minWidth)
			newSize.x = minWidth;

		if (newSize.y < minHeight)
			newSize.y = minHeight;

		window.sizeDelta = newSize;

		lastMousePosition = currentMousePosition;
	}
}