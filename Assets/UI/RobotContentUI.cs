using UnityEngine;

public class RobotPanelUI : MonoBehaviour
{
    public RectTransform panel;
    public float openHeight = 500f;
    public float speed = 8f;

    private bool isOpen = false;
    private float currentHeight = 0f;

    void Update()
    {
        float targetHeight = isOpen ? openHeight : 0f;

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * speed);

        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;
    }
}