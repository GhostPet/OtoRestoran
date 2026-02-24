using UnityEngine;
using UnityEngine.EventSystems;

public class WindowResize : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform window;

    public float resizeSpeed = 2.5f;   // Hız (2-3 ideal)

    public float minWidth = 250f;      // Minimum genişlik
    public float minHeight = 150f;     // Minimum yükseklik

    private Vector2 lastMousePosition;

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastMousePosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePosition = eventData.position;
        Vector2 difference = (currentMousePosition - lastMousePosition) * resizeSpeed;

        Vector2 newSize = window.sizeDelta + new Vector2(difference.x, -difference.y);

        // Minimum boyut kontrolü
        if (newSize.x < minWidth)
            newSize.x = minWidth;

        if (newSize.y < minHeight)
            newSize.y = minHeight;

        window.sizeDelta = newSize;

        lastMousePosition = currentMousePosition;
    }
}