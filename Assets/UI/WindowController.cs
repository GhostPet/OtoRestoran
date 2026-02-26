using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WindowController : MonoBehaviour
{
    [Header("References")]
    public RectTransform windowRect;
    public GameObject body;           // Kod alanının parent'ı
    public GameObject resizeHandle;   // Köşedeki resize objesi

    [Header("Minimize Size")]
    public float minimizedWidth = 250f;
    public float minimizedHeight = 60f;

    [Header("Animation")]
    public float animationDuration = 0.25f;

    private float originalWidth;
    private float originalHeight;

    private bool minimized = false;
    private Coroutine currentRoutine;

    private CanvasGroup bodyCanvas;

    void Awake()
    {
        bodyCanvas = body.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        originalWidth = windowRect.sizeDelta.x;
        originalHeight = windowRect.sizeDelta.y;
    }

    public void ToggleMinimize()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (!minimized)
            currentRoutine = StartCoroutine(Minimize());
        else
            currentRoutine = StartCoroutine(Restore());
    }

    IEnumerator Minimize()
    {
        minimized = true;

        originalWidth = windowRect.sizeDelta.x;
        originalHeight = windowRect.sizeDelta.y;

        float time = 0;

        float startWidth = windowRect.sizeDelta.x;
        float startHeight = windowRect.sizeDelta.y;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            float newWidth = Mathf.Lerp(startWidth, minimizedWidth, t);
            float newHeight = Mathf.Lerp(startHeight, minimizedHeight, t);

            windowRect.sizeDelta = new Vector2(newWidth, newHeight);

            yield return null;
        }

        windowRect.sizeDelta = new Vector2(minimizedWidth, minimizedHeight);

        // Body gizle (SetActive değil!)
        if (bodyCanvas != null)
        {
            bodyCanvas.alpha = 0;
            bodyCanvas.interactable = false;
            bodyCanvas.blocksRaycasts = false;
        }

        // Resize handle gizle
        if (resizeHandle != null)
            resizeHandle.SetActive(false);
    }

    IEnumerator Restore()
    {
        minimized = false;

        float time = 0;

        float startWidth = windowRect.sizeDelta.x;
        float startHeight = windowRect.sizeDelta.y;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            float newWidth = Mathf.Lerp(startWidth, originalWidth, t);
            float newHeight = Mathf.Lerp(startHeight, originalHeight, t);

            windowRect.sizeDelta = new Vector2(newWidth, newHeight);

            yield return null;
        }

        windowRect.sizeDelta = new Vector2(originalWidth, originalHeight);

        // Body geri getir
        if (bodyCanvas != null)
        {
            bodyCanvas.alpha = 1;
            bodyCanvas.interactable = true;
            bodyCanvas.blocksRaycasts = true;
        }

        // Resize handle geri getir
        if (resizeHandle != null)
            resizeHandle.SetActive(true);
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }
}