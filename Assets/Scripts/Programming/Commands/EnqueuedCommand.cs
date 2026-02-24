using System;

public class EnqueuedCommand : IRobotCommand, ICompletable {
	private readonly IRobotCommand _impl;
	private readonly object[] _args;
	private readonly int _line;
	private bool _isCompleted;
	public bool IsCompleted {
		get {
			if (_impl is ICompletable c) return c.IsCompleted;
			return _isCompleted;
		}
		set {
			if (_impl is ICompletable c) c.IsCompleted = value;
			else _isCompleted = value;
		}
	}

	public EnqueuedCommand(IRobotCommand impl, object[] args, int line = -1) {
		_impl = impl ?? throw new ArgumentNullException(nameof(impl));
		_args = args ?? new object[0];
		_line = line;
		IsCompleted = false;
	}

	public int ExpectedArgumentCount => _impl.ExpectedArgumentCount;

	public bool Tick(params object[] args) {
		// Set execution context line for accurate error reporting
		int prev = CommandExecutionContext.CurrentLine;
		CommandExecutionContext.CurrentLine = _line;

		bool done = _impl.Tick(_args);
		if (done) IsCompleted = true;

		// restore previous
		CommandExecutionContext.CurrentLine = prev;
		return done;
	}

	public void Reset() {
		_impl.Reset();
		IsCompleted = false;
	}
}
