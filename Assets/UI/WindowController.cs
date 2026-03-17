using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowController : MonoBehaviour {
	[Header("References")]
	public RectTransform windowRect;
	public GameObject body;           // Kod alanının parent'ı
	public GameObject taskNameInput;  // TaskNameInput nesnesi
	public GameObject saveButton;     // SaveButton nesnesi
	public GameObject handleLeft;      // Sol kenar
	public GameObject handleBottomLeft;// Sol alt köşe
	public GameObject handleBottom;    // Alt kenar
	public GameObject handleBottomRight;// Sağ alt köşe
	public GameObject handleRight;     // Sağ kenar

	[Header("Minimize Size")]
	public float minimizedWidth = 250f;
	public float minimizedHeight = 60f;

	[Header("Animation")]
	public float animationDuration = 0.25f;

	private float originalWidth;
	private float originalHeight;

	private bool minimized = false;
	private Coroutine currentRoutine;

	private CanvasGroup bodyCanvas;

	// Runtime resize state
	private RectTransform parentRect;
	private bool isResizing = false;
	private enum ResizeHandleType { Left, BottomLeft, Bottom, BottomRight, Right }
	private ResizeHandleType activeHandle;
	private Vector2 initialSize;
	private Vector2 initialAnchoredPos;
	private Vector2 fixedNormalized; // normalized coords (0..1) of the fixed point
	private Vector2 fixedLocal; // fixed point in parent local space

	void Awake() {
		bodyCanvas = body.GetComponent<CanvasGroup>();
	}

	void Start() {
		originalWidth = windowRect.sizeDelta.x;
		originalHeight = windowRect.sizeDelta.y;
	}

	public void ToggleMinimize() {
		if (currentRoutine != null)
			StopCoroutine(currentRoutine);

		if (!minimized)
			currentRoutine = StartCoroutine(Minimize());
		else
			currentRoutine = StartCoroutine(Restore());
	}

	IEnumerator Minimize() {
		minimized = true;

		originalWidth = windowRect.sizeDelta.x;
		originalHeight = windowRect.sizeDelta.y;

		float time = 0;

		float startWidth = windowRect.sizeDelta.x;
		float startHeight = windowRect.sizeDelta.y;

		// TaskNameInput ve SaveButton gizle
		if (taskNameInput != null) taskNameInput.SetActive(false);
		if (saveButton != null) saveButton.SetActive(false);

		while (time < animationDuration) {
			time += Time.deltaTime;
			float t = time / animationDuration;

			float newWidth = Mathf.Lerp(startWidth, minimizedWidth, t);
			float newHeight = Mathf.Lerp(startHeight, minimizedHeight, t);

			windowRect.sizeDelta = new Vector2(newWidth, newHeight);

			yield return null;
		}

		windowRect.sizeDelta = new Vector2(minimizedWidth, minimizedHeight);

		// Body gizle (SetActive değil!)
		if (bodyCanvas != null) {
			bodyCanvas.alpha = 0;
			bodyCanvas.interactable = false;
			bodyCanvas.blocksRaycasts = false;
		}

		// Resize handle gizle
		SetHandlesActive(false);
	}

	IEnumerator Restore() {
		minimized = false;

		float time = 0;

		float startWidth = windowRect.sizeDelta.x;
		float startHeight = windowRect.sizeDelta.y;

		while (time < animationDuration) {
			time += Time.deltaTime;
			float t = time / animationDuration;

			float newWidth = Mathf.Lerp(startWidth, originalWidth, t);
			float newHeight = Mathf.Lerp(startHeight, originalHeight, t);

			windowRect.sizeDelta = new Vector2(newWidth, newHeight);

			yield return null;
		}

		windowRect.sizeDelta = new Vector2(originalWidth, originalHeight);

		// Body geri getir
		if (bodyCanvas != null) {
			bodyCanvas.alpha = 1;
			bodyCanvas.interactable = true;
			bodyCanvas.blocksRaycasts = true;
		}

		// TaskNameInput ve SaveButton geri getir
		if (taskNameInput != null) taskNameInput.SetActive(true);
		if (saveButton != null) saveButton.SetActive(true);

		// Resize handle geri getir
		SetHandlesActive(true);
	}

	public void CloseWindow() {
		gameObject.SetActive(false);
	}

	void SetHandlesActive(bool active) {
		if (handleLeft != null) handleLeft.SetActive(active);
		if (handleBottomLeft != null) handleBottomLeft.SetActive(active);
		if (handleBottom != null) handleBottom.SetActive(active);
		if (handleBottomRight != null) handleBottomRight.SetActive(active);
		if (handleRight != null) handleRight.SetActive(active);
	}

	// These public methods can be wired to EventTrigger entries (PointerDown, Drag, PointerUp)
	public void OnBeginResizeLeft(BaseEventData data) { BeginResize(ResizeHandleType.Left, data as PointerEventData); }
	public void OnBeginResizeBottomLeft(BaseEventData data) { BeginResize(ResizeHandleType.BottomLeft, data as PointerEventData); }
	public void OnBeginResizeBottom(BaseEventData data) { BeginResize(ResizeHandleType.Bottom, data as PointerEventData); }
	public void OnBeginResizeBottomRight(BaseEventData data) { BeginResize(ResizeHandleType.BottomRight, data as PointerEventData); }
	public void OnBeginResizeRight(BaseEventData data) { BeginResize(ResizeHandleType.Right, data as PointerEventData); }

	public void OnDragResizeLeft(BaseEventData data) { DoResize(data as PointerEventData); }
	public void OnDragResizeBottomLeft(BaseEventData data) { DoResize(data as PointerEventData); }
	public void OnDragResizeBottom(BaseEventData data) { DoResize(data as PointerEventData); }
	public void OnDragResizeBottomRight(BaseEventData data) { DoResize(data as PointerEventData); }
	public void OnDragResizeRight(BaseEventData data) { DoResize(data as PointerEventData); }

	public void OnEndResize(BaseEventData _) { EndResize(); }

	private void BeginResize(ResizeHandleType handle, PointerEventData ped) {
		if (ped == null) return;

		parentRect = windowRect.parent as RectTransform;
		if (parentRect == null) return;

		isResizing = true;
		activeHandle = handle;

		initialSize = windowRect.sizeDelta;
		initialAnchoredPos = windowRect.anchoredPosition;

		// determine fixed normalized point depending on handle
		fixedNormalized = handle switch {
			ResizeHandleType.Left => new Vector2(1f, 0.5f),// right midpoint
			ResizeHandleType.BottomLeft => new Vector2(1f, 1f),// top-right
			ResizeHandleType.Bottom => new Vector2(0.5f, 1f),// top midpoint
			ResizeHandleType.BottomRight => new Vector2(0f, 1f),// top-left
			ResizeHandleType.Right => new Vector2(0f, 0.5f),// left midpoint
			_ => new Vector2(0f, 1f),
		};

		// compute fixed local position in parent space
		// fixedLocal = anchoredPosition + (fixedNormalized - pivot) * size
		fixedLocal = initialAnchoredPos + Vector2.Scale((fixedNormalized - windowRect.pivot), initialSize);
	}

	private void DoResize(PointerEventData ped) {
		if (!isResizing || ped == null || parentRect == null)
			return;

		// get pointer in parent local space
		if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, ped.position, ped.pressEventCamera, out Vector3 pointerWorld))
			return;

		Vector3 pointerLocal3 = parentRect.InverseTransformPoint(pointerWorld);
		Vector2 pointerLocal = new(pointerLocal3.x, pointerLocal3.y);

		Vector2 newSize = initialSize;

		if (activeHandle == ResizeHandleType.Left || activeHandle == ResizeHandleType.Right) {
			// horizontal only
			float newWidth = Mathf.Abs(pointerLocal.x - fixedLocal.x);
			newWidth = Mathf.Max(minimizedWidth, newWidth);
			newSize.x = newWidth;
			newSize.y = initialSize.y;
		} else if (activeHandle == ResizeHandleType.Bottom || activeHandle == ResizeHandleType.BottomLeft || activeHandle == ResizeHandleType.BottomRight) {
			// corner or bottom: both dimensions for corners, or vertical only for bottom
			if (activeHandle == ResizeHandleType.Bottom) {
				float newHeight = Mathf.Abs(pointerLocal.y - fixedLocal.y);
				newHeight = Mathf.Max(minimizedHeight, newHeight);
				newSize.y = newHeight;
				newSize.x = initialSize.x;
			} else {
				float newWidth = Mathf.Abs(pointerLocal.x - fixedLocal.x);
				float newHeight = Mathf.Abs(pointerLocal.y - fixedLocal.y);
				newWidth = Mathf.Max(minimizedWidth, newWidth);
				newHeight = Mathf.Max(minimizedHeight, newHeight);
				newSize = new Vector2(newWidth, newHeight);
			}
		}

		// compute anchoredPosition so that fixed point stays at fixedLocal
		Vector2 offsetFromPivot = Vector2.Scale((fixedNormalized - windowRect.pivot), newSize);
		Vector2 newAnchored = fixedLocal - offsetFromPivot;

		windowRect.sizeDelta = newSize;
		windowRect.anchoredPosition = newAnchored;
	}

	private void EndResize() {
		isResizing = false;
	}
}