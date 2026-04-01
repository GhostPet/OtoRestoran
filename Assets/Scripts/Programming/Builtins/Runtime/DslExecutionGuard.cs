public sealed class DslExecutionGuard {
	private readonly int _maxStatements;
	private readonly int _maxLoopIterations;
	private int _statementCount;
	private int _loopIterationCount;

	public DslExecutionGuard(int maxStatements = 10000, int maxLoopIterations = 5000) {
		_maxStatements = maxStatements > 0 ? maxStatements : 10000;
		_maxLoopIterations = maxLoopIterations > 0 ? maxLoopIterations : 5000;
	}

	public void EnterStatement(int line) {
		_statementCount++;
		if (_statementCount > _maxStatements) {
			throw new DslRuntimeError($"Execution stopped: statement limit exceeded (line {line})", line);
		}
	}

	public void EnterLoopIteration(int line) {
		_loopIterationCount++;
		if (_loopIterationCount > _maxLoopIterations) {
			throw new DslRuntimeError($"Execution stopped: loop iteration limit exceeded (line {line})", line);
		}
	}
}
