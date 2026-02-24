using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform window;

    public float dragSpeed = 1.5f; // 1 normal, 1.5-2 daha hızlı

    private Vector2 startMousePosition;
    private Vector2 startWindowPosition;
    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        window.SetAsLastSibling();

        startMousePosition = eventData.position;
        startWindowPosition = window.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 mouseDifference =
            (eventData.position - startMousePosition) / canvas.scaleFactor;

        window.anchoredPosition =
            startWindowPosition + mouseDifference * dragSpeed;
    }
}