using UnityEngine;
using UnityEngine.EventSystems;

public class RobotIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.3f;
    public float speed = 8f;

    bool isHovering = false;

    void Update()
    {
        float target = isHovering ? hoverScale : 1f;

        Vector3 targetScale = Vector3.one * target;

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}