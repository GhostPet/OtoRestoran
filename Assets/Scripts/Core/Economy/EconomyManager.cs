using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [Header("Başlangıç Ayarları")]
    [SerializeField] private int startingBalance = 1000;
    [SerializeField] private bool initializeOnAwake = true;

    private int currentBalance;
    private bool isInitialized;

    /// <summary>
    /// Oyuncunun o an sahip olduğu para miktarını dışarıya sadece okunur olarak açar.
    /// Para değişimi her zaman metodlar üzerinden yapılır.
    /// </summary>
    public int CurrentBalance => currentBalance;

    /// <summary>
    /// Sistem hazır mı bilgisini verir.
    /// Özellikle başka sistemler sahne açılışında economy'e erişecekse bu bilgi faydalıdır.
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// Bakiye her değiştiğinde yeni bakiye bilgisini yayınlar.
    /// UI tarafı para yazısını güncellemek için bunu dinleyebilir.
    /// </summary>
    public event Action<int> BalanceChanged;

    /// <summary>
    /// Economy üzerinden geçen her işlemin sonucunu yayınlar.
    /// İleride log, analytics, save veya bildirim sistemi buna bağlanabilir.
    /// </summary>
    public event Action<EconomyTransactionResult> TransactionProcessed;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            Initialize(startingBalance);
        }
    }

    /// <summary>
    /// Economy sistemini verilen başlangıç parası ile başlatır.
    /// Bu metod yeni oyun açılırken veya save'den veri yüklenirken kullanılabilir.
    /// </summary>
    public void Initialize(int initialBalance)
    {
        if (initialBalance < 0)
        {
            initialBalance = 0;
        }

        currentBalance = initialBalance;
        isInitialized = true;

        var result = new EconomyTransactionResult(
            true,
            EconomyTransactionType.Initialize,
            0,
            currentBalance,
            currentBalance,
            "Ekonomi sistemi başlatıldı.",
            "Initial balance setup");

        RaiseTransactionEvents(result);
    }

    /// <summary>
    /// Dış sistemlerin hızlıca 'bu ürün alınabiliyor mu?' kontrolü yapabilmesi için yazıldı.
    /// Burada sadece para yeterliliği kontrol edilir.
    /// </summary>
    public bool CanAfford(int amount)
    {
        if (!isInitialized)
        {
            return false;
        }

        if (amount < 0)
        {
            return false;
        }

        return currentBalance >= amount;
    }

    /// <summary>
    /// Oyuncunun parasından harcama düşer.
    /// Shop satın alma işlemleri gibi durumlar bu metod üzerinden gitmelidir.
    /// </summary>
    public bool TrySpend(int amount, string reason, out EconomyTransactionResult result)
    {
        if (!isInitialized)
        {
            result = CreateFailedResult(EconomyTransactionType.ShopPurchase, amount, "Ekonomi sistemi henüz başlatılmadı.", reason);
            return false;
        }

        if (amount <= 0)
        {
            result = CreateFailedResult(EconomyTransactionType.ShopPurchase, amount, "Harcama tutarı 0'dan büyük olmalıdır.", reason);
            return false;
        }

        if (!CanAfford(amount))
        {
            result = CreateFailedResult(EconomyTransactionType.ShopPurchase, amount, "Yetersiz bakiye.", reason);
            return false;
        }

        int previousBalance = currentBalance;
        currentBalance -= amount;

        result = new EconomyTransactionResult(
            true,
            EconomyTransactionType.ShopPurchase,
            amount,
            previousBalance,
            currentBalance,
            "Harcama işlemi başarılı.",
            reason);

        RaiseTransactionEvents(result);
        return true;
    }

    /// <summary>
    /// Oyuncuya para ekler.
    /// Ürün satışı veya görev ödülü gibi durumlar için kullanılabilir.
    /// </summary>
    public bool TryEarn(int amount, string reason, out EconomyTransactionResult result)
    {
        if (!isInitialized)
        {
            result = CreateFailedResult(EconomyTransactionType.ShopSale, amount, "Ekonomi sistemi henüz başlatılmadı.", reason);
            return false;
        }

        if (amount <= 0)
        {
            result = CreateFailedResult(EconomyTransactionType.ShopSale, amount, "Kazanım tutarı 0'dan büyük olmalıdır.", reason);
            return false;
        }

        int previousBalance = currentBalance;
        currentBalance += amount;

        result = new EconomyTransactionResult(
            true,
            EconomyTransactionType.ShopSale,
            amount,
            previousBalance,
            currentBalance,
            "Gelir işlemi başarılı.",
            reason);

        RaiseTransactionEvents(result);
        return true;
    }

    /// <summary>
    /// Save/Load gibi durumlarda bakiyeyi doğrudan ayarlamak için ayrı tutuldu.
    /// Oynanış içi para ekleme/çıkarma yerine mümkünse TrySpend ve TryEarn kullanılmalıdır.
    /// </summary>
    public void SetBalance(int newBalance, string reason = "Manual balance update")
    {
        if (newBalance < 0)
        {
            newBalance = 0;
        }

        int previousBalance = currentBalance;
        currentBalance = newBalance;
        isInitialized = true;

        var result = new EconomyTransactionResult(
            true,
            EconomyTransactionType.ManualAdjustment,
            Mathf.Abs(currentBalance - previousBalance),
            previousBalance,
            currentBalance,
            "Bakiye güncellendi.",
            reason);

        RaiseTransactionEvents(result);
    }

    private EconomyTransactionResult CreateFailedResult(EconomyTransactionType transactionType, int amount, string message, string reason)
    {
        return new EconomyTransactionResult(
            false,
            transactionType,
            Mathf.Max(0, amount),
            currentBalance,
            currentBalance,
            message,
            reason);
    }

    private void RaiseTransactionEvents(EconomyTransactionResult result)
    {
        BalanceChanged?.Invoke(currentBalance);
        TransactionProcessed?.Invoke(result);
    }
}
