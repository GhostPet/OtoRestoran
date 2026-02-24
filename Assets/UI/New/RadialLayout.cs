using UnityEngine;

public class RadialLayout : MonoBehaviour
{
    public float radius = 150f;
    public float startAngle = -90f;
    public float endAngle = 90f;

    void Start()
    {
        Arrange();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Arrange();
    }
#endif

    public void Arrange()
    {
        int count = transform.childCount;
        if (count == 0) return;

        float totalAngle = endAngle - startAngle;
        float angleStep = count > 1 ? totalAngle / (count - 1) : 0;

        for (int i = 0; i < count; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;

            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            child.anchoredPosition = new Vector2(x, y);
        }
    }
}