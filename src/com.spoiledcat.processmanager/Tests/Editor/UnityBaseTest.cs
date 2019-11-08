using System;
using System.Runtime.CompilerServices;
using SpoiledCat.Logging;
using SpoiledCat.SimpleIO;
using SpoiledCat.ProcessManager;
using SpoiledCat.Threading;
using SpoiledCat.Unity;
using UnityEngine.TestTools;

namespace BaseTests
{
	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined here so the tests can be compiled without
	// referencing Unity, and nunit on the command line
	// outside of Unity can execute the tests. Basically I don't
	// want to keep two copies of all the tests.
	public class CustomUnityTestAttribute : UnityTestAttribute
	{ }

	public partial class BaseTest
	{
		private LogAdapterBase existingLogger;
		private bool existingTracing;

		public BaseTest()
		{
			// set up the logger so it doesn't write exceptions to the unity log, the test runner doesn't like it
			existingLogger = LogHelper.LogAdapter;
			existingTracing = LogHelper.TracingEnabled;
			LogHelper.TracingEnabled = false;
			LogHelper.LogAdapter = new NullLogAdapter();

			TaskManager = new TaskManager().Initialize();

			UnityEngine.Debug.Log($"Starting test fixture. Main thread is {TaskManager.UIThread}");
		}

		public void Dispose()
		{
			TaskManager?.Dispose();
			LogHelper.LogAdapter = existingLogger;
			LogHelper.TracingEnabled = existingTracing;
		}

		protected void StartTest(out System.Diagnostics.Stopwatch watch, out ILogging logger, out ITaskManager taskManager,
			out SPath testPath, out IEnvironment environment, out IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new UnityLogAdapter(), true);
			watch = new System.Diagnostics.Stopwatch();

			taskManager = TaskManager;

			testPath = SPath.CreateTempDirectory(testName);

			environment = new UnityEnvironment(testName);
			((UnityEnvironment)environment).SetWorkingDirectory(testPath);
			environment.Initialize(testPath, testPath, "2018.4", testPath, testPath.Combine("Assets"));

			processManager = new ProcessManager(environment, taskManager.Token);

			logger.Trace("START");
			watch.Start();
		}

		protected SPath? testApp;

		protected SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
				{
					testApp = "Packages/com.spoiledcat.processmanager/Tests/Helpers~/Helper.CommandLine.exe".ToSPath().Resolve();
					if (!testApp.Value.FileExists())
					{
						UnityEngine.Debug.LogException(new InvalidOperationException("Test helper binaries are missing. Build the UnityTools.sln solution once with `dotnet build` in order to set up the tests."));
					}
				}
				return testApp.Value;
			}
		}

	}
}
