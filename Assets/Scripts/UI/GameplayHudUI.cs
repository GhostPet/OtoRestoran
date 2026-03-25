using TMPro;
using UnityEngine;

public class GameplayHudUI : MonoBehaviour {
	[SerializeField] private TMP_Text balanceText;
	[SerializeField] private TMP_Text scoreText;
	[SerializeField] private TMP_Text phaseText;
	[SerializeField] private TMP_Text countdownText;

	private EconomyManager economyManager;
	private GameScoreManager gameScoreManager;
	private PhaseStateMachine phaseStateMachine;

	public void SetReferences(
		EconomyManager economy,
		GameScoreManager score,
		PhaseStateMachine phaseMachine) {
		Unsubscribe();

		economyManager = economy;
		gameScoreManager = score;
		phaseStateMachine = phaseMachine;

		Subscribe();
		RefreshAll();
	}

	private void OnEnable() {
		Subscribe();
		RefreshAll();
	}

	private void OnDisable() {
		Unsubscribe();
	}

	private void Update() {
		RefreshCountdown();
	}

	private void Subscribe() {
		if (!isActiveAndEnabled) {
			return;
		}

		if (economyManager != null) {
			economyManager.BalanceChanged -= HandleBalanceChanged;
			economyManager.BalanceChanged += HandleBalanceChanged;
		}

		if (gameScoreManager != null) {
			gameScoreManager.ScoreChanged -= HandleScoreChanged;
			gameScoreManager.ScoreChanged += HandleScoreChanged;
		}

		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
			phaseStateMachine.PhaseChanged += HandlePhaseChanged;
		}
	}

	private void Unsubscribe() {
		if (economyManager != null) {
			economyManager.BalanceChanged -= HandleBalanceChanged;
		}

		if (gameScoreManager != null) {
			gameScoreManager.ScoreChanged -= HandleScoreChanged;
		}

		if (phaseStateMachine != null) {
			phaseStateMachine.PhaseChanged -= HandlePhaseChanged;
		}
	}

	private void RefreshAll() {
		RefreshBalance();
		RefreshScore();
		RefreshPhase();
		RefreshCountdown();
	}

	private void RefreshBalance() {
		if (balanceText == null) {
			return;
		}

		int balance = economyManager != null ? economyManager.CurrentBalance : 0;
		balanceText.text = "Bakiye: " + balance;
	}

	private void RefreshScore() {
		if (scoreText == null) {
			return;
		}

		int score = gameScoreManager != null ? gameScoreManager.CurrentScore : 0;
		scoreText.text = "Skor: " + score;
	}

	private void RefreshPhase() {
		if (phaseText == null) {
			return;
		}

		string phaseLabel = "-";
		if (phaseStateMachine != null) {
			switch (phaseStateMachine.CurrentPhase) {
				case PhaseStateMachine.Phase.Preparation:
					phaseLabel = "Hazırlık";
					break;
				case PhaseStateMachine.Phase.Service:
					phaseLabel = "Servis";
					break;
				case PhaseStateMachine.Phase.DayEnd:
					phaseLabel = "Gün Sonu";
					break;
				default:
					phaseLabel = "Bekleniyor";
					break;
			}
		}

		phaseText.text = "Aşama: " + phaseLabel;
	}

	private void RefreshCountdown() {
		if (countdownText == null) {
			return;
		}

		if (phaseStateMachine == null) {
			countdownText.text = "Sayaç: --:--";
			return;
		}

		float remaining = Mathf.Max(0f, phaseStateMachine.PhaseTimeRemaining);
		int minutes = Mathf.FloorToInt(remaining / 60f);
		int seconds = Mathf.FloorToInt(remaining % 60f);
		countdownText.text = "Sayaç: " + minutes.ToString("00") + ":" + seconds.ToString("00");
	}

	private void HandleBalanceChanged(int newBalance) {
		RefreshBalance();
	}

	private void HandleScoreChanged(int newScore) {
		RefreshScore();
	}

	private void HandlePhaseChanged(PhaseStateMachine.Phase phase) {
		RefreshPhase();
		RefreshCountdown();
	}
}
