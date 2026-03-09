using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyun sırasında tüketilecek malzemeleri yöneten ana envanterdir.
/// Bu sistem build modundan tamamen bağımsızdır.
/// Örnek içerikler: ekmek, köfte, sos, içecek malzemeleri.
/// </summary>
public class InvenyoryManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private ShopManager shopManager;

    [Header("Başlangıç Verisi")]
    [SerializeField] private List<InventoryItemEntry> startingItems = new List<InventoryItemEntry>();

    private readonly Dictionary<ItemSO, int> itemQuantities = new Dictionary<ItemSO, int>();

    public event Action InventoryChanged;
    public event Action<ItemSO, int> ItemQuantityChanged;

    private void Awake()
    {
        RebuildRuntimeLookup();
    }

    private void OnEnable()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }

        if (shopManager != null)
        {
            shopManager.PurchaseCompleted += HandlePurchaseCompleted;
            shopManager.SaleCompleted += HandleSaleCompleted;
        }
    }

    private void OnDisable()
    {
        if (shopManager != null)
        {
            shopManager.PurchaseCompleted -= HandlePurchaseCompleted;
            shopManager.SaleCompleted -= HandleSaleCompleted;
        }
    }

    public int GetQuantity(ItemSO item)
    {
        if (item == null)
        {
            return 0;
        }

        int quantity;
        if (itemQuantities.TryGetValue(item, out quantity))
        {
            return quantity;
        }

        return 0;
    }

    public bool HasEnough(ItemSO item, int quantity)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetQuantity(item) >= quantity;
    }

    public void AddItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            return;
        }

        int currentQuantity = GetQuantity(item);
        int newQuantity = currentQuantity + quantity;
        itemQuantities[item] = newQuantity;

        RaiseInventoryEvents(item, newQuantity);
    }

    public bool TryRemoveItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            return false;
        }

        int currentQuantity = GetQuantity(item);
        if (currentQuantity < quantity)
        {
            return false;
        }

        int newQuantity = currentQuantity - quantity;
        if (newQuantity <= 0)
        {
            itemQuantities.Remove(item);
            newQuantity = 0;
        }
        else
        {
            itemQuantities[item] = newQuantity;
        }

        RaiseInventoryEvents(item, newQuantity);
        return true;
    }

    public List<InventoryItemEntry> CreateSnapshot()
    {
        var snapshot = new List<InventoryItemEntry>();

        foreach (KeyValuePair<ItemSO, int> pair in itemQuantities)
        {
            snapshot.Add(new InventoryItemEntry(pair.Key, pair.Value));
        }

        return snapshot;
    }

    private void RebuildRuntimeLookup()
    {
        itemQuantities.Clear();

        for (int i = 0; i < startingItems.Count; i++)
        {
            InventoryItemEntry entry = startingItems[i];
            if (entry == null || entry.Item == null || entry.Quantity <= 0)
            {
                continue;
            }

            AddItem(entry.Item, entry.Quantity);
        }
    }

    private void HandlePurchaseCompleted(ShopTransactionEventArgs args)
    {
        if (args == null || args.Product == null)
        {
            return;
        }

        if (args.Product.StorageType != ShopProductStorageType.ConsumableInventory)
        {
            return;
        }

        AddItem(args.Product.ConsumableItem, args.Quantity);
    }

    private void HandleSaleCompleted(ShopTransactionEventArgs args)
    {
        if (args == null || args.Product == null)
        {
            return;
        }

        if (args.Product.StorageType != ShopProductStorageType.ConsumableInventory)
        {
            return;
        }

        TryRemoveItem(args.Product.ConsumableItem, args.Quantity);
    }

    private void RaiseInventoryEvents(ItemSO item, int quantity)
    {
        ItemQuantityChanged?.Invoke(item, quantity);
        InventoryChanged?.Invoke();
    }
}
