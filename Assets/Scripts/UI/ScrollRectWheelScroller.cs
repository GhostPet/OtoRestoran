using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScrollRect için basit bir yardımcı. Amaç:
/// - Yalnızca dikey kaydırma
/// - Elastic (bounce) kaldırmak (Clamped)
/// - İstenirse inertia'yı kapatmak
/// - Mouse tekerleğini doğrudan dikey normalized pozisyona eşlemek
/// Bu component'i ScrollRect'in olduğu GameObject'e ekleyebilir veya targetScrollRect atayabilirsiniz.
/// </summary>
public class ScrollRectWheelScroller : MonoBehaviour {
	[SerializeField] private ScrollRect targetScrollRect;
	[SerializeField][Tooltip("Her tekerlek adımında viewport yüksekliğinin ne kadarı kadar kaydırılacağını belirler.")] private float wheelViewportPercent = 0.12f;
	[SerializeField][Tooltip("İçerik kısa olsa bile her tekerlek adımında uygulanacak minimum piksel miktarı.")] private float minimumWheelPixels = 48f;
	[SerializeField] private bool disableInertia = true;
	[SerializeField] private bool forceVerticalOnly = true;
	[SerializeField] private bool invertWheel = false;
	[SerializeField][Tooltip("False ise kullanıcı sadece aşağı doğru kaydırabilir; yukarı kaydırma engellenir.")] private bool allowScrollUp = true;
	[SerializeField][Tooltip("Built-in ScrollRect tekerlek kaydırmasını kapatır; çift kaydırmayı önler.")] private bool disableBuiltInWheel = true;
	[SerializeField][Tooltip("Fare viewport üzerindeyken kaydırma uygular.")] private bool requirePointerOverViewport = true;

	private RectTransform viewportRect;
	private RectTransform contentRect;
	private float lastViewportHeight = -1f;
	private float lastContentHeight = -1f;

	private void Awake() {
		Initialize();
	}

	private void OnEnable() {
		Initialize();
		RefreshLayout();
	}

	private void Update() {
		if (targetScrollRect == null || contentRect == null || viewportRect == null) {
			return;
		}

		RefreshLayoutIfNeeded();

		if (requirePointerOverViewport && !RectTransformUtility.RectangleContainsScreenPoint(viewportRect, Input.mousePosition, null)) {
			return;
		}

		Vector2 wheel = Input.mouseScrollDelta;
		if (Mathf.Approximately(wheel.y, 0f)) {
			return;
		}

		float maxScroll = GetMaxScrollPixels();
		if (maxScroll <= 0f) {
			return;
		}

		float wheelPixels = Mathf.Max(minimumWheelPixels, viewportRect.rect.height * wheelViewportPercent);
		float delta = -wheel.y * wheelPixels * (invertWheel ? -1f : 1f);

		// anchoredPosition.y küçülüyorsa kullanıcı yukarı kaydırıyordur
		if (!allowScrollUp && delta < 0f) {
			return;
		}

		Vector2 anchoredPosition = contentRect.anchoredPosition;
		anchoredPosition.y = Mathf.Clamp(anchoredPosition.y + delta, 0f, maxScroll);
		contentRect.anchoredPosition = anchoredPosition;

		if (disableInertia) {
			targetScrollRect.StopMovement();
		}
	}

	[ContextMenu("Refresh Layout")]
	public void RefreshLayout() {
		if (!ResolveReferences()) {
			return;
		}

		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
		Canvas.ForceUpdateCanvases();

		lastViewportHeight = viewportRect.rect.height;
		lastContentHeight = contentRect.rect.height;
		ClampContentPosition();
	}

	private void Initialize() {
		if (!ResolveReferences()) {
			Debug.LogWarning("ScrollRectWheelScroller: targetScrollRect bulunamadı. Component devre dışı bırakılıyor.");
			enabled = false;
			return;
		}

		if (forceVerticalOnly) {
			targetScrollRect.horizontal = false;
			targetScrollRect.vertical = true;
		}

		targetScrollRect.movementType = ScrollRect.MovementType.Clamped;

		if (disableInertia) {
			targetScrollRect.inertia = false;
		}

		if (disableBuiltInWheel) {
			targetScrollRect.scrollSensitivity = 0f;
		}
	}

	private bool ResolveReferences() {
		if (targetScrollRect == null) {
			targetScrollRect = GetComponent<ScrollRect>();
		}

		if (targetScrollRect == null) {
			return false;
		}

		contentRect = targetScrollRect.content;
		viewportRect = targetScrollRect.viewport != null
			? targetScrollRect.viewport
			: targetScrollRect.GetComponent<RectTransform>();

		return contentRect != null && viewportRect != null;
	}

	private void RefreshLayoutIfNeeded() {
		float viewportHeight = viewportRect.rect.height;
		float contentHeight = contentRect.rect.height;
		if (Mathf.Approximately(viewportHeight, lastViewportHeight) && Mathf.Approximately(contentHeight, lastContentHeight)) {
			return;
		}

		RefreshLayout();
	}

	private float GetMaxScrollPixels() {
		return Mathf.Max(0f, contentRect.rect.height - viewportRect.rect.height);
	}

	private void ClampContentPosition() {
		if (contentRect == null) {
			return;
		}

		Vector2 anchoredPosition = contentRect.anchoredPosition;
		anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0f, GetMaxScrollPixels());
		contentRect.anchoredPosition = anchoredPosition;
	}
}
