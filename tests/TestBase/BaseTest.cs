using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpoiledCat.Base.Tests
{
	using Logging;
	using NiceIO;
	using Unity;

	using ProcessManager;
	using Threading;

	public class BaseTest : IDisposable
	{
		protected const int Timeout = 30000;

		public BaseTest()
		{
			Logger = LogHelper.GetLogger(GetType());

			TaskManager = new TaskManager();
			var syncContext = new TestThreadSynchronizationContext(TaskManager.Token);
			TaskManager.Initialize(new SynchronizationContextTaskScheduler(syncContext));

			var env = new UnityEnvironment("test-app");
			TestBasePath = NPath.CreateTempDirectory("unit tests");
			NPath.FileSystem = new FileSystem(TestBasePath);
			env.SetWorkingDirectory(TestBasePath);
			env.Initialize(TestBasePath, TestBasePath, "2018.4", TestBasePath, TestBasePath.Combine("Assets"));

			ProcessManager = new ProcessManager(env, TaskManager.Token);
		}

		protected ILogging Logger { get; }

		protected ITaskManager TaskManager { get; }

		protected IProcessManager ProcessManager { get; set; }
		protected NPath TestBasePath { get; private set; }
		protected NPath TestApp => System.Reflection.Assembly.GetExecutingAssembly().Location.ToNPath().Parent.Combine("CommandLine.exe");

		public void Dispose()
		{

			if (TaskManager != null)
			{
				TaskManager?.Dispose();
				try
				{
					TestBasePath.DeleteIfExists();
				}
				catch
				{}
			}
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
		{
			watch = new Stopwatch();
			logger = LogHelper.GetLogger(testName);
			logger.Trace("Starting test");
		}

		protected void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
		{
			if (!String.IsNullOrEmpty(message))
				logger.Trace(message);
			watch.Reset();
			watch.Start();
		}

		protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
		{
			watch.Stop();
			logger.Trace($"Time: {watch.ElapsedMilliseconds}");
		}
	}

}
