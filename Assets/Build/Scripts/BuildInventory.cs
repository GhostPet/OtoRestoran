using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildItem
{
    public string itemName;
    public GameObject prefab;
    public int count;



}
/*
public class BuildInventory : MonoBehaviour
{
    public List<BuildItem> items = new List<BuildItem>();

    public Transform panelParent;
    public GameObject buttonPrefab;

    public void GenerateUI(BuildManager buildManager)
    {
        foreach (Transform child in panelParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            var itemLocal = item; // avoid closure capture issues
            // don't show items with zero count
            if (itemLocal.count <= 0)
                continue;

            GameObject btn = Instantiate(buttonPrefab, panelParent);

            btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text =
                itemLocal.itemName + " (" + itemLocal.count + ")";

            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                buildManager.SetCurrentPrefab(itemLocal.prefab);
            });
        }
    }

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
*/