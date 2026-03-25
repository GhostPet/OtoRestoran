using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GameWindowResizeHandleUI : MonoBehaviour, IPointerDownHandler, IDragHandler {
	[SerializeField] private GameWindowUI window;
	[SerializeField] private bool resizeLeft;
	[SerializeField] private bool resizeRight = true;
	[SerializeField] private bool resizeBottom = true;
	[SerializeField] private bool resizeTop;

	private void Awake() {
       Image image = GetComponent<Image>();
		if (image != null) {
			image.color = new Color(1f, 1f, 1f, 0.001f);
			image.raycastTarget = true;
		}

		if (window == null) {
			window = GetComponentInParent<GameWindowUI>();
		}
	}

	public void OnPointerDown(PointerEventData eventData) {
		if (window != null) {
			window.BringToFront();
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (window == null) {
			return;
		}

		window.Resize(eventData.delta / window.GetCanvasScaleFactor(), resizeLeft, resizeRight, resizeBottom, resizeTop);
	}
}
