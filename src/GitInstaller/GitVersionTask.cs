using System.Threading;

namespace SpoiledCat.Git
{
	using Threading;
	using Utilities;

	public class GitVersionTask : GitProcessTask<TheVersion>
	{
		private const string TaskName = "git --version";

		public GitVersionTask(CancellationToken token, IOutputProcessor<TheVersion> processor = null)
			: base(token, processor ?? new VersionOutputProcessor())
		{
			Name = TaskName;
		}

		public override string ProcessArguments => "--version";
		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
		public override string Message { get; set; } = "Reading git version...";
	}
}
