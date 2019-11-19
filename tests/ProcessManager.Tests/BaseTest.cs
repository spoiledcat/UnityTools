using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using SpoiledCat.Logging;
using SpoiledCat.SimpleIO;
using SpoiledCat.Unity;
using SpoiledCat.Threading;
using SpoiledCat.ProcessManager;

namespace BaseTests
{
	public partial class BaseTest
	{
		public const bool TracingEnabled = false;

		public BaseTest()
		{
			LogHelper.LogAdapter = new NUnitLogAdapter();
			LogHelper.TracingEnabled = TracingEnabled;

			var syncContext = new TestThreadSynchronizationContext(default(CancellationToken));
			TaskManager = new TaskManager().Initialize(syncContext);

			LogHelper.Trace($"Starting test fixture. Main thread is {TaskManager.UIThread}");
		}

		public void Dispose()
		{
			TaskManager?.Dispose();
		}


		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager,
			out SPath testPath, out IEnvironment environment, out IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new NUnitLogAdapter(), TracingEnabled);
			watch = new Stopwatch();

			taskManager = TaskManager;

			testPath = SPath.CreateTempDirectory(testName);

			environment = new UnityEnvironment(testName);
			((UnityEnvironment)environment).SetWorkingDirectory(testPath);
			environment.Initialize(testPath, testPath, "2018.4", testPath, testPath.Combine("Assets"));

			processManager = new ProcessManager(environment, taskManager.Token);

			logger.Trace("START");
			watch.Start();
		}

		protected async Task RunTest(Func<IEnumerator> testMethodToRun)
		{
			var scheduler = ThreadingHelper.GetUIScheduler(new TestThreadSynchronizationContext(default));
			var taskStart = new Task<IEnumerator>(testMethodToRun);
			taskStart.Start(scheduler);
			var e = await RunOn(testMethodToRun, scheduler);
			while (await RunOn(s => ((IEnumerator)s).MoveNext(), e, scheduler))
			{ }
		}

		private Task<T> RunOn<T>(Func<T> method, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}

		private Task<T> RunOn<T>(Func<object, T> method, object state, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, state, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}

		protected SPath? testApp;

		protected SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
					testApp = System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent.Combine("Helper.CommandLine.exe");
				return testApp.Value;
			}
		}
	}
}
