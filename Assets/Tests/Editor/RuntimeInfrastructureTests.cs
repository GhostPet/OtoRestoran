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
		var first = new EnqueuedCommand(new DropCommand(), new object[] { "first" }, 1);
		var second = new EnqueuedCommand(new CleanTableCommand(), new object[] { "second" }, 2);

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
		var enqueued = new EnqueuedCommand(new MoveToCommand(), new object[] { "bad_target" }, 17, "queued");

		bool done = enqueued.Tick();

		Assert.IsTrue(done);
		Assert.IsTrue(enqueued.IsCompleted);
		Assert.IsFalse(string.IsNullOrWhiteSpace(enqueued.ExecutionError));
		StringAssert.Contains("Line 17", enqueued.ExecutionError);
		Assert.AreEqual("caller", CommandExecutionContext.CurrentContextId);
		Assert.AreEqual(3, CommandExecutionContext.CurrentLine);
	}

	[Test]
	public void Validator_ReportsUndefinedCommandsAndArgumentCountIssues() {
		var validator = new Validator();
		var lines = new List<string> {
			"print(hello)",
			"no_such_command()",
			"move_to()"
		};

		List<ValidationError> errors = validator.Validate(lines);

		Assert.IsNotNull(errors);
		Assert.AreEqual(2, errors.Count);
		Assert.IsTrue(errors.Exists(error => error is UndefinedVariableError));
		Assert.IsTrue(errors.Exists(error => error is InvalidArgumentCountError));
	}
}
