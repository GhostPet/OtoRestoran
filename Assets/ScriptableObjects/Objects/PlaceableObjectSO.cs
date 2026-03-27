using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlaceableObject", menuName = "OtoRestoran/Storage/Placeable Object")]
public class PlaceableObjectSO : ScriptableObject {
	[FormerlySerializedAs("id")]
	[SerializeField] private string itemName;
	[SerializeField] private bool showInStorage = true;
	public GameObject prefab;
	public Vector2Int size = new(1, 1);
	public Vector2Int pivot = Vector2Int.zero;
	[Min(0)] public int maxOwnedCount;

	public string Name => string.IsNullOrWhiteSpace(itemName) ? name : itemName;
	public bool ShowInStorage => showInStorage;
	public bool HasOwnershipLimit => maxOwnedCount > 0;

	private void OnValidate() {
		size.x = Mathf.Max(1, size.x);
		size.y = Mathf.Max(1, size.y);
		pivot.x = Mathf.Clamp(pivot.x, 0, size.x - 1);
		pivot.y = Mathf.Clamp(pivot.y, 0, size.y - 1);
		maxOwnedCount = Mathf.Max(0, maxOwnedCount);
	}
}
