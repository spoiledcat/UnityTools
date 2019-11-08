namespace SpoiledCat.ProcessManager
{
	using SimpleIO;
	using Threading;
	using Unity;

	public class FindExecTask : SimpleProcessTask<SPath>
	{
		public FindExecTask(ITaskManager taskManager, IProcessManager processManager,
			string execToFind, IEnvironment environment)
			: base(taskManager, processManager,
				  environment.IsWindows ? "where" : "which",
				  execToFind,
				  new FirstNonNullLineOutputProcessor<SPath>(line => new SPath(line)))
		{
			processManager.Configure(this);
		}

		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
	}
}
