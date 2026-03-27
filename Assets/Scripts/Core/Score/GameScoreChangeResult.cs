using System;

/// <summary>
/// Game score üzerinde gerçekleşen bir değişikliğin özet sonucunu taşır.
/// Bu yapı sayesinde UI, save veya analytics sistemleri aynı veri modelini kullanabilir.
/// </summary>
[Serializable]
public readonly struct GameScoreChangeResult {
	public GameScoreChangeResult(int previousScore, int currentScore, int delta, string reason) {
		PreviousScore = previousScore;
		CurrentScore = currentScore;
		Delta = delta;
		Reason = reason;
	}

	public int PreviousScore { get; }

	public int CurrentScore { get; }

	public int Delta { get; }

	public string Reason { get; }
}
