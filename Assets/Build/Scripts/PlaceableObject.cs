using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public GameObject originalPrefab;
    public int width = 1;
    public int height = 1;
    
    [HideInInspector]
    public UnityEngine.Vector2Int placedGridPosition;

    [HideInInspector]
    public int placedRotation;
}
