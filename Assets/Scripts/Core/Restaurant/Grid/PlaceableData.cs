using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableData", menuName = "Restaurant/PlaceableData", order = 100)]
public class PlaceableData : ScriptableObject {
	public string id;
	public GameObject prefab;
	public Vector2Int size = new(1, 1);
	// pivot offset inside the size (0..size-1). (0,0) means bottom-left corner aligns to grid cell.
	public Vector2Int pivot = Vector2Int.zero;
}
