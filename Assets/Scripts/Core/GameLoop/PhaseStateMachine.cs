using System;
using UnityEngine;

/// <summary>
/// Oyun fazlarını (Hazırlık → Servis → Gün Sonu) yöneten state machine.
/// DayManager bu sınıfı kullanarak faz geçişlerini tetikler.
/// </summary>
public class PhaseStateMachine : MonoBehaviour {
	public enum Phase {
		None,
		Preparation,   // Hazırlık fazı: robotları programla, masa düzenle
		Service,       // Servis fazı: müşteriler gelir, robotlar çalışır
		DayEnd         // Gün sonu: puan hesaplama, kayıt
	}

	[Header("Faz Süreleri (saniye)")]
	[SerializeField] private float preparationDuration = 300f;
	[SerializeField] private float serviceDuration = 180f;

	private Phase currentPhase = Phase.None;
	private float phaseTimer;
	private bool phaseRunning;

	public Phase CurrentPhase => currentPhase;
	public float PhaseTimeRemaining => phaseTimer;
	public float PhaseDuration => GetPhaseDuration(currentPhase);
	public float PhaseProgress => PhaseDuration > 0f ? 1f - (phaseTimer / PhaseDuration) : 1f;

	/// <summary>Faz değiştiğinde yayınlanır. Parametre: yeni faz.</summary>
	public event Action<Phase> PhaseChanged;

	/// <summary>Faz süre dolumunda yayınlanır.</summary>
	public event Action<Phase> PhaseEnded;

	private void Update() {
		if (!phaseRunning) return;

		phaseTimer -= Time.deltaTime;
		if (phaseTimer <= 0f) {
			phaseTimer = 0f;
			EndCurrentPhase();
		}
	}

	// ─── Genel API ────────────────────────────────────────────────────

	public void EnterPhase(Phase phase) {
		if (currentPhase == phase) return;

		currentPhase = phase;
		phaseTimer = GetPhaseDuration(phase);
		phaseRunning = phase != Phase.None && phase != Phase.DayEnd;

		PhaseChanged?.Invoke(phase);
		Debug.Log($"[PhaseStateMachine] → {phase}  (süre: {phaseTimer}s)");
	}

	/// <summary>Hazırlık fazını başlatır.</summary>
	public void StartPreparation() => EnterPhase(Phase.Preparation);

	/// <summary>Servis fazını başlatır.</summary>
	public void StartService() => EnterPhase(Phase.Service);

	/// <summary>Gün sonu fazına geçer.</summary>
	public void EndDay() => EnterPhase(Phase.DayEnd);

	/// <summary>Aktif fazı manuel olarak sonlandırır (erken geçiş için).</summary>
	public void ForceNextPhase() {
		EndCurrentPhase();
	}

	// ─── Dahili ───────────────────────────────────────────────────────

	private void EndCurrentPhase() {
		phaseRunning = false;
		PhaseEnded?.Invoke(currentPhase);
		Debug.Log($"[PhaseStateMachine] Faz bitti: {currentPhase}");

		switch (currentPhase) {
			case Phase.Preparation: StartService(); break;
			case Phase.Service: EndDay(); break;
		}
	}

	private float GetPhaseDuration(Phase phase) {
		switch (phase) {
			case Phase.Preparation: return preparationDuration;
			case Phase.Service: return serviceDuration;
			default: return 0f;
		}
	}
}
