using System;
using UnityEngine;

/// <summary>
/// Restoranın müşteriler tarafından verilen değerlendirme puanını yönetir.
/// Bu puan 1 ile 10 arasında küsuratlıdır ve ileride müşteri sıklığı, fiyat toleransı gibi sistemlere veri sağlayabilir.
/// </summary>
public class RestaurantRatingManager : MonoBehaviour {
	[Header("Başlangıç Ayarları")]
	[SerializeField] private float startingRating = 5f;
	[SerializeField] private bool initializeOnAwake = true;

	[Header("Sınırlar")]
	[SerializeField] private float minimumRating = 1f;
	[SerializeField] private float maximumRating = 10f;

	[Header("Kararlılık Ayarı")]
	[SerializeField] private int initialVirtualVoteCount = 5;

	private float currentRating;
	private float weightedRatingTotal;
	private float totalWeight;
	private int totalVotes;
	private bool isInitialized;

	public float CurrentRating => currentRating;

	public int TotalVotes => totalVotes;

	public bool IsInitialized => isInitialized;

	/// <summary>
	/// Rating değeri değiştiğinde yeni değeri yayınlar.
	/// UI veya müşteri frekans sistemi bunu dinleyebilir.
	/// </summary>
	public event Action<float> RatingChanged;

	/// <summary>
	/// Her rating güncellemesini detaylı veriyle yayınlar.
	/// </summary>
	public event Action<RestaurantRatingUpdateResult> RatingUpdated;

	private void Awake() {
		if (initializeOnAwake) {
			Initialize(startingRating);
		}
	}

	/// <summary>
	/// Rating sistemini başlatır.
	/// Başlangıçta birkaç sanal oy kullanarak ilk rating'in çok sert oynamasını önler.
	/// </summary>
	public void Initialize(float initialRating) {
		float clampedRating = ClampRating(initialRating);

		currentRating = clampedRating;
		totalVotes = Mathf.Max(0, initialVirtualVoteCount);
		totalWeight = Mathf.Max(0, initialVirtualVoteCount);
		weightedRatingTotal = clampedRating * totalWeight;
		isInitialized = true;

		RaiseRatingEvents(new RestaurantRatingUpdateResult(currentRating, currentRating, currentRating, 0f, totalVotes, "Restaurant rating sistemi başlatıldı."));
	}

	/// <summary>
	/// Tek bir müşteri değerlendirmesini sisteme ekler.
	/// Örneğin müşteri memnun kaldıysa 8.5, çok memnun kaldıysa 9.7 gibi değerler verilebilir.
	/// </summary>
	public void RegisterCustomerRating(float ratingValue, float weight = 1f, string reason = "Customer rating received") {
		if (!isInitialized) {
			return;
		}

		if (weight <= 0f) {
			return;
		}

		float clampedInput = ClampRating(ratingValue);
		float previousRating = currentRating;

		weightedRatingTotal += clampedInput * weight;
		totalWeight += weight;
		totalVotes += 1;

		if (totalWeight <= 0f) {
			currentRating = ClampRating(startingRating);
		} else {
			currentRating = ClampRating(weightedRatingTotal / totalWeight);
		}

		RaiseRatingEvents(new RestaurantRatingUpdateResult(previousRating, currentRating, clampedInput, weight, totalVotes, reason));
	}

	/// <summary>
	/// Plan değişirse doğrudan rating'i sabit bir değere çekmek için kullanılabilir.
	/// </summary>
	public void SetRating(float newRating, string reason = "Restaurant rating manually updated") {
		float clampedRating = ClampRating(newRating);
		float previousRating = currentRating;

		currentRating = clampedRating;
		weightedRatingTotal = clampedRating * Mathf.Max(1f, totalWeight);
		isInitialized = true;

		RaiseRatingEvents(new RestaurantRatingUpdateResult(previousRating, currentRating, clampedRating, 0f, totalVotes, reason));
	}

	/// <summary>
	/// İleride müşteri sıklığı sisteminde kullanılabilecek basit bir çarpan üretir.
	/// Yüksek rating müşteri talebini artırır, yüksek fiyat seviyesi bunu aşağı çekebilir.
	/// priceLevelNormalized için 0 düşük fiyat, 1 yüksek fiyat kabul edilir.
	/// </summary>
	public float EvaluateDemandModifier(float priceLevelNormalized) {
		float normalizedRating = Mathf.InverseLerp(minimumRating, maximumRating, currentRating);
		float ratingFactor = Mathf.Lerp(0.6f, 1.4f, normalizedRating);
		float pricePenalty = Mathf.Lerp(1.15f, 0.65f, Mathf.Clamp01(priceLevelNormalized));
		return ratingFactor * pricePenalty;
	}

	private float ClampRating(float rating) {
		return Mathf.Clamp(rating, minimumRating, maximumRating);
	}

	private void RaiseRatingEvents(RestaurantRatingUpdateResult result) {
		RatingChanged?.Invoke(currentRating);
		RatingUpdated?.Invoke(result);
	}
}
