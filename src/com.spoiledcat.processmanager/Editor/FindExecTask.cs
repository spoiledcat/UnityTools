namespace SpoiledCat.ProcessManager
{
	using NiceIO;
	using Threading;
	using Unity;

	public class FindExecTask : SimpleProcessTask<NPath>
	{
		public FindExecTask(ITaskManager taskManager, IProcessManager processManager,
			string execToFind, IEnvironment environment)
			: base(taskManager, processManager,
				  environment.IsWindows ? "where" : "which",
				  execToFind,
				  new FirstNonNullLineOutputProcessor<NPath>(line => new NPath(line)))
		{
			processManager.Configure(this);
		}

		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
	}
}
