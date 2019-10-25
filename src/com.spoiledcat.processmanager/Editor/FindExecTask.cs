namespace SpoiledCat.Threading
{
	using NiceIO;
	using ProcessManager;
	using Unity;

	public class FindExecTask : ProcessTask<NPath>
	{
		public FindExecTask(ITaskManager taskManager, IProcessManager processManager,
			string executable, IEnvironment environment)
			 : base(taskManager, taskManager.Token, processManager.DefaultProcessEnvironment,
				 outputProcessor: new FirstNonNullLineOutputProcessor<NPath>(line => new NPath(line)))
		{
			Name = environment.IsWindows ? "where" : "which";
			ProcessArguments = executable;
			processManager.Configure(this);
		}

		public override string ProcessName => Name;
		public override string ProcessArguments { get; }
		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
	}
}
