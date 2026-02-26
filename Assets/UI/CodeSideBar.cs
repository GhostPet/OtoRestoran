using System.Collections;
using UnityEngine;

public class CodeSideBar : MonoBehaviour {
	public GameObject[] icons;
	public float radius = 300f;
	public float animationSpeed = 6f;

	bool isOpen = false;

	public void ToggleMenu() {
		if (!isOpen)
			StartCoroutine(OpenMenu());
		else
			CloseMenu();
	}

	IEnumerator OpenMenu() {
		isOpen = true;

		if (icons == null || icons.Length == 0) {
			yield break;
		}

		float angleStep = 180f / Mathf.Max(1, (icons.Length - 1));

		for (int i = 0; i < icons.Length; i++) {
			GameObject icon = icons[i];
			if (icon == null)
				continue;

			// Make sure the icon is visible and reset transform for animation
			icon.SetActive(true);
			RectTransform rect = icon.GetComponent<RectTransform>();
			if (rect == null)
				continue;

			rect.localScale = Vector3.zero;
			rect.anchoredPosition = Vector2.zero;

			float angle = angleStep * i;
			float rad = angle * Mathf.Deg2Rad;

			Vector2 targetPos = new Vector2(
				-Mathf.Sin(rad) * radius,
				Mathf.Cos(rad) * radius
			);

			// Bring to front and animate
			icon.transform.SetAsLastSibling();
			StartCoroutine(AnimateIcon(rect, targetPos));
		}

		yield return null;
	}

	void CloseMenu() {
		isOpen = false;

		// Stop any running animations started by this component
		StopAllCoroutines();

		if (icons == null)
			return;

		// Hide the icons instead of destroying them
		for (int i = 0; i < icons.Length; i++) {
			GameObject icon = icons[i];
			if (icon == null)
				continue;

			icon.SetActive(false);
		}
	}

	IEnumerator AnimateIcon(RectTransform rect, Vector2 target) {
		float t = 0;

		while (t < 1) {
			t += Time.deltaTime * animationSpeed;

			rect.anchoredPosition = Vector2.Lerp(Vector2.zero, target, t);
			rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);

			yield return null;
		}

		rect.anchoredPosition = target;
		rect.localScale = Vector3.one;
	}
}