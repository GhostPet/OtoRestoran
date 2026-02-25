using UnityEngine;

public class RobotIconOpener : MonoBehaviour
{
    public GameObject windowPrefab;

    public void OpenRobotWindow()
    {
        // Sahnedeki Canvas'ı otomatik bulur
        Canvas canvas = FindObjectOfType<Canvas>();

        GameObject window = Instantiate(windowPrefab, canvas.transform);

        RectTransform rect = window.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }
}