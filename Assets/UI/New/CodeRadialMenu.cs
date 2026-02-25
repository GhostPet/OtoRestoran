using UnityEngine;
using System.Collections;

public class CodeRadialMenu : MonoBehaviour
{
    public GameObject[] iconPrefabs;   // 4 farklı prefab
    public float radius = 220f;
    public float animationSpeed = 6f;

    bool isOpen = false;

    public void ToggleMenu()
    {
        if (!isOpen)
            StartCoroutine(OpenMenu());
        else
            CloseMenu();
    }

    IEnumerator OpenMenu()
    {
        isOpen = true;

        float angleStep = 180f / (iconPrefabs.Length - 1);

        for (int i = 0; i < iconPrefabs.Length; i++)
        {
            GameObject icon = Instantiate(iconPrefabs[i], transform);
            RectTransform rect = icon.GetComponent<RectTransform>();

            rect.localScale = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;

            float angle = angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 targetPos = new Vector2(
                -Mathf.Sin(rad) * radius,
                Mathf.Cos(rad) * radius
            );

            StartCoroutine(AnimateIcon(rect, targetPos));
        }

        yield return null;
    }

    void CloseMenu()
    {
        isOpen = false;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    IEnumerator AnimateIcon(RectTransform rect, Vector2 target)
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * animationSpeed;

            rect.anchoredPosition = Vector2.Lerp(Vector2.zero, target, t);
            rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);

            yield return null;
        }

        rect.anchoredPosition = target;
        rect.localScale = Vector3.one;
    }
}