using System;

public class EnqueuedCommand : IRobotCommand, ICompletable {
	private readonly IRobotCommand _impl;
	private readonly object[] _args;
	private readonly int _line;
	private readonly string _contextId;
	private bool _isCompleted;
	public string ExecutionError { get; private set; }
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

	public EnqueuedCommand(IRobotCommand impl, object[] args, int line = -1, string contextId = null) {
		_impl = impl ?? throw new ArgumentNullException(nameof(impl));
		_args = args ?? new object[0];
		_line = line;
		_contextId = string.IsNullOrEmpty(contextId) ? CommandExecutionContext.CurrentContextId : contextId;
		IsCompleted = false;
	}

	public int ExpectedArgumentCount => _impl.ExpectedArgumentCount;


	public bool Tick(params object[] args) {
		// Switch to this enqueued command's context
		var prevContext = CommandExecutionContext.CurrentContextId;
		CommandExecutionContext.CurrentContextId = _contextId;

		// Set execution context line for accurate error reporting
		int prevLine = CommandExecutionContext.CurrentLine;
		CommandExecutionContext.CurrentLine = _line;

		// If caller passed a robot as first arg, set it for this context so commands
		// relying on CurrentRobot see the right robot.
		if (args != null && args.Length > 0 && args[0] is IRobot r) {
			CommandExecutionContext.CurrentRobot = r;
		}

		try {
			bool done = _impl.Tick(_args);
			if (done) IsCompleted = true;

			// restore previous
			CommandExecutionContext.CurrentLine = prevLine;
			CommandExecutionContext.CurrentContextId = prevContext;
			return done;
		} catch (ValidationError vex) {
			ExecutionError = vex.ToString();
			IsCompleted = true;
		} catch (Exception ex) {
			ExecutionError = $"Runtime error in queued command: {ex.Message}";
			IsCompleted = true;
		}

		// restore previous
		CommandExecutionContext.CurrentLine = prevLine;
		CommandExecutionContext.CurrentContextId = prevContext;
		return true;
	}

	public void Reset() {
		_impl.Reset();
		IsCompleted = false;
		ExecutionError = null;
	}
}
