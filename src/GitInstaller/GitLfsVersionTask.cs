using System.Threading;

namespace SpoiledCat.Git
{
	using Threading;
	using Utilities;

	public class GitLfsVersionTask : GitProcessTask<TheVersion>
	{
		private const string TaskName = "git lfs version";

		public GitLfsVersionTask(CancellationToken token, IOutputProcessor<TheVersion> processor = null)
			: base(token, processor ?? new LfsVersionOutputProcessor())
		{
			Name = TaskName;
		}

		public override string ProcessArguments => "lfs version";
		public override TaskAffinity Affinity => TaskAffinity.Concurrent;
		public override string Message { get; set; } = "Reading LFS version...";
	}
}
