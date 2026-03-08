using System;

/// <summary>
/// Shop işlemlerinin sonucunu tek tip bir veri modeli ile dışarıya verir.
/// Hem satın alma hem satış aynı yapı üzerinden raporlanır.
/// </summary>
[Serializable]
public readonly struct ShopOperationResult
{
    public ShopOperationResult(bool success, ShopTransactionType transactionType, int totalAmount, string message)
    {
        Success = success;
        TransactionType = transactionType;
        TotalAmount = totalAmount;
        Message = message;
    }

    public bool Success { get; }

    public ShopTransactionType TransactionType { get; }

    /// <summary>
    /// Satın almada ödenen, satışta kazanılan toplam tutardır.
    /// </summary>
    public int TotalAmount { get; }

    public string Message { get; }

    public static ShopOperationResult CreateSuccess(ShopTransactionType transactionType, int totalAmount, string message)
    {
        return new ShopOperationResult(true, transactionType, totalAmount, message);
    }

    public static ShopOperationResult CreateFailure(ShopTransactionType transactionType, int totalAmount, string message)
    {
        return new ShopOperationResult(false, transactionType, totalAmount, message);
    }
}
