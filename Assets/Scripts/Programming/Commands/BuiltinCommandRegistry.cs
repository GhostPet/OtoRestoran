using System;
using System.Collections.Generic;

public static class BuiltinCommandRegistry {
	private static readonly Dictionary<string, IRobotCommand> _commands = new();

	static BuiltinCommandRegistry() {
		Register("move_to", new MoveToCommand());
		Register("serve", new ServeOrderCommand());
		Register("clean", new CleanTableCommand());
		Register("wait", new WaitCommand());
		Register("pickup", new PickupCommand());
		Register("print", new PrintCommand());
		Register("drop", new DropCommand());
	}

	public static void Register(string name, IRobotCommand command) {
		_commands[name] = command;
	}

	public static bool IsBuiltin(string name) => _commands.ContainsKey(name);

	public static IRobotCommand GetCommand(string name) {
		if (!_commands.ContainsKey(name))
			throw new Exception($"Command '{name}' not registered.");

		IRobotCommand prototype = _commands[name];
		IRobotCommand instance = Activator.CreateInstance(prototype.GetType()) as IRobotCommand;
		return instance ?? prototype;
	}

	public static List<string> GetAllCommandNames() => new(_commands.Keys);
}
