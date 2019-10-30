using System;

namespace SpoiledCat.ProcessManager.Tests
{}

namespace SpoiledCat.Base.Tests
{
	using System.Collections;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using Logging;
	using NiceIO;
	using Unity;

	using Threading;
	using System.Threading.Tasks;
	using System.Threading;
	using ProcessManager;

	public partial class BaseTest
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


		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager,
			out NPath testPath, out IEnvironment environment, out IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new NUnitLogAdapter(), true);
			watch = new Stopwatch();

			taskManager = TaskManager;

			testPath = NPath.CreateTempDirectory(testName);

			environment = new UnityEnvironment(testName);
			((UnityEnvironment)environment).SetWorkingDirectory(testPath);
			environment.Initialize(testPath, testPath, "2018.4", testPath, testPath.Combine("Assets"));

			processManager = new ProcessManager(environment, taskManager.Token);

			logger.Trace("START");
			watch.Start();
		}


		protected void RunTest(Func<IEnumerator> testMethodToRun)
		{
			var test = testMethodToRun();
			while (test.MoveNext())
			{}
		}

		protected NPath? testApp;

		protected NPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
					testApp = System.Reflection.Assembly.GetExecutingAssembly().Location.ToNPath().Parent.Combine("Helper.CommandLine.exe");
				return testApp.Value;
			}
		}
	}
}
