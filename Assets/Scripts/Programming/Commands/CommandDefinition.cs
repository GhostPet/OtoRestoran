public class CommandDefinition {
	public string Name { get; private set; }
	public IRobotCommand CommandInstance { get; private set; }

	public CommandDefinition(string name, IRobotCommand instance) {
		Name = name;
		CommandInstance = instance;
	}
}
