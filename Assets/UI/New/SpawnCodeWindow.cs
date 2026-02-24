using UnityEngine;

public class SpawnCodeWindow : MonoBehaviour
{
	public GameObject codeWindowPrefab;
	public Transform canvasParent;

	public void SpawnWindow()
	{
		GameObject newWindow = Instantiate(codeWindowPrefab, canvasParent);

		// Ortaya spawn olsun ve sabit boyutta açılsın (600x800).
		RectTransform rect = newWindow.GetComponent<RectTransform>();
		// Ensure the rect uses center anchors/pivot so size and position are stable.
		rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		// Set size (width x height) and center it.
		rect.sizeDelta = new Vector2(600f, 800f);
		rect.anchoredPosition = Vector2.zero;

		// En üste gelsin
		newWindow.transform.SetAsLastSibling();
	}
}