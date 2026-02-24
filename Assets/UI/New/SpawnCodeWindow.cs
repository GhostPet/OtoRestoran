using UnityEngine;

public class SpawnCodeWindow : MonoBehaviour
{
    public GameObject codeWindowPrefab;
    public Transform canvasParent;

    public void SpawnWindow()
    {
        GameObject newWindow = Instantiate(codeWindowPrefab, canvasParent);

        // Ortaya spawn olsun
        RectTransform rect = newWindow.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        // En üste gelsin
        newWindow.transform.SetAsLastSibling();
    }
}