using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GameWindowDragHandleUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler {
	[SerializeField] private GameWindowUI window;

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

	public void OnBeginDrag(PointerEventData eventData) {
		if (window != null) {
			window.BringToFront();
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (window == null) {
			return;
		}

		window.Move(eventData.delta / window.GetCanvasScaleFactor());
	}
}
