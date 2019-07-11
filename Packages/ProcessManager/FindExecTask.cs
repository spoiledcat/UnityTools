using System.Threading;

namespace SpoiledCat.Threading
{
	using NiceIO;
	using ProcessManager;
	using Unity;

	public class FindExecTask : ProcessTask<NPath>
	{
		private readonly string arguments;

		public FindExecTask(string executable, IEnvironment environment, CancellationToken token)
			 : base(token, outputProcessor: new FirstNonNullLineOutputProcessor<NPath>(line => new NPath(line)))
		{
			Name = environment.IsWindows ? "where" : "which";
			arguments = executable;
		}

		public override string ProcessName => Name;
		public override string ProcessArguments => arguments;
		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
	}
}
