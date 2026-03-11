using System;
using UnityEngine;

/// <summary>
/// Oyunun genel ilerleme puanını yöneten ana sınıftır.
/// Bu skor doğrudan para değildir; oyunda yeni sistemlerin, shop ürünlerinin veya içeriklerin açılmasında kullanılabilir.
/// </summary>
public class GameScoreManager : MonoBehaviour
{
    [Header("Başlangıç Ayarları")]
    [SerializeField] private int startingScore;
    [SerializeField] private bool initializeOnAwake = true;

    private int currentScore;
    private bool isInitialized;

    /// <summary>
    /// O anki toplam game score değeri.
    /// </summary>
    public int CurrentScore => currentScore;

    public bool IsInitialized => isInitialized;

    /// <summary>
    /// Game score her değiştiğinde yeni değeri yayınlar.
    /// Shop UI veya başka ilerleme ekranları bunu dinleyebilir.
    /// </summary>
    public event Action<int> ScoreChanged;

    /// <summary>
    /// Game score değişimlerinin tamamını standart bir veri modeli ile yayınlar.
    /// </summary>
    public event Action<GameScoreChangeResult> ScoreUpdated;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            Initialize(startingScore);
        }
    }

    /// <summary>
    /// Sistemi başlangıç skoru ile ayağa kaldırır.
    /// Yeni oyun başlatılırken veya save yüklenirken kullanılabilir.
    /// </summary>
    public void Initialize(int initialScore)
    {
        if (initialScore < 0)
        {
            initialScore = 0;
        }

        currentScore = initialScore;
        isInitialized = true;

        RaiseScoreEvents(new GameScoreChangeResult(currentScore, currentScore, 0, "Game score sistemi başlatıldı."));
    }

    /// <summary>
    /// Dış sistemlerin hızlıca 'gerekli score'a ulaşıldı mı?' kontrolü yapabilmesi için yazıldı.
    /// </summary>
    public bool HasRequiredScore(int requiredScore)
    {
        if (!isInitialized)
        {
            return false;
        }

        if (requiredScore <= 0)
        {
            return true;
        }

        return currentScore >= requiredScore;
    }

    /// <summary>
    /// Game score'a puan ekler.
    /// Şu an kaynağı belirsiz olsa da ileride görev, sipariş, gün sonu performansı gibi sistemler bunu çağırabilir.
    /// </summary>
    public void AddScore(int amount, string reason = "Game score gained")
    {
        if (!isInitialized || amount <= 0)
        {
            return;
        }

        int previousScore = currentScore;
        currentScore += amount;

        RaiseScoreEvents(new GameScoreChangeResult(previousScore, currentScore, amount, reason));
    }

    /// <summary>
    /// Gerekirse score düşürme ihtiyacı için ayrı tutuldu.
    /// Şu an aktif kullanılmasa da ileride ceza sistemleri için hazırdır.
    /// </summary>
    public void RemoveScore(int amount, string reason = "Game score lost")
    {
        if (!isInitialized || amount <= 0)
        {
            return;
        }

        int previousScore = currentScore;
        currentScore -= amount;
        if (currentScore < 0)
        {
            currentScore = 0;
        }

        RaiseScoreEvents(new GameScoreChangeResult(previousScore, currentScore, currentScore - previousScore, reason));
    }

    /// <summary>
    /// Save/load veya debug senaryolarında score'u doğrudan ayarlamak için kullanılır.
    /// </summary>
    public void SetScore(int newScore, string reason = "Game score manually updated")
    {
        if (newScore < 0)
        {
            newScore = 0;
        }

        int previousScore = currentScore;
        currentScore = newScore;
        isInitialized = true;

        RaiseScoreEvents(new GameScoreChangeResult(previousScore, currentScore, currentScore - previousScore, reason));
    }

    private void RaiseScoreEvents(GameScoreChangeResult result)
    {
        ScoreChanged?.Invoke(currentScore);
        ScoreUpdated?.Invoke(result);
    }
}
