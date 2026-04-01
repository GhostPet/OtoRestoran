using NUnit.Framework;
using System.Collections.Generic;

public class RuntimeInfrastructureTests {
	[SetUp]
	public void SetUp() {
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "caller";
		CommandExecutionContext.CurrentLine = 3;
		CommandExecutionContext.CurrentRobot = null;
	}

	[TearDown]
	public void TearDown() {
		CommandExecutionContext.ClearVariables();
		CommandExecutionContext.CurrentContextId = "default";
		CommandExecutionContext.CurrentLine = -1;
		CommandExecutionContext.CurrentRobot = null;
	}

	[Test]
	public void RobotCommandQueue_PreservesFifoOrderAndClear() {
		var queue = new RobotCommandQueue();
		var first = new EnqueuedCommand(new TestCommandStub(), new object[] { "first" }, 1);
		var second = new EnqueuedCommand(new TestCommandStub(), new object[] { "second" }, 2);

		queue.Enqueue(first);
		queue.Enqueue(second);

		Assert.AreSame(first, queue.Peek());
		Assert.AreSame(first, queue.Dequeue());
		Assert.AreSame(second, queue.Dequeue());
		Assert.IsFalse(queue.HasCommands());

		queue.Enqueue(first);
		queue.Clear();
		Assert.IsFalse(queue.HasCommands());
	}

	[Test]
	public void EnqueuedCommand_CapturesValidationErrorsAndRestoresCallerContext() {
		var enqueued = new EnqueuedCommand(new FailingCommandStub(), new object[] { "bad_target" }, 17, "queued");

		bool done = enqueued.Tick();

		Assert.IsTrue(done);
		Assert.IsTrue(enqueued.IsCompleted);
		Assert.IsFalse(string.IsNullOrWhiteSpace(enqueued.ExecutionError));
		StringAssert.Contains("Line 17", enqueued.ExecutionError);
		Assert.AreEqual("caller", CommandExecutionContext.CurrentContextId);
		Assert.AreEqual(3, CommandExecutionContext.CurrentLine);
	}

	[Test]
	public void Validator_AcceptsPythonStyleDslWithoutCommandRegistry() {
		var validator = new Validator();
		var lines = new List<string> {
		 "def main():",
			"    values = range(3)",
			"    for value in values:",
			"        x = int(value)",
			""
		};

		List<ValidationError> errors = validator.Validate(lines);

		Assert.IsNotNull(errors);
		Assert.AreEqual(0, errors.Count);
	}

	[Test]
	public void Validator_AcceptsPythonStyleLiteralsAndWhileTrue() {
		var validator = new Validator();
		var lines = new List<string> {
			"def main():",
		   "    missing = None",
			"    active = True",
			"    if missing == None:",
			"        while True:",
			"            active = False",
			""
		};

		List<ValidationError> errors = validator.Validate(lines);

		Assert.IsNotNull(errors);
		Assert.AreEqual(0, errors.Count);
	}

	[Test]
	public void Validator_ReportsSyntaxErrorsForInvalidPythonStyleDsl() {
		var validator = new Validator();
		var lines = new List<string> {
			"def main():",
			"    values = range(3)",
			"    if values",
			"        x = 1"
		};

		List<ValidationError> errors = validator.Validate(lines);

		Assert.IsNotNull(errors);
		Assert.AreEqual(1, errors.Count);
		Assert.IsInstanceOf<InvalidFunctionCallError>(errors[0]);
	}

	private sealed class TestCommandStub : IRobotCommand {
		public int ExpectedArgumentCount => 1;

		public bool Tick(params object[] args) {
			return true;
		}

		public void Reset() {
		}
	}

	private sealed class FailingCommandStub : IRobotCommand {
		public int ExpectedArgumentCount => 1;

		public bool Tick(params object[] args) {
			throw new ValidationError("Line 17: invalid target", 17);
		}

		public void Reset() {
		}
	}
}
