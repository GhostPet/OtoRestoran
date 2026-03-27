using System;
using UnityEngine;

public class WaitCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private float _timeRemaining = 0;

	// Test-dostu delta enjeksiyonu. Null ise varsayılan olarak Time.deltaTime kullanılır.
	// Testler burada sabit bir delta sağlayarak deterministik çalıştırma elde edebilir.
	public static Func<float> DeltaProvider { get; set; } = null;

	public bool Tick(params object[] args) {
		// Sadece zamanlayıcı sıfır veya sıfırın altındaysa başlangıç argümanını oku.
		if (_timeRemaining <= 0) {
			if (args == null || args.Length == 0)
				throw new ValidationError($"wait() requires a duration argument (line {CommandExecutionContext.CurrentLine})", CommandExecutionContext.CurrentLine);

			float seconds = Convert.ToSingle(args[0]);
			_timeRemaining = seconds;
			int line = CommandExecutionContext.CurrentLine;
			Debug.Log($"[WaitCommand] Waiting for {seconds}s (line {line})");
		}

		// Use injected delta for tests when provided, otherwise fall back to Time.deltaTime
		float dt = DeltaProvider != null ? DeltaProvider() : Time.deltaTime;
		_timeRemaining -= dt;

		if (_timeRemaining <= 0) {
			_timeRemaining = 0;
			return true;
		}

		return false;
	}

	public void Reset() {
		_timeRemaining = 0;
	}
}