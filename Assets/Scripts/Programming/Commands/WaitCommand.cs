using UnityEngine;
using System;

public class WaitCommand : IRobotCommand {
	public int ExpectedArgumentCount => 1;
	private float _timeRemaining = 0;

	public bool Tick(params object[] args) {
		float seconds = Convert.ToSingle(args[0]);

		if (_timeRemaining <= 0) {
			_timeRemaining = seconds;
			int line = CommandExecutionContext.CurrentLine;
			Debug.Log($"[WaitCommand] Waiting for {seconds}s (line {line})");
		}

		_timeRemaining -= Time.deltaTime;

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
