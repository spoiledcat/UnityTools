using System.Threading;

namespace SpoiledCat.Git
{
	using System;
	using ProcessManager;
	using Threading;

	public abstract class GitProcessTask<T> : ProcessTask<T>
	{
		private IGitEnvironment GitEnvironment => GitProcessEnvironment.Instance.Environment as IGitEnvironment;
		public GitProcessTask(CancellationToken token,
			IOutputProcessor<T> processor = null)
			: base(token, null, null, processor, GitProcessEnvironment.Instance)
		{
			if (ProcessEnvironment == null)
				throw new InvalidOperationException("You need to initialize a GitProcessEnvironment instance");
		}

		public override string ProcessName => GitEnvironment.GitExecutablePath;
	}
}
