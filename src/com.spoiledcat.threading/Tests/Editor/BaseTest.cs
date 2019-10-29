namespace SpoiledCat.Base.Tests
{
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using Threading;
	using Logging;
	using UnityEngine.TestTools;
	using Debug = UnityEngine.Debug;

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
		public BaseTest()
		{
			TaskManager = new TaskManager().Initialize();

			Debug.Log($"Starting test fixture. Main thread is {TaskManager.UIThread}");
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager, [CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new UnityLogAdapter(), true);
			watch = new Stopwatch();

			taskManager = TaskManager;

			logger.Trace("START");
			watch.Start();
		}
	}
}
