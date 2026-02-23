using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class IntegrationTests {
	[Test]
	public void BuiltinCommandRegistry_ContainsCommandsAndTypes() {
		var names = BuiltinCommandRegistry.GetAllCommandNames();
		Assert.Contains("move_to", names);
		Assert.Contains("serve", names);
		Assert.Contains("print", names);
		// Ensure GetCommand returns an object
		var c = BuiltinCommandRegistry.GetCommand("print");
		Assert.IsNotNull(c);
	}

	[Test]
	public void EnqueuedCommand_PropagatesCompletion_ForNonCompletableImpl() {
		var impl = new DropCommand();
		var enc = new EnqueuedCommand(impl, new object[] { "item" }, line: 10);
		Assert.IsFalse(enc.IsCompleted);
		// First tick: DropCommand returns false (starts), so EnqueuedCommand.Tick returns false
		bool done = enc.Tick();
		Assert.IsFalse(done);
		// Second tick: DropCommand returns true -> EnqueuedCommand should mark IsCompleted
		done = enc.Tick();
		Assert.IsTrue(done);
		Assert.IsTrue(enc.IsCompleted);
	}

	[Test]
	public void RobotCommandQueue_ProcessSequenceOfCommands() {
		var queue = new RobotCommandQueue();
		// enqueue several enqueued commands wrapping simple commands
		queue.Enqueue(new EnqueuedCommand(new DropCommand(), new object[] { "x" }, 1));
		queue.Enqueue(new EnqueuedCommand(new CleanTableCommand(), new object[] { "t1" }, 2));
		queue.Enqueue(new EnqueuedCommand(new ServeOrderCommand(), new object[] { "o1" }, 3));

		int processed = 0;
		while (queue.HasCommands()) {
			var cmd = queue.Dequeue();
			if (cmd == null) break;

			// simulate engine ticking a command until it reports completion
			int safety = 0;
			while (true) {
				bool r = cmd.Tick();
				if (r) break;
				safety++;
				Assert.Less(safety, 10, "Command did not complete in reasonable ticks");
			}
			processed++;
		}

		Assert.AreEqual(3, processed);
	}

	[Test]
	public void Validator_ReportsUndefinedAndArgumentCountErrors() {
		var v = new Validator();
		var lines = new List<string> {
			"print(hello)",     // ok: print expects 1
			"no_such_cmd()",    // undefined
			"move_to()",        // missing argument
		};

		var errs = v.Validate(lines);
		Assert.IsNotNull(errs);
		Assert.IsTrue(errs.Count >= 2);

		bool hasUndefined = false;
		bool hasArgCount = false;
		foreach (var e in errs) {
			if (e is UndefinedVariableError) hasUndefined = true;
			if (e is InvalidArgumentCountError) hasArgCount = true;
		}

		Assert.IsTrue(hasUndefined);
		Assert.IsTrue(hasArgCount);
	}
}
