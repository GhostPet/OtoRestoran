using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid'e yerleştirilecek objeler için ayrı tutulan build envanteridir.
/// Bu sistem mutfak malzemelerinden bağımsızdır ve ileride PlacementController ile kolay bağlanacak şekilde yazılmıştır.
/// </summary>
public class BuildInventoryManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private ShopManager shopManager;

    [Header("Başlangıç Verisi")]
    [SerializeField] private List<BuildInventoryEntry> startingItems = new List<BuildInventoryEntry>();

    private readonly Dictionary<PlaceableData, int> placeableQuantities = new Dictionary<PlaceableData, int>();

    public event Action InventoryChanged;
    public event Action<PlaceableData, int> BuildItemQuantityChanged;

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

    public int GetQuantity(PlaceableData placeableData)
    {
        if (placeableData == null)
        {
            return 0;
        }

        int quantity;
        if (placeableQuantities.TryGetValue(placeableData, out quantity))
        {
            return quantity;
        }

        return 0;
    }

    public bool HasEnough(PlaceableData placeableData, int quantity)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetQuantity(placeableData) >= quantity;
    }

    public void AddPlaceable(PlaceableData placeableData, int quantity)
    {
        if (placeableData == null || quantity <= 0)
        {
            return;
        }

        int currentQuantity = GetQuantity(placeableData);
        int newQuantity = currentQuantity + quantity;
        placeableQuantities[placeableData] = newQuantity;

        RaiseInventoryEvents(placeableData, newQuantity);
    }

    public bool TryRemovePlaceable(PlaceableData placeableData, int quantity)
    {
        if (placeableData == null || quantity <= 0)
        {
            return false;
        }

        int currentQuantity = GetQuantity(placeableData);
        if (currentQuantity < quantity)
        {
            return false;
        }

        int newQuantity = currentQuantity - quantity;
        if (newQuantity <= 0)
        {
            placeableQuantities.Remove(placeableData);
            newQuantity = 0;
        }
        else
        {
            placeableQuantities[placeableData] = newQuantity;
        }

        RaiseInventoryEvents(placeableData, newQuantity);
        return true;
    }

    /// <summary>
    /// Grid tarafı ileride yerleştirme başarılı olduğunda bu metodu çağırarak envanterden 1 adet düşebilir.
    /// Böylece placement sistemi ile build inventory arasındaki bağ çok basit kalır.
    /// </summary>
    public bool TryConsumeForPlacement(PlaceableData placeableData)
    {
        return TryRemovePlaceable(placeableData, 1);
    }

    public List<BuildInventoryEntry> CreateSnapshot()
    {
        var snapshot = new List<BuildInventoryEntry>();

        foreach (KeyValuePair<PlaceableData, int> pair in placeableQuantities)
        {
            snapshot.Add(new BuildInventoryEntry(pair.Key, pair.Value));
        }

        return snapshot;
    }

    private void RebuildRuntimeLookup()
    {
        placeableQuantities.Clear();

        for (int i = 0; i < startingItems.Count; i++)
        {
            BuildInventoryEntry entry = startingItems[i];
            if (entry == null || entry.PlaceableData == null || entry.Quantity <= 0)
            {
                continue;
            }

            AddPlaceable(entry.PlaceableData, entry.Quantity);
        }
    }

    private void HandlePurchaseCompleted(ShopTransactionEventArgs args)
    {
        if (args == null || args.Product == null)
        {
            return;
        }

        if (args.Product.StorageType != ShopProductStorageType.BuildInventory)
        {
            return;
        }

        AddPlaceable(args.Product.BuildPlaceableData, args.Quantity);
    }

    private void HandleSaleCompleted(ShopTransactionEventArgs args)
    {
        if (args == null || args.Product == null)
        {
            return;
        }

        if (args.Product.StorageType != ShopProductStorageType.BuildInventory)
        {
            return;
        }

        TryRemovePlaceable(args.Product.BuildPlaceableData, args.Quantity);
    }

    private void RaiseInventoryEvents(PlaceableData placeableData, int quantity)
    {
        BuildItemQuantityChanged?.Invoke(placeableData, quantity);
        InventoryChanged?.Invoke();
    }
}
