using System;

/// <summary>
/// Restoran değerlendirme puanı üzerinde yapılan bir güncellemenin sonucunu taşır.
/// İleride müşteri yorumu, fiyat etkisi veya özel olaylar geldiğinde aynı model kullanılabilir.
/// </summary>
[Serializable]
public readonly struct RestaurantRatingUpdateResult {
	public RestaurantRatingUpdateResult(float previousRating, float currentRating, float inputRating, float weight, int totalVotes, string reason) {
		PreviousRating = previousRating;
		CurrentRating = currentRating;
		InputRating = inputRating;
		Weight = weight;
		TotalVotes = totalVotes;
		Reason = reason;
	}

	public float PreviousRating { get; }

	public float CurrentRating { get; }

	public float InputRating { get; }

	public float Weight { get; }

	public int TotalVotes { get; }

	public string Reason { get; }
}
