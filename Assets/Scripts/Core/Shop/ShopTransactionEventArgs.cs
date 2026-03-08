using System;

/// <summary>
/// Başarılı shop işlemlerinde ilgili ürün ve işlem özeti bu sınıf ile yayınlanır.
/// Böylece inventory, placement veya UI sistemleri gereken aksiyonu alabilir.
/// </summary>
[Serializable]
public sealed class ShopTransactionEventArgs
{
    public ShopTransactionEventArgs(
        ShopProductDefinitionSO product,
        int quantity,
        int totalPrice,
        int balanceAfterTransaction,
        string tabId,
        ShopTransactionType transactionType)
    {
        Product = product;
        Quantity = quantity;
        TotalPrice = totalPrice;
        BalanceAfterTransaction = balanceAfterTransaction;
        TabId = tabId;
        TransactionType = transactionType;
    }

    public ShopProductDefinitionSO Product { get; }

    public int Quantity { get; }

    public int TotalPrice { get; }

    public int BalanceAfterTransaction { get; }

    public string TabId { get; }

    public ShopTransactionType TransactionType { get; }
}
