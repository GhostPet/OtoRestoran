using UnityEngine;
using System.Collections;

public class RobotProgramsPanelController : MonoBehaviour
{
    public RectTransform panel;
    public RectTransform dockPanel;

    public float panelOpenX = 0f;
    public float panelClosedX = 400f;

    public float dockOpenX = -400f;
    public float dockClosedX = 0f;

    public float speed = 8f;

    bool isOpen = false;

    public void TogglePanel()
    {
        StopAllCoroutines();

        if (isOpen)
        {
            StartCoroutine(Slide(panelClosedX, dockClosedX));
        }
        else
        {
            StartCoroutine(Slide(panelOpenX, dockOpenX));
        }

        isOpen = !isOpen;
    }

    IEnumerator Slide(float panelTarget, float dockTarget)
    {
        while (
            Mathf.Abs(panel.anchoredPosition.x - panelTarget) > 0.1f
            || Mathf.Abs(dockPanel.anchoredPosition.x - dockTarget) > 0.1f
        )
        {
            float panelX = Mathf.Lerp(
                panel.anchoredPosition.x,
                panelTarget,
                Time.deltaTime * speed
            );

            float dockX = Mathf.Lerp(
                dockPanel.anchoredPosition.x,
                dockTarget,
                Time.deltaTime * speed
            );

            panel.anchoredPosition = new Vector2(panelX, panel.anchoredPosition.y);
            dockPanel.anchoredPosition = new Vector2(dockX, dockPanel.anchoredPosition.y);

            yield return null;
        }

        panel.anchoredPosition = new Vector2(panelTarget, panel.anchoredPosition.y);
        dockPanel.anchoredPosition = new Vector2(dockTarget, dockPanel.anchoredPosition.y);
    }
}