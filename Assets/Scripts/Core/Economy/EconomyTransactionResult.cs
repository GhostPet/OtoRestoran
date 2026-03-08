using System;

/// <summary>
/// Economy tarafında gerçekleşen her işlemin sonucunu standart bir yapıda taşır.
/// Bu sayede farklı sistemler aynı veri modeli ile sonucu okuyabilir.
/// </summary>
[Serializable]
public readonly struct EconomyTransactionResult
{
    public EconomyTransactionResult(
        bool success,
        EconomyTransactionType transactionType,
        int amount,
        int previousBalance,
        int currentBalance,
        string message,
        string reason)
    {
        Success = success;
        TransactionType = transactionType;
        Amount = amount;
        PreviousBalance = previousBalance;
        CurrentBalance = currentBalance;
        Message = message;
        Reason = reason;
    }

    /// <summary>
    /// İşlem tamamlandı mı bilgisini verir.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// İşlemin hangi kategoriye ait olduğunu belirtir.
    /// </summary>
    public EconomyTransactionType TransactionType { get; }

    /// <summary>
    /// İşlemde kullanılan para miktarıdır.
    /// Harcama veya gelir olmasına göre yorum üst sistemde yapılabilir.
    /// </summary>
    public int Amount { get; }

    /// <summary>
    /// İşlem öncesi bakiye.
    /// </summary>
    public int PreviousBalance { get; }

    /// <summary>
    /// İşlem sonrası bakiye.
    /// </summary>
    public int CurrentBalance { get; }

    /// <summary>
    /// UI veya log ekranlarında gösterilebilecek açıklama mesajı.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// İşlemi tetikleyen üst seviye nedeni saklar.
    /// Örneğin hangi ürünün alındığı veya hangi sistemin parayı değiştirdiği burada tutulabilir.
    /// </summary>
    public string Reason { get; }
}
