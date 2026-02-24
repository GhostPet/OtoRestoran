using UnityEngine;
using System.Collections;

public class WindowController : MonoBehaviour
{
    public RectTransform windowRect;
    public GameObject body;

    float originalHeight;
    bool minimized = false;

    void Start()
    {
        originalHeight = windowRect.sizeDelta.y;
    }

    public void ToggleMinimize()
    {
        if (!minimized)
            StartCoroutine(Minimize());
        else
            StartCoroutine(Restore());
    }

    IEnumerator Minimize()
    {
        minimized = true;

        float targetHeight = 150f; // sadece header kalacak
        float duration = 0.25f;
        float time = 0;

        float startHeight = windowRect.sizeDelta.y;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float newHeight = Mathf.Lerp(startHeight, targetHeight, t);
            windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, newHeight);

            yield return null;
        }

        body.SetActive(false);
    }

    IEnumerator Restore()
    {
        minimized = false;

        body.SetActive(true);

        float duration = 0.25f;
        float time = 0;

        float startHeight = windowRect.sizeDelta.y;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float newHeight = Mathf.Lerp(startHeight, originalHeight, t);
            windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, newHeight);

            yield return null;
        }
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }
}