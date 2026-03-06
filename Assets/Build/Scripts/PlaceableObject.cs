using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public GameObject originalPrefab;
    public PlaceableType type = PlaceableType.Generic;
    public int width = 1;
    public int height = 1;
    
    [HideInInspector]
    public UnityEngine.Vector2Int placedGridPosition;

    [HideInInspector]
    public int placedRotation;
}

public enum PlaceableType
{
    Generic,
    Table,
    Chair
}
