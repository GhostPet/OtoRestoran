using System;
using UnityEngine;

/// <summary>
/// Toplu alım ve toplu satış için adet bazlı fiyat kademesi tanımlar.
/// Örnek: 1+, 10+, 100+, 1000+ gibi.
/// </summary>
[Serializable]
public class ShopPriceTier
{
    [SerializeField] private string tierName;
    [SerializeField] private int minimumQuantity = 1;
    [SerializeField] private int buyUnitPrice = 0;
    [SerializeField] private int sellUnitPrice = -1;

    public string TierName => tierName;

    public int MinimumQuantity => minimumQuantity;

    public int BuyUnitPrice => buyUnitPrice;

    public int SellUnitPrice => sellUnitPrice;

    public void ClampValues()
    {
        if (minimumQuantity < 1)
        {
            minimumQuantity = 1;
        }

        if (buyUnitPrice < 0)
        {
            buyUnitPrice = 0;
        }

        if (sellUnitPrice < -1)
        {
            sellUnitPrice = -1;
        }
    }
}
