using UnityEngine;
using UnityEngine.EventSystems;

public class SideBarIcon : MonoBehaviour,
	IPointerEnterHandler,
	IPointerExitHandler {
	public float hoverMultiplier = 1.3f;
	public float speed = 12f;

	private Vector2 originalSize;
	private Vector2 targetSize;
	private RectTransform rect;

	void Awake() {
		rect = GetComponent<RectTransform>();
		originalSize = rect.sizeDelta;   // Gerçek başlangıç boyutu
		targetSize = originalSize;
	}

	void Update() {
		rect.sizeDelta = Vector2.Lerp(
			rect.sizeDelta,
			targetSize,
			Time.deltaTime * speed);
	}

	public void OnPointerEnter(PointerEventData eventData) {
		targetSize = originalSize * hoverMultiplier;
	}

	public void OnPointerExit(PointerEventData eventData) {
		targetSize = originalSize;
	}
}