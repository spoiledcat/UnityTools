using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpoiledCat.Base.Tests
{
	using System.Collections;
	using Logging;
	using NiceIO;
	using Threading;
	using System.Threading.Tasks;
	using System.Threading;


	public partial class BaseTest : IDisposable
	{
		public BaseTest()
		{
			LogHelper.LogAdapter = new NUnitLogAdapter();
			LogHelper.TracingEnabled = true;

			var syncContext = new TestThreadSynchronizationContext(default(CancellationToken));
			TaskManager = new TaskManager().Initialize(syncContext);

			LogHelper.Info($"Starting test fixture. Main thread is {TaskManager.UIThread}");
		}

		public void Dispose()
		{
			TaskManager?.Dispose();
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager, [CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new NUnitLogAdapter(), true);
			watch = new Stopwatch();

			taskManager = TaskManager;

			logger.Trace("START");
			watch.Start();
		}

		protected void RunTest(Func<IEnumerator> testMethodToRun)
		{
			var test = testMethodToRun();
			while (test.MoveNext())
			{}
		}
	}
}
