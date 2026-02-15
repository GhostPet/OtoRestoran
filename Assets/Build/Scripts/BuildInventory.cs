using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildItem
{
    public string itemName;
    public GameObject prefab;
    public int count;
}

public class BuildInventory : MonoBehaviour
{
    public List<BuildItem> items = new List<BuildItem>();

    public bool HasItem(GameObject prefab)
    {
        var item = items.Find(i => i.prefab == prefab);
        return item != null && item.count > 0;
    }

    public void RemoveItem(GameObject prefab)
    {
        var item = items.Find(i => i.prefab == prefab);
        if (item != null && item.count > 0)
            item.count--;
    }

    public void AddItem(GameObject prefab)
    {
        var item = items.Find(i => i.prefab == prefab);
        if (item != null)
            item.count++;
    }
}
