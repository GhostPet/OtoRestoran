namespace RestAutoRant.Shared.Interfaces {
	public interface IRobotCommand {
		bool CanExecute(IRobot robot);
		void Execute(IRobot robot);
		bool IsCompleted { get; }
	}
}
